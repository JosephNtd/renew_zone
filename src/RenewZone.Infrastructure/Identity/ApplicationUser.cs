using Microsoft.AspNetCore.Identity;

namespace RenewZone.Infrastructure.Identity;

/// <summary>
/// User quản trị của hệ thống (khách mua hàng KHÔNG có tài khoản — guest checkout).
/// Kế thừa sẵn để sau này mở rộng thêm trường mà không phải đổi kiểu user toàn hệ thống.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}
