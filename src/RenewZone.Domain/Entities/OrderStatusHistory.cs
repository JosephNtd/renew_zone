using RenewZone.Domain.Common;
using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Một dòng trong SỔ NHẬT KÝ trạng thái đơn — append-only ledger, giống <see cref="OrderItem"/>:
/// ghi vào rồi KHÔNG bao giờ sửa/xóa. Trả lời được "ai đổi, lúc nào, từ đâu sang đâu, vì sao"
/// khi có tranh chấp ("sao đơn của tôi bị hủy?").
/// <para>
/// Cố ý dùng <see cref="BaseEntity"/> (chỉ Id) chứ KHÔNG <c>AuditableEntity</c> và KHÔNG
/// <c>ISoftDeletable</c>: đây là bản ghi sổ sách bất biến, không có khái niệm "cập nhật" hay
/// "xóa mềm". <see cref="ChangedAt"/> vì thế phải set TAY trong service (không có hook audit lo hộ).
/// </para>
/// </summary>
public class OrderStatusHistory : BaseEntity
{
    public int OrderId { get; set; }

    /// <summary>Trạng thái trước khi đổi. NULL ở mốc đầu tiên (lúc tạo đơn — chưa có "từ").</summary>
    public OrderStatus? FromStatus { get; set; }

    public OrderStatus ToStatus { get; set; }

    /// <summary>Ghi chú tùy chọn admin nhập khi đổi (vd "khách hẹn giao chiều thứ 7").</summary>
    public string? Note { get; set; }

    /// <summary>
    /// Ai thực hiện — SNAPSHOT tên tại thời điểm đổi (KHÔNG phải FK sang AspNetUsers).
    /// Cùng triết lý với <see cref="OrderItem.ProductName"/>: tài khoản admin có thể bị xóa sau này,
    /// nhưng sổ sách phải giữ nguyên "ai đã làm" và không phải Include mỗi lần đọc.
    /// "Khách đặt hàng" cho mốc tạo đơn từ web khách (guest checkout — không có danh tính).
    /// </summary>
    public string ChangedBy { get; set; } = null!;

    /// <summary>Thời điểm — set tay trong service (không auto vì đây không phải AuditableEntity).</summary>
    public DateTime ChangedAt { get; set; }

    public Order Order { get; set; } = null!;
}
