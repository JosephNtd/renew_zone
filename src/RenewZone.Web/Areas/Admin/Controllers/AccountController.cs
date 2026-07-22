using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RenewZone.Infrastructure.Identity;
using RenewZone.Web.Areas.Admin.Models;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// Đăng nhập/đăng xuất quản trị. KHÔNG kế thừa <see cref="BaseAdminController"/> — trang đăng nhập
/// phải vào được khi chưa đăng nhập, kế thừa sẽ tạo vòng lặp chuyển hướng vô tận.
/// Bù lại, các attribute bảo mật được gắn tường minh ngay tại đây.
/// <para>
/// ⚠️ <c>[AllowAnonymous]</c> đặt trên TỪNG action Login chứ không phải trên class: để ở class thì nó
/// đè luôn <c>[Authorize]</c> của Logout (analyzer ASP0026 đã bắt được) — đăng xuất khi đó thành
/// endpoint ẩn danh, sai hoàn toàn ý đồ.
/// </para>
/// </summary>
[Area("Admin")]
[AutoValidateAntiforgeryToken]
public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous]
    [HttpGet("/admin/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        // Đã đăng nhập rồi mà vẫn mở trang login ⇒ đưa thẳng vào dashboard, đừng bắt gõ lại
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("/admin/login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByNameAsync(model.UserName);

        if (user is not null)
        {
            // lockoutOnFailure: true ⇒ Identity tự khóa sau 5 lần sai trong 10 phút (cấu hình ở Giai đoạn 0)
            var result = await signInManager.PasswordSignInAsync(
                user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Chỉ tài khoản có role Admin mới được vào — user Identity khác (nếu sau này có)
                // đăng nhập đúng mật khẩu vẫn không được lọt vào khu quản trị
                if (!await userManager.IsInRoleAsync(user, AppRoles.Admin))
                {
                    await signInManager.SignOutAsync();
                    logger.LogWarning("Tài khoản {User} đăng nhập đúng mật khẩu nhưng không có quyền Admin", model.UserName);
                    ModelState.AddModelError(string.Empty, "Tài khoản này không có quyền truy cập trang quản trị.");
                    return View(model);
                }

                logger.LogInformation("Quản trị viên {User} đăng nhập thành công", model.UserName);
                return RedirectToLocalOrDashboard(model.ReturnUrl);
            }

            if (result.IsLockedOut)
            {
                var minutes = user.LockoutEnd.HasValue
                    ? Math.Max(1, (int)Math.Ceiling((user.LockoutEnd.Value - DateTimeOffset.Now).TotalMinutes))
                    : 10;

                logger.LogWarning("Tài khoản {User} bị khóa do đăng nhập sai nhiều lần", model.UserName);
                ModelState.AddModelError(string.Empty,
                    $"Tài khoản đã bị tạm khóa do nhập sai quá nhiều lần. Vui lòng thử lại sau {minutes} phút.");
                return View(model);
            }
        }

        // Sai tên đăng nhập hay sai mật khẩu đều báo CÙNG MỘT CÂU — nói rõ "không có tài khoản này"
        // là tự tiết lộ tài khoản nào có thật cho người đang dò
        logger.LogWarning("Đăng nhập thất bại cho tên đăng nhập {User}", model.UserName);
        ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
        return View(model);
    }

    [HttpPost("/admin/logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Chỉ chuyển hướng tới đường dẫn NỘI BỘ. <c>Url.IsLocalUrl</c> loại mọi URL tuyệt đối
    /// (kể cả "//evil.com" trông như đường dẫn tương đối) — chống open redirect.
    /// </summary>
    private IActionResult RedirectToLocalOrDashboard(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }
}
