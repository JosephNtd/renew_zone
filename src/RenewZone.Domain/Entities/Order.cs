using RenewZone.Domain.Common;
using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Đơn hàng đặt bởi KHÁCH VÃNG LAI (guest checkout — site không có tài khoản người mua),
/// nên thông tin khách lưu thẳng tại đây, không tham chiếu bảng user nào.
/// </summary>
public class Order : AuditableEntity
{
    /// <summary>Mã đơn hiển thị cho khách tra cứu/đối chiếu, vd "RZ250720483920". Unique.</summary>
    public string Code { get; set; } = null!;

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
}
