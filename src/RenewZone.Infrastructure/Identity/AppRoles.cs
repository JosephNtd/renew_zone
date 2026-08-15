namespace RenewZone.Infrastructure.Identity;

/// <summary>
/// Tên role dùng thống nhất toàn hệ thống — tránh gõ chuỗi rời rạc trong [Authorize].
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";

    /// <summary>
    /// Khách mua hàng (Giai đoạn 9). Mọi tài khoản đăng ký từ web khách đều vào role này.
    /// Dùng để phân biệt với admin ở màn quản lý khách hàng và ở luồng chuyển hướng đăng nhập.
    /// </summary>
    public const string Customer = "Customer";
}
