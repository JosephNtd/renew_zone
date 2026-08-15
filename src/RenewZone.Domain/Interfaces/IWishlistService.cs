using RenewZone.Domain.Models.Account;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Danh sách sản phẩm yêu thích (Giai đoạn 9). Mọi phương thức nhận <c>userId</c> và tự lọc —
/// xem ghi chú chống IDOR ở <see cref="ICustomerOrderService"/>.
/// </summary>
public interface IWishlistService
{
    Task<IReadOnlyList<WishlistEntry>> GetMyWishlistAsync(string userId);

    /// <summary>
    /// Bật/tắt tim. Trả <c>IsFavorite</c> = trạng thái SAU thao tác để client vẽ lại nút mà
    /// không phải đoán (bấm nhanh 2 lần ở 2 tab vẫn ra đúng trạng thái thật trong DB).
    /// </summary>
    Task<WishlistToggleResult> ToggleAsync(string userId, int productId);

    /// <summary>Id các sản phẩm đã thích — để tô sẵn nút tim khi render danh sách sản phẩm.</summary>
    Task<IReadOnlySet<int>> GetFavoriteProductIdsAsync(string userId);

    Task<int> CountAsync(string userId);
}

public record WishlistToggleResult(bool Ok, bool IsFavorite, int Count, string Message);
