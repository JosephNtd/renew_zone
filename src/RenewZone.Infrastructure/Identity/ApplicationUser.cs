using Microsoft.AspNetCore.Identity;

namespace RenewZone.Infrastructure.Identity;

/// <summary>
/// Tài khoản của hệ thống — dùng CHUNG cho quản trị viên và khách mua hàng (Giai đoạn 9),
/// phân biệt bằng role: <see cref="AppRoles.Admin"/> vs <see cref="AppRoles.Customer"/>.
/// <para>
/// Vì sao một bảng chứ không tách bảng khách riêng: Identity đã lo sẵn băm mật khẩu, khóa
/// tài khoản sau nhiều lần sai, cookie, token — dựng bảng khách riêng đồng nghĩa viết lại
/// toàn bộ những thứ đó (viết sai là lỗ bảo mật thật). Phân quyền bằng role nên khách
/// KHÔNG bao giờ lọt được vào <c>/admin</c>.
/// </para>
/// <para>
/// ⚠️ <c>UserName</c> của khách hàng = SỐ ĐIỆN THOẠI (xem CLAUDE.md mục 4.17); admin giữ
/// username chữ (<c>admin</c>). <c>Email</c> của khách là TÙY CHỌN.
/// </para>
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    /// <summary>Ngày sinh — tùy chọn, để shop chăm sóc khách (ưu đãi sinh nhật).</summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>Giới tính tự khai: "Nam" / "Nữ" / "Khác" / null. Để chuỗi tự do cho khỏi migration khi đổi.</summary>
    public string? Gender { get; set; }

    /// <summary>Thời điểm tạo tài khoản — Identity không có sẵn cột này, màn admin Khách hàng cần.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
