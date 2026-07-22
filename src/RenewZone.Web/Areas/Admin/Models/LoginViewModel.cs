using System.ComponentModel.DataAnnotations;

namespace RenewZone.Web.Areas.Admin.Models;

/// <summary>
/// Form đăng nhập quản trị. Mọi thông báo lỗi đều viết tiếng Việt — không có ErrorMessage
/// thì framework chèn chuỗi "The X field is required." vào HTML (CLAUDE.md bẫy số 11).
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [Display(Name = "Tên đăng nhập")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }

    /// <summary>
    /// Trang muốn vào trước khi bị chặn. ⚠️ PHẢI kiểm <c>Url.IsLocalUrl()</c> trước khi redirect —
    /// nhận thẳng giá trị này là lỗ hổng open redirect kinh điển (kẻ tấn công gửi link
    /// <c>/admin/login?returnUrl=https://evil.com</c> để cướp phiên sau khi nạn nhân đăng nhập).
    /// </summary>
    public string? ReturnUrl { get; set; }
}
