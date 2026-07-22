using System.Text.Json;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;

namespace RenewZone.Web.Services;

/// <inheritdoc cref="ICartService" />
public class CartService(IHttpContextAccessor accessor, ICatalogQueryService catalog) : ICartService
{
    private const string SessionKey = "rz_cart";

    /// <summary>Trần số lượng mỗi dòng — cửa hàng bán lẻ, tránh khách gõ số vô lý.</summary>
    public const int MaxQuantityPerItem = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private ISession Session =>
        accessor.HttpContext?.Session
        ?? throw new InvalidOperationException("Không có HttpContext — CartService chỉ dùng trong request.");

    public Task<Cart> GetCartAsync() => Task.FromResult(Read());

    public async Task<CartOpResult> AddAsync(int productId, IReadOnlyList<int> optionValueIds, int quantity)
    {
        var cart = Read();

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
            // Đồng bộ lại tên/giá/ảnh theo dữ liệu mới nhất — giỏ có thể nằm trong session khá lâu.
            item.ProductName = product.Name;
            item.Slug = product.Slug;
            item.UnitPrice = unitPrice;
            item.Quantity = Clamp(item.Quantity + qty);
        }

        Save(cart);
        return CartOpResult.Success(cart, item, "Đã thêm vào giỏ hàng");
    }

    public Task<CartOpResult> UpdateQuantityAsync(string key, int quantity)
    {
        var cart = Read();
        var item = cart.Find(key);

        if (item is null)
        {
            return Task.FromResult(CartOpResult.Fail(cart, "Sản phẩm không còn trong giỏ hàng."));
        }

        item.Quantity = Clamp(quantity);
        Save(cart);
        return Task.FromResult(CartOpResult.Success(cart, item));
    }

    public Task<CartOpResult> RemoveAsync(string key)
    {
        var cart = Read();
        var item = cart.Find(key);

        if (item is null)
        {
            return Task.FromResult(CartOpResult.Fail(cart, "Sản phẩm không còn trong giỏ hàng."));
        }

        cart.Items.Remove(item);
        Save(cart);
        return Task.FromResult(CartOpResult.Success(cart, message: "Đã xóa khỏi giỏ hàng"));
    }

    public Task ClearAsync()
    {
        Session.Remove(SessionKey);
        return Task.CompletedTask;
    }

    // ---------------------------------------------------------------- helpers

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

    private Cart Read()
    {
        var raw = Session.GetString(SessionKey);
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
            // Dữ liệu session cũ/hỏng (vd đổi cấu trúc CartItem giữa 2 lần deploy) ⇒ bỏ, coi như giỏ mới.
            Session.Remove(SessionKey);
            return new Cart();
        }
    }

    private void Save(Cart cart)
    {
        if (cart.IsEmpty)
        {
            Session.Remove(SessionKey);
            return;
        }

        Session.SetString(SessionKey, JsonSerializer.Serialize(cart, JsonOptions));
    }
}
