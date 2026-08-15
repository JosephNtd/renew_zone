using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RenewZone.Infrastructure.Identity;
using RenewZone.Web.Helpers;
using RenewZone.Web.Models.ViewModels;
using RenewZone.Web.Services;

namespace RenewZone.Web.Controllers;

/// <summary>
/// Đăng ký / đăng nhập / đăng xuất cho KHÁCH HÀNG (Giai đoạn 9).
/// Khu quản trị có cửa riêng ở <c>Areas/Admin/Controllers/AccountController</c> — cùng bảng user,
/// cùng cookie, chỉ khác role và giao diện.
/// <para>
/// Mỗi thao tác có HAI cửa vào dùng chung một phần lõi:
/// <list type="bullet">
///   <item>Trang đầy đủ (<c>/dang-nhap</c>, <c>/dang-ky</c>) — cho người vào thẳng URL, cho
///   <c>returnUrl</c> khi bị chặn ở trang cần đăng nhập, và để hoạt động cả khi tắt JavaScript.</item>
///   <item>Endpoint JSON (<c>/tai-khoan/dang-nhap-ajax</c>) — cho hộp thoại bật lên khi khách
///   vãng lai bấm "Thêm vào giỏ", để họ không mất trang đang xem.</item>
/// </list>
/// Phần lõi <see cref="TrySignInAsync"/>/<see cref="TryRegisterAsync"/> là nơi DUY NHẤT kiểm tra —
/// hai cửa không thể có luật khác nhau.
/// </para>
/// </summary>
public partial class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ICartService cart,
    ILogger<AuthController> logger) : Controller
{
    [GeneratedRegex(PhoneRules.Pattern)]
    private static partial Regex PhoneFormat();

    // ───────────────────────────── Trang đầy đủ ─────────────────────────────

    [HttpGet("/dang-nhap")]
    public IActionResult Login(string? returnUrl = null)
    {
        // Đang đăng nhập mà vẫn CHỦ ĐỘNG mở trang này ⇒ gần như chắc chắn là muốn ĐỔI sang tài
        // khoản khác (hay gặp nhất: đang đăng nhập admin, muốn thử vai khách hàng).
        //
        // ⚠️ Trước đây chỗ này đá thẳng về /tai-khoan (sửa 2026-07-28) nên không có cách nào tới
        // được form, trừ khi tự biết đường đăng xuất — cùng loại bẫy với /admin/login. Nay vẫn
        // hiện form, chỉ thêm dải thông báo cho biết đang đăng nhập bằng ai.
        SetCurrentAccountNotice();

        ViewData["Title"] = "Đăng nhập";
        ViewData["MetaDescription"] = "Đăng nhập tài khoản để mua hàng và theo dõi đơn hàng của bạn.";
        return View(new LoginInput { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// Đưa tên tài khoản đang đăng nhập (nếu có) sang view để hiện dải nhắc "bạn đang là ai".
    /// Không có ai đăng nhập ⇒ null ⇒ view ẩn hẳn dải đó.
    /// </summary>
    private void SetCurrentAccountNotice() =>
        ViewData["CurrentAccount"] = User.Identity?.IsAuthenticated == true ? User.DisplayName() : null;

    [HttpPost("/dang-nhap")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Đăng nhập";
            return View(input);
        }

        var result = await TrySignInAsync(input.Phone, input.Password, input.RememberMe);
        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewData["Title"] = "Đăng nhập";
            return View(input);
        }

        return RedirectAfterAuth(input.ReturnUrl, result.IsAdmin);
    }

    [HttpGet("/dang-ky")]
    public IActionResult Register(string? returnUrl = null)
    {
        // Cùng lý do với Login: đang đăng nhập vẫn có thể muốn tạo tài khoản khác (vd chủ shop
        // dựng thử một tài khoản khách để kiểm tra giao diện phía người mua).
        SetCurrentAccountNotice();

        ViewData["Title"] = "Đăng ký tài khoản";
        ViewData["MetaDescription"] = "Tạo tài khoản để mua hàng, lưu địa chỉ và theo dõi đơn hàng.";
        return View(new RegisterInput { ReturnUrl = returnUrl });
    }

    [HttpPost("/dang-ky")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Đăng ký tài khoản";
            return View(input);
        }

        var result = await TryRegisterAsync(input);
        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewData["Title"] = "Đăng ký tài khoản";
            return View(input);
        }

        return RedirectAfterAuth(input.ReturnUrl);
    }

    /// <param name="returnUrl">
    /// Nơi quay về sau khi đăng xuất. Dùng cho trường hợp "đang đăng nhập nhầm tài khoản":
    /// trang 403 gửi kèm <c>/admin/login</c> để người dùng đăng xuất xong là tới thẳng form
    /// đăng nhập quản trị, không phải tự mò đường. Vẫn qua <c>Url.IsLocalUrl</c> chống open redirect.
    /// </param>
    [HttpPost("/dang-xuat")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await signInManager.SignOutAsync();

        // Giỏ hàng đã nằm trong DB theo tài khoản; không xóa gì cả để lần đăng nhập sau còn nguyên.
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return Redirect("/");
    }

    // ───────────────────────────── Endpoint JSON cho hộp thoại ─────────────────────────────

    [HttpPost("/tai-khoan/dang-nhap-ajax")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginAjax([FromBody] LoginAjaxRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Json(new { ok = false, message = "Vui lòng nhập số điện thoại và mật khẩu." });
        }

        var result = await TrySignInAsync(request.Phone.Trim(), request.Password, request.RememberMe);

        return Json(new
        {
            ok = result.Ok,
            message = result.Message,
            displayName = result.DisplayName,
            isAdmin = result.IsAdmin
        });
    }

    [HttpPost("/tai-khoan/dang-ky-ajax")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterAjax([FromBody] RegisterAjaxRequest? request)
    {
        if (request is null)
        {
            return Json(new { ok = false, message = "Dữ liệu không hợp lệ." });
        }

        var input = new RegisterInput
        {
            FullName = (request.FullName ?? "").Trim(),
            Phone = (request.Phone ?? "").Trim(),
            Email = request.Email,
            Password = request.Password ?? "",
            ConfirmPassword = request.ConfirmPassword ?? "",
            Website = request.Website
        };

        // Hộp thoại không đi qua model binding nên DataAnnotations chưa chạy — validate tay ở đây
        // để hai cửa vào có CÙNG bộ luật (Lưu ý: đây là chốt chặn thật, không phải trang trí).
        if (input.FullName.Length is < 2 or > 150)
        {
            return Json(new { ok = false, message = "Họ tên cần từ 2 đến 150 ký tự." });
        }
        if (input.Password.Length < 6)
        {
            return Json(new { ok = false, message = "Mật khẩu cần ít nhất 6 ký tự." });
        }
        if (input.Password != input.ConfirmPassword)
        {
            return Json(new { ok = false, message = "Hai mật khẩu không khớp nhau." });
        }

        var result = await TryRegisterAsync(input);

        return Json(new { ok = result.Ok, message = result.Message, displayName = result.DisplayName });
    }

    // ───────────────────────────── Phần lõi dùng chung ─────────────────────────────

    /// <summary>
    /// Đăng nhập bằng SỐ ĐIỆN THOẠI (chính là <c>UserName</c> của tài khoản khách).
    /// Admin dùng username chữ nên cũng đăng nhập được ở đây — tiện khi họ vừa quản trị vừa
    /// xem web khách; cờ <c>IsAdmin</c> để chuyển hướng thẳng vào khu quản trị.
    /// </summary>
    private async Task<AuthResult> TrySignInAsync(string phone, string password, bool rememberMe)
    {
        var normalized = NormalizePhone(phone);
        var user = await userManager.FindByNameAsync(normalized);

        if (user is not null)
        {
            var result = await signInManager.PasswordSignInAsync(
                user, password, rememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Gộp giỏ NGAY sau khi đăng nhập, truyền userId tường minh (xem ghi chú ở
                // ICartService.MergeGuestCartAsync). Bọc try/catch: giỏ hỏng không được biến
                // một lần đăng nhập thành công thành trang lỗi.
                try
                {
                    await cart.MergeGuestCartAsync(user.Id);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Không gộp được giỏ hàng vãng lai cho tài khoản {UserId}", user.Id);
                }

                var isAdmin = await userManager.IsInRoleAsync(user, AppRoles.Admin);
                logger.LogInformation("Khách {User} đăng nhập thành công", normalized);

                return new AuthResult(true, "Đăng nhập thành công.", DisplayNameOf(user), isAdmin);
            }

            if (result.IsLockedOut)
            {
                // Hai loại khóa rất khác nhau với người dùng, phải nói khác nhau:
                //  • Identity tự khóa 10 phút sau 5 lần sai  ⇒ chờ là vào lại được.
                //  • Admin khóa tài khoản (mốc năm 9999)     ⇒ chờ vô ích, phải gọi shop.
                // Không tách ra thì khách bị khóa sẽ đọc được câu "thử lại sau 2147483647 phút".
                var lockedByAdmin = user.LockoutEnd > DateTimeOffset.Now.AddDays(365);

                if (lockedByAdmin)
                {
                    return new AuthResult(false,
                        "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ hotline của shop để được hỗ trợ.");
                }

                var minutes = user.LockoutEnd.HasValue
                    ? Math.Max(1, (int)Math.Ceiling((user.LockoutEnd.Value - DateTimeOffset.Now).TotalMinutes))
                    : 10;

                return new AuthResult(false,
                    $"Tài khoản đã bị tạm khóa do nhập sai quá nhiều lần. Vui lòng thử lại sau {minutes} phút.");
            }
        }

        // Sai số điện thoại hay sai mật khẩu đều báo CÙNG MỘT CÂU — nói rõ "chưa có tài khoản này"
        // là tự xác nhận cho người đang dò rằng số nào đã đăng ký (giống trang đăng nhập admin).
        logger.LogWarning("Đăng nhập thất bại cho số {Phone}", normalized);
        return new AuthResult(false, "Số điện thoại hoặc mật khẩu không đúng.");
    }

    private async Task<AuthResult> TryRegisterAsync(RegisterInput input)
    {
        // Honeypot: người thật không nhìn thấy ô này nên không bao giờ điền. Báo thành công giả
        // để bot không biết mình bị chặn, nhưng KHÔNG tạo tài khoản nào (giống form liên hệ).
        if (!string.IsNullOrWhiteSpace(input.Website))
        {
            logger.LogWarning("Chặn đăng ký nghi bot (honeypot) cho số {Phone}", input.Phone);
            return new AuthResult(true, "Đăng ký thành công!");
        }

        var phone = NormalizePhone(input.Phone);
        var email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();

        // Kiểm định dạng SĐT Ở ĐÂY chứ không chỉ bằng DataAnnotations của trang: hộp thoại gửi
        // JSON nên không đi qua model binding — thiếu dòng này là cửa AJAX lọt số rác vào DB.
        if (!PhoneFormat().IsMatch(phone))
        {
            return new AuthResult(false, PhoneRules.Message);
        }

        if (email is not null && !MailAddress.TryCreate(email, out _))
        {
            return new AuthResult(false, "Email không hợp lệ.");
        }

        if (await userManager.FindByNameAsync(phone) is not null)
        {
            return new AuthResult(false,
                "Số điện thoại này đã được đăng ký. Bạn hãy đăng nhập hoặc dùng số khác nhé.");
        }

        // Email là tùy chọn nên Identity đã tắt RequireUniqueEmail — tự kiểm ở đây để vẫn không
        // thể có hai tài khoản chung một email (cần thiết nếu sau này làm khôi phục qua email).
        if (email is not null && await userManager.FindByEmailAsync(email) is not null)
        {
            return new AuthResult(false, "Email này đã được dùng cho một tài khoản khác.");
        }

        var user = new ApplicationUser
        {
            UserName = phone,
            PhoneNumber = phone,
            PhoneNumberConfirmed = false, // chưa có OTP — cố ý không tự nhận là đã xác minh
            Email = email,
            FullName = input.FullName.Trim(),
            CreatedAt = DateTime.Now
        };

        var created = await userManager.CreateAsync(user, input.Password);
        if (!created.Succeeded)
        {
            var message = string.Join(" ", created.Errors.Select(e => Translate(e.Code, e.Description)));
            return new AuthResult(false, message);
        }

        await userManager.AddToRoleAsync(user, AppRoles.Customer);
        await signInManager.SignInAsync(user, isPersistent: true);

        try
        {
            await cart.MergeGuestCartAsync(user.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không gộp được giỏ hàng vãng lai cho tài khoản mới {UserId}", user.Id);
        }

        logger.LogInformation("Tài khoản khách mới: {Phone}", phone);
        return new AuthResult(true, "Đăng ký thành công!", DisplayNameOf(user));
    }

    /// <summary>
    /// Chuẩn hóa SĐT trước khi tra/tạo tài khoản: bỏ khoảng trắng, dấu chấm, gạch nối mà người
    /// dùng hay gõ ("0912 345 678", "0912.345.678"). Không có bước này thì cùng một số có thể
    /// tạo ra nhiều tài khoản khác nhau và khách đăng nhập lại không được.
    /// </summary>
    private static string NormalizePhone(string phone) =>
        new(phone.Where(c => !char.IsWhiteSpace(c) && c is not ('.' or '-' or '(' or ')')).ToArray());

    private static string DisplayNameOf(ApplicationUser user) =>
        string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? "Bạn" : user.FullName;

    /// <summary>Đổi thông báo lỗi tiếng Anh của Identity sang tiếng Việt cho những mã hay gặp.</summary>
    private static string Translate(string code, string fallback) => code switch
    {
        "PasswordTooShort" => "Mật khẩu cần ít nhất 6 ký tự.",
        "DuplicateUserName" => "Số điện thoại này đã được đăng ký.",
        "DuplicateEmail" => "Email này đã được dùng cho một tài khoản khác.",
        "InvalidUserName" => "Số điện thoại chứa ký tự không hợp lệ.",
        "InvalidEmail" => "Email không hợp lệ.",
        _ => fallback
    };

    /// <summary>
    /// Sau khi đăng nhập/đăng ký thì đi đâu. <c>Url.IsLocalUrl</c> loại mọi URL tuyệt đối
    /// (kể cả "//evil.com" trông như đường dẫn tương đối) — chống open redirect.
    /// </summary>
    private IActionResult RedirectAfterAuth(string? returnUrl, bool isAdmin = false)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return Redirect(isAdmin ? "/admin" : "/tai-khoan");
    }

    private record AuthResult(bool Ok, string Message, string? DisplayName = null, bool IsAdmin = false);
}

/// <summary>Body JSON của hộp thoại đăng nhập.</summary>
public class LoginAjaxRequest
{
    public string? Phone { get; set; }
    public string? Password { get; set; }
    public bool RememberMe { get; set; } = true;
}

/// <summary>Body JSON của hộp thoại đăng ký.</summary>
public class RegisterAjaxRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }

    /// <summary>Honeypot chống bot — người thật không điền.</summary>
    public string? Website { get; set; }
}
