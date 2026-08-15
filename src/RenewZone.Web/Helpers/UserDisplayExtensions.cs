using System.Security.Claims;
using RenewZone.Infrastructure.Identity;

namespace RenewZone.Web.Helpers;

/// <summary>
/// Lấy tên hiển thị của người đang đăng nhập ngay trong view, không truy vấn DB.
/// </summary>
public static class UserDisplayExtensions
{
    /// <summary>
    /// Họ tên nếu có (claim do <see cref="AppUserClaimsPrincipalFactory"/> gắn lúc đăng nhập),
    /// nếu không thì rơi về tên đăng nhập — với khách hàng đó là số điện thoại.
    /// Không bao giờ trả chuỗi rỗng (luật fallback ở CLAUDE.md mục 4.7).
    /// </summary>
    public static string DisplayName(this ClaimsPrincipal user)
    {
        var fullName = user.FindFirstValue(AppUserClaimsPrincipalFactory.FullNameClaim);
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return string.IsNullOrWhiteSpace(user.Identity?.Name) ? "Bạn" : user.Identity.Name;
    }
}
