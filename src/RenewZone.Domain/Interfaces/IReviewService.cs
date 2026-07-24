using RenewZone.Domain.Entities;
using RenewZone.Domain.Models;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Đánh giá phía web khách (Giai đoạn 8): ghi đánh giá mới (trạng thái chờ duyệt) và đọc
/// danh sách đánh giá ĐÃ DUYỆT để hiện trên trang chi tiết sản phẩm.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Ghi đánh giá của khách ở trạng thái chờ duyệt. Trả về bản ghi vừa tạo để tầng Web đẩy
    /// thông báo realtime cho admin. KHÔNG đụng tới AvgRating/ReviewCount (chỉ đổi khi admin duyệt).
    /// Trả null nếu ProductId không trỏ tới sản phẩm còn active — không tin client, kiểm ở server.
    /// </summary>
    Task<ProductReview?> SubmitAsync(ReviewRequest request);

    /// <summary>Đánh giá đã duyệt của một sản phẩm, mới nhất trước — dựng section đánh giá ở PDP.</summary>
    Task<IReadOnlyList<ProductReview>> GetApprovedAsync(int productId);
}
