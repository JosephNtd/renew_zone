using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Đánh giá việc giao hàng của cả một đơn (khách chấm 1–5 sao + ghi chú), mỗi đơn tối đa 1 bản ghi.
/// Chỉ shop xem (chi tiết đơn ở trang quản trị + điểm trung bình ở Dashboard), không hiện công khai
/// nên không cần duyệt như đánh giá sản phẩm.
/// </summary>
public class DeliveryReview : AuditableEntity
{
    public int OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    public Order Order { get; set; } = null!;
}
