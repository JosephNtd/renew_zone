using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace RenewZone.Infrastructure.Identity;

/// <summary>
/// Nhét thêm HỌ TÊN của người dùng vào cookie đăng nhập (Giai đoạn 9).
/// <para>
/// Vì sao cần: <c>User.Identity.Name</c> là <c>UserName</c>, mà với khách hàng thì UserName
/// chính là SỐ ĐIỆN THOẠI. Chào khách bằng "Xin chào, 0912345678" thì rất vô duyên, còn nếu
/// mỗi chỗ hiển thị lại tự truy vấn DB để lấy tên thì header + thanh bên + mọi trang đều tốn
/// thêm một câu query cho mỗi lần tải trang.
/// </para>
/// <para>
/// Claim được sinh lúc đăng nhập và mỗi lần <c>RefreshSignInAsync</c> (khu tài khoản gọi hàm
/// này sau khi khách sửa hồ sơ), nên tên trên thanh trên cùng luôn khớp với hồ sơ.
/// </para>
/// </summary>
public class AppUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    /// <summary>Tên claim chứa họ tên hiển thị. Dùng hằng để view và factory không gõ lệch chuỗi.</summary>
    public const string FullNameClaim = "rz:fullname";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            identity.AddClaim(new Claim(FullNameClaim, user.FullName));
        }

        return identity;
    }
}
