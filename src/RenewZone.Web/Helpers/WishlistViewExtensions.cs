using System.Security.Claims;
using RenewZone.Domain.Interfaces;

namespace RenewZone.Web.Helpers;

/// <summary>
/// Tra "sản phẩm này đã được thích chưa" ngay trong view, dùng cho nút ♡ ở card sản phẩm và PDP.
/// </summary>
public static class WishlistViewExtensions
{
    private const string CacheKey = "rz_fav_ids";

    /// <summary>
    /// Tập id sản phẩm mà người đang đăng nhập đã thích. Khách vãng lai ⇒ tập rỗng.
    /// <para>
    /// Kết quả được nhớ trong <c>HttpContext.Items</c> (sống đúng một request): trang danh mục
    /// render 12 card, không nhớ lại thì thành 12 lần truy vấn cho cùng một câu hỏi.
    /// </para>
    /// <para>
    /// Bọc try/catch: nút tim là tiện ích phụ, lỗi khi đọc nó tuyệt đối không được làm sập
    /// trang danh sách sản phẩm — cùng triết lý với <c>GetAdminBrandAsync</c> (mục 4.16).
    /// </para>
    /// </summary>
    public static async Task<IReadOnlySet<int>> GetFavoriteIdsAsync(
        this IWishlistService wishlist, HttpContext context)
    {
        if (context.Items.TryGetValue(CacheKey, out var cached) && cached is IReadOnlySet<int> ids)
        {
            return ids;
        }

        IReadOnlySet<int> result;

        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

        if (string.IsNullOrEmpty(userId))
        {
            result = new HashSet<int>();
        }
        else
        {
            try
            {
                result = await wishlist.GetFavoriteProductIdsAsync(userId);
            }
            catch
            {
                result = new HashSet<int>();
            }
        }

        context.Items[CacheKey] = result;
        return result;
    }
}
