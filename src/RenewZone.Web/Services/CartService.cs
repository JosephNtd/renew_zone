using System.Security.Claims;
using System.Text.Json;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;

namespace RenewZone.Web.Services;

/// <inheritdoc cref="ICartService" />
public class CartService(
    IHttpContextAccessor accessor,
    ICatalogQueryService catalog,
    IUserCartStore userCarts) : ICartService
{
    private const string SessionKey = "rz_cart";

    /// <summary>
    /// Khóa cache giỏ trong <c>HttpContext.Items</c> (sống đúng một request).
    /// Một lần tải trang đọc giỏ nhiều lần (badge ở header, mini-cart, view chính); với giỏ
    /// Session thì miễn phí, nhưng giỏ DB mà không nhớ lại sẽ thành 3 lần SELECT cho mỗi trang.
    /// </summary>
    private const string RequestCacheKey = "rz_cart_loaded";

    /// <summary>Trần số lượng mỗi dòng — cửa hàng bán lẻ, tránh khách gõ số vô lý.</summary>
    public const int MaxQuantityPerItem = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private HttpContext Context =>
        accessor.HttpContext
        ?? throw new InvalidOperationException("Không có HttpContext — CartService chỉ dùng trong request.");

    /// <summary>Id tài khoản đang đăng nhập, hoặc null khi là khách vãng lai.</summary>
    private string? CurrentUserId =>
        Context.User?.Identity?.IsAuthenticated == true
            ? Context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

    public async Task<Cart> GetCartAsync()
    {
        if (Context.Items.TryGetValue(RequestCacheKey, out var cached) && cached is Cart cart)
        {
            return cart;
        }

        var loaded = await LoadAsync();
        Context.Items[RequestCacheKey] = loaded;
        return loaded;
    }

    public async Task<CartOpResult> AddAsync(int productId, IReadOnlyList<int> optionValueIds, int quantity)
    {
        var cart = await GetCartAsync();
        var result = await AddToCartAsync(cart, productId, optionValueIds, quantity);

        if (result.Ok)
        {
            await SaveAsync(cart);
        }

        return result;
    }

    public async Task<CartOpResult> UpdateQuantityAsync(string key, int quantity)
    {
        var cart = await GetCartAsync();
        var item = cart.Find(key);

        if (item is null)
        {
            return CartOpResult.Fail(cart, "Sản phẩm không còn trong giỏ hàng.");
        }

        item.Quantity = Clamp(quantity);
        await SaveAsync(cart);
        return CartOpResult.Success(cart, item);
    }

    public async Task<CartOpResult> RemoveAsync(string key)
    {
        var cart = await GetCartAsync();
        var item = cart.Find(key);

        if (item is null)
        {
            return CartOpResult.Fail(cart, "Sản phẩm không còn trong giỏ hàng.");
        }

        cart.Items.Remove(item);
        await SaveAsync(cart);
        return CartOpResult.Success(cart, message: "Đã xóa khỏi giỏ hàng");
    }

    public async Task ClearAsync()
    {
        Context.Session.Remove(SessionKey);

        if (CurrentUserId is { } userId)
        {
            await userCarts.ClearAsync(userId);
        }

        Context.Items[RequestCacheKey] = new Cart();
    }

    public async Task MergeGuestCartAsync(string userId)
    {
        var guest = ReadSession();
        if (guest.IsEmpty)
        {
            // Không có gì để gộp. KHÔNG được ghi đè giỏ đã lưu của tài khoản bằng giỏ rỗng —
            // đăng nhập trên máy mới sẽ xóa sạch giỏ khách để dành từ hôm trước.
            return;
        }

        var target = ParseOrEmpty(await userCarts.LoadAsync(userId));

        // Đi qua đúng AddToCartAsync như khi khách tự bấm ⇒ giá được tính lại theo bảng giá HIỆN
        // TẠI và dòng trùng biến thể được cộng dồn. Món nào không còn bán thì lặng lẽ bỏ qua:
        // đăng nhập mà báo lỗi "sản phẩm X đã ngừng bán" là trải nghiệm rất khó hiểu.
        foreach (var item in guest.Items)
        {
            await AddToCartAsync(target, item.ProductId, item.OptionValueIds, item.Quantity);
        }

        await userCarts.SaveAsync(userId, JsonSerializer.Serialize(target, JsonOptions));

        Context.Session.Remove(SessionKey);
        Context.Items[RequestCacheKey] = target;
    }

    /// <summary>
    /// Phần lõi thêm hàng: THUẦN thao tác trên đối tượng <paramref name="cart"/> truyền vào,
    /// không tự đọc/ghi kho chứa. Nhờ vậy vừa dùng cho khách bấm "Thêm vào giỏ", vừa dùng cho
    /// việc gộp giỏ lúc đăng nhập — một đoạn logic duy nhất, không có bản sao để lệch.
    /// </summary>
    private async Task<CartOpResult> AddToCartAsync(
        Cart cart, int productId, IReadOnlyList<int> optionValueIds, int quantity)
    {
        var product = await catalog.GetProductForCartAsync(productId);
        if (product is null)
        {
            return CartOpResult.Fail(cart, "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");
        }

        // Lưu ý 6.5: sản phẩm "Liên hệ" không có giá công khai ⇒ chặn cả ở server, không chỉ ở UI.
        if (product.IsContactPrice)
        {
            return CartOpResult.Fail(cart, "Sản phẩm này vui lòng liên hệ hotline để được tư vấn.");
        }

        if (!TryResolveOptions(product, optionValueIds, out var chosen))
        {
            return CartOpResult.Fail(cart, "Tùy chọn không hợp lệ, vui lòng chọn lại.");
        }

        var unitPrice = product.DisplayPrice + chosen.Sum(v => v.PriceAdjustment);
        if (unitPrice < 0)
        {
            unitPrice = 0;
        }

        var chosenIds = chosen.Select(v => v.Id).ToList();
        var key = CartItem.BuildKey(product.Id, chosenIds);
        var qty = Clamp(quantity);

        var item = cart.Find(key);
        if (item is null)
        {
            item = new CartItem
            {
                Key = key,
                ProductId = product.Id,
                ProductName = product.Name,
                Slug = product.Slug,
                ImageUrl = product.Images.FirstOrDefault()?.Url ?? "/images/placeholder-product.svg",
                UnitPrice = unitPrice,
                VariantDescription = chosen.Count > 0 ? string.Join(" · ", chosen.Select(v => v.Value)) : null,
                OptionValueIds = chosenIds.Order().ToList(),
                Quantity = qty
            };
            cart.Items.Add(item);
        }
        else
        {
            // Đồng bộ lại tên/giá/ảnh theo dữ liệu mới nhất — giỏ có thể đã nằm im khá lâu.
            item.ProductName = product.Name;
            item.Slug = product.Slug;
            item.UnitPrice = unitPrice;
            item.Quantity = Clamp(item.Quantity + qty);
        }

        return CartOpResult.Success(cart, item, "Đã thêm vào giỏ hàng");
    }

    /// <summary>
    /// Đối chiếu id option client gửi lên với dữ liệu thật của sản phẩm.
    /// Id lạ (không thuộc sản phẩm) ⇒ từ chối; nhóm nào khách chưa chọn ⇒ lấy giá trị mặc định,
    /// đúng như PDP đã chọn sẵn khi mở trang.
    /// </summary>
    private static bool TryResolveOptions(
        Product product,
        IReadOnlyList<int> optionValueIds,
        out List<ProductOptionValue> chosen)
    {
        chosen = [];

        var validIds = product.OptionGroups.SelectMany(g => g.Values).Select(v => v.Id).ToHashSet();
        if (optionValueIds.Any(id => !validIds.Contains(id)))
        {
            return false;
        }

        foreach (var group in product.OptionGroups)
        {
            var pick = group.Values.FirstOrDefault(v => optionValueIds.Contains(v.Id))
                       ?? group.Values.FirstOrDefault(v => v.IsDefault)
                       ?? group.Values.FirstOrDefault();

            if (pick is not null)
            {
                chosen.Add(pick);
            }
        }

        return true;
    }

    private static int Clamp(int quantity) => Math.Clamp(quantity, 1, MaxQuantityPerItem);

    private async Task<Cart> LoadAsync() =>
        CurrentUserId is { } userId
            ? ParseOrEmpty(await userCarts.LoadAsync(userId))
            : ReadSession();

    private async Task SaveAsync(Cart cart)
    {
        Context.Items[RequestCacheKey] = cart;

        if (CurrentUserId is { } userId)
        {
            if (cart.IsEmpty)
            {
                await userCarts.ClearAsync(userId);
                return;
            }

            await userCarts.SaveAsync(userId, JsonSerializer.Serialize(cart, JsonOptions));
            return;
        }

        if (cart.IsEmpty)
        {
            Context.Session.Remove(SessionKey);
            return;
        }

        Context.Session.SetString(SessionKey, JsonSerializer.Serialize(cart, JsonOptions));
    }

    private Cart ReadSession()
    {
        var raw = Context.Session.GetString(SessionKey);
        var cart = ParseOrEmpty(raw);

        // Dữ liệu session cũ/hỏng (vd đổi cấu trúc CartItem giữa 2 lần deploy) ⇒ dọn luôn cho
        // khỏi vấp lại ở mỗi request sau.
        if (cart.IsEmpty && !string.IsNullOrEmpty(raw))
        {
            Context.Session.Remove(SessionKey);
        }

        return cart;
    }

    private static Cart ParseOrEmpty(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return new Cart();
        }

        try
        {
            return JsonSerializer.Deserialize<Cart>(raw, JsonOptions) ?? new Cart();
        }
        catch (JsonException)
        {
            return new Cart();
        }
    }
}
