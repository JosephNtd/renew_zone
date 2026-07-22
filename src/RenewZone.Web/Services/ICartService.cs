using RenewZone.Domain.Models;

namespace RenewZone.Web.Services;

/// <summary>
/// Giỏ hàng lưu trong Session (thuần tầng Web — không đụng DB trực tiếp, sản phẩm lấy
/// qua <see cref="RenewZone.Domain.Interfaces.ICatalogQueryService"/>).
/// </summary>
public interface ICartService
{
    Task<Cart> GetCartAsync();

    /// <summary>
    /// Thêm sản phẩm vào giỏ. Giá và mô tả biến thể được TÍNH LẠI Ở SERVER từ
    /// <paramref name="optionValueIds"/>; client không gửi giá. Cùng bộ option ⇒ cộng dồn số lượng.
    /// </summary>
    Task<CartOpResult> AddAsync(int productId, IReadOnlyList<int> optionValueIds, int quantity);

    Task<CartOpResult> UpdateQuantityAsync(string key, int quantity);

    Task<CartOpResult> RemoveAsync(string key);

    Task ClearAsync();
}
