using RenewZone.Domain.Enums;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Quản lý đánh giá sản phẩm ở trang admin (Giai đoạn 8): duyệt / từ chối / trả lời / xóa.
/// Mỗi thao tác đổi trạng thái đều tính lại <c>Product.AvgRating</c> + <c>ReviewCount</c>
/// (chỉ đếm đánh giá <see cref="ReviewStatus.Approved"/>) — card + PDP đọc thẳng 2 cột đó.
/// </summary>
public interface IReviewAdminService
{
    Task<PagedResult<ReviewListItem>> GetListAsync(ReviewListRequest request);
    Task<ReviewStatusCounts> GetCountsAsync();

    /// <summary>Duyệt hoặc từ chối. Không đổi gì nếu đã ở trạng thái đó (tránh recompute thừa).</summary>
    Task<AdminResult> SetStatusAsync(int id, ReviewStatus status);

    /// <summary>Ghi câu trả lời công khai của shop. Chuỗi rỗng ⇒ gỡ phản hồi.</summary>
    Task<AdminResult> ReplyAsync(int id, string? reply);

    Task<AdminResult> DeleteAsync(int id);
}
