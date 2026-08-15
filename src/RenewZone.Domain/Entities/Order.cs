using RenewZone.Domain.Common;
using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Đơn hàng. Từ Giai đoạn 9 khách BẮT BUỘC đăng nhập mới đặt được, nhưng thông tin người
/// nhận vẫn lưu snapshot thẳng tại đây (không đọc ngược qua tài khoản): khách có thể sửa
/// hồ sơ hay đổi số điện thoại sau khi đặt, mà sổ sách thì không được đổi theo.
/// </summary>
public class Order : AuditableEntity
{
    /// <summary>Mã đơn hiển thị cho khách tra cứu/đối chiếu, vd "RZ250720483920". Unique.</summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Tài khoản đã đặt đơn (khóa ngoại sang <c>AspNetUsers</c>, không có navigation vì Domain
    /// không biết <c>ApplicationUser</c>). NULL = đơn của khách vãng lai đặt TRƯỚC Giai đoạn 9.
    /// <para>
    /// ⚠️ Đơn NULL cố ý KHÔNG được tự gắn cho tài khoản trùng số điện thoại (quyết định
    /// 2026-07-28): không có OTP xác minh SĐT nên bất kỳ ai đăng ký bằng SĐT người khác
    /// sẽ đọc được tên/địa chỉ/lịch sử mua của họ. Khách cần tra đơn cũ thì gọi shop.
    /// </para>
    /// </summary>
    public string? UserId { get; set; }

    public string CustomerName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string Address { get; set; } = null!;
    public string? Note { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;

    /// <summary>Tổng tiền chốt tại thời điểm đặt (tổng LineTotal các item).</summary>
    public decimal TotalAmount { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];

    /// <summary>Nhật ký các lần đổi trạng thái (append-only) — xem <see cref="OrderStatusHistory"/>.</summary>
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = [];
}
