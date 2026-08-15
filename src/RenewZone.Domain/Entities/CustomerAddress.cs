using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Một địa chỉ nhận hàng trong sổ địa chỉ của khách (Giai đoạn 9).
/// <para>
/// <c>UserId</c> là khóa ngoại sang <c>AspNetUsers</c> nhưng KHÔNG có navigation property:
/// tầng Domain không được biết <c>ApplicationUser</c> (nằm ở Infrastructure) — quan hệ được
/// khai báo trong <c>CustomerAddressConfiguration</c>, nơi có sẵn cả hai kiểu.
/// </para>
/// <para>
/// Cố ý KHÔNG xóa mềm: đây là dữ liệu riêng của khách, khách xóa là phải mất thật (thùng rác
/// của admin không có lý do gì chứa địa chỉ nhà khách hàng). Đơn hàng đã đặt vẫn giữ được
/// địa chỉ vì <c>Order.Address</c> là snapshot chuỗi, không tham chiếu bảng này.
/// </para>
/// </summary>
public class CustomerAddress : AuditableEntity
{
    public string UserId { get; set; } = null!;

    /// <summary>Nhãn khách tự đặt để phân biệt: "Nhà riêng", "Công ty"... Tùy chọn.</summary>
    public string? Label { get; set; }

    /// <summary>Người nhận có thể khác chủ tài khoản (gửi quà, nhờ nhận hộ).</summary>
    public string ReceiverName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    /// <summary>Địa chỉ một dòng — khớp đúng kiểu dữ liệu <c>Order.Address</c> đang dùng.</summary>
    public string AddressLine { get; set; } = null!;

    /// <summary>
    /// Địa chỉ chọn sẵn khi vào trang đặt hàng. Mỗi khách chỉ có ĐÚNG 1 địa chỉ mặc định —
    /// bảo đảm tập trung trong <c>CustomerAddressService</c> (cùng triết lý
    /// <c>EnsureSingleDefault</c> của biến thể sản phẩm, CLAUDE.md mục 4.9).
    /// </summary>
    public bool IsDefault { get; set; }
}
