namespace RenewZone.Infrastructure.Identity;

/// <summary>
/// Tên role dùng thống nhất toàn hệ thống — tránh gõ chuỗi rời rạc trong [Authorize].
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
}
