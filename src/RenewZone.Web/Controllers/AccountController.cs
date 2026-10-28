using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Account;
using RenewZone.Infrastructure.Identity;
using RenewZone.Web.Helpers;
using RenewZone.Web.Models;
using RenewZone.Web.Models.ViewModels;
using RenewZone.Web.Services;

namespace RenewZone.Web.Controllers;

/// <summary>
/// Khu tài khoản của khách hàng: <c>/tai-khoan/...</c> (Giai đoạn 9).
/// <para>
/// <c>[Authorize]</c> ở CẤP CLASS nên mặc định là AN TOÀN — thêm action mới sau này tự động được
/// bảo vệ, muốn mở cho khách vãng lai phải cố ý gắn <c>[AllowAnonymous]</c> (cùng triết lý
/// <c>BaseAdminController</c>). Chưa đăng nhập ⇒ cookie event ở Program.cs đá về <c>/dang-nhap</c>
/// kèm <c>returnUrl</c>, nên đăng nhập xong khách quay lại đúng trang đang muốn xem.
/// </para>
/// <para>
/// ⚠️ MỌI truy vấn đều đi qua service kèm <see cref="UserId"/>. Không action nào được nhận Id
/// bản ghi rồi đọc thẳng — đó là cách sinh ra lỗi IDOR (đổi số trên URL để xem đơn người khác).
/// </para>
/// </summary>
[Authorize]
[AutoValidateAntiforgeryToken]
public class AccountController(
    ICustomerOrderService orders,
    ICustomerAddressService addresses,
    IWishlistService wishlist,
    ICartService cart,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    private const int OrdersPageSize = 10;

    /// <summary>Id tài khoản đang đăng nhập. Đã qua <c>[Authorize]</c> nên luôn có giá trị.</summary>
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ───────────────────────────── Tổng quan ─────────────────────────────

    [HttpGet("/tai-khoan")]
    public async Task<IActionResult> Index()
    {
        SetSeo("Tài khoản của tôi");

        return View(new AccountDashboardViewModel
        {
            DisplayName = User.DisplayName(),
            Summary = await orders.GetSummaryAsync(UserId)
        });
    }

    // ───────────────────────────── Hồ sơ ─────────────────────────────

    [HttpGet("/tai-khoan/ho-so")]
    public async Task<IActionResult> Profile()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return await ForceSignOutAsync();
        }

        SetSeo("Thông tin cá nhân");

        return View(new ProfileInput
        {
            FullName = user.FullName ?? "",
            Phone = user.PhoneNumber ?? user.UserName ?? "",
            Email = user.Email,
            DateOfBirth = user.DateOfBirth,
            Gender = user.Gender
        });
    }

    [HttpPost("/tai-khoan/ho-so")]
    public async Task<IActionResult> Profile(ProfileInput input)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return await ForceSignOutAsync();
        }

        if (!ModelState.IsValid)
        {
            SetSeo("Thông tin cá nhân");
            return View(input);
        }

        var phone = new string(input.Phone.Where(c => !char.IsWhiteSpace(c) && c is not ('.' or '-')).ToArray());
        var email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();

        // SĐT vừa là số liên lạc VỪA là tên đăng nhập ⇒ đổi số là đổi cách đăng nhập.
        // Phải kiểm trùng trước, nếu không sẽ vỡ ở unique index của Identity với thông báo thô.
        if (!string.Equals(phone, user.UserName, StringComparison.Ordinal))
        {
            if (await userManager.FindByNameAsync(phone) is not null)
            {
                ModelState.AddModelError(nameof(input.Phone),
                    "Số điện thoại này đã thuộc về một tài khoản khác.");
                SetSeo("Thông tin cá nhân");
                return View(input);
            }

            user.UserName = phone;
            user.NormalizedUserName = userManager.NormalizeName(phone);
            user.PhoneNumber = phone;
        }

        if (email is not null && !string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                ModelState.AddModelError(nameof(input.Email),
                    "Email này đã thuộc về một tài khoản khác.");
                SetSeo("Thông tin cá nhân");
                return View(input);
            }
        }

        user.FullName = input.FullName.Trim();
        user.Email = email;
        user.NormalizedEmail = email is null ? null : userManager.NormalizeEmail(email);
        user.DateOfBirth = input.DateOfBirth;
        user.Gender = string.IsNullOrWhiteSpace(input.Gender) ? null : input.Gender;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty,
                string.Join(" ", result.Errors.Select(e => e.Description)));
            SetSeo("Thông tin cá nhân");
            return View(input);
        }

        // Cookie đang mang tên đăng nhập CŨ. Không làm mới thì thanh trên cùng vẫn hiện tên cũ
        // cho tới lần đăng nhập sau — và nếu đổi UserName thì cookie còn có thể mất hiệu lực.
        await signInManager.RefreshSignInAsync(user);

        TempData["AccountMessage"] = "Đã cập nhật thông tin cá nhân.";
        return RedirectToAction(nameof(Profile));
    }

    // ───────────────────────────── Đổi mật khẩu ─────────────────────────────

    [HttpGet("/tai-khoan/doi-mat-khau")]
    public IActionResult ChangePassword()
    {
        SetSeo("Đổi mật khẩu");
        return View(new ChangePasswordInput());
    }

    [HttpPost("/tai-khoan/doi-mat-khau")]
    public async Task<IActionResult> ChangePassword(ChangePasswordInput input)
    {
        if (!ModelState.IsValid)
        {
            SetSeo("Đổi mật khẩu");
            return View(input);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return await ForceSignOutAsync();
        }

        var result = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Errors.Any(e => e.Code == "PasswordMismatch")
                ? "Mật khẩu hiện tại không đúng."
                : string.Join(" ", result.Errors.Select(e => e.Description)));

            SetSeo("Đổi mật khẩu");
            return View(input);
        }

        // Đổi mật khẩu làm SecurityStamp thay đổi ⇒ cookie hiện tại hết hiệu lực. Không refresh
        // thì khách bị đá ra ngoài ngay sau khi đổi thành công — trông y như hệ thống lỗi.
        await signInManager.RefreshSignInAsync(user);

        TempData["AccountMessage"] = "Đã đổi mật khẩu thành công.";
        return RedirectToAction(nameof(ChangePassword));
    }

    // ───────────────────────────── Đơn hàng ─────────────────────────────

    [HttpGet("/tai-khoan/don-hang")]
    public async Task<IActionResult> Orders(int trang = 1)
    {
        var page = trang < 1 ? 1 : trang;

        SetSeo("Đơn hàng của tôi");

        return View(new MyOrdersViewModel
        {
            Orders = await orders.GetMyOrdersAsync(UserId, page, OrdersPageSize)
        });
    }

    [HttpGet("/tai-khoan/don-hang/{code}")]
    public async Task<IActionResult> OrderDetail(string code)
    {
        var detail = await orders.GetMyOrderAsync(UserId, code);
        if (detail is null)
        {
            // Đơn của người khác trả 404 y như đơn không tồn tại — không xác nhận mã nào có thật.
            return NotFound();
        }

        SetSeo($"Đơn hàng {detail.Code}");
        return View(detail);
    }

    [HttpPost("/tai-khoan/don-hang/{code}/huy")]
    public async Task<IActionResult> CancelOrder(string code, [FromBody] CancelOrderRequest? request)
    {
        var result = await orders.CancelMyOrderAsync(UserId, code, request?.Reason);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    /// <summary>Khách chấm sao việc giao hàng của đơn đã hoàn thành (Giai đoạn 10). Service tự kiểm quyền.</summary>
    [HttpPost("/tai-khoan/don-hang/{code}/danh-gia-giao-hang")]
    public async Task<IActionResult> ReviewDelivery(string code, [FromBody] DeliveryReviewInput? input)
    {
        if (input is null)
        {
            return Json(new { ok = false, message = "Dữ liệu không hợp lệ." });
        }

        var result = await orders.SubmitDeliveryReviewAsync(UserId, code, new DeliveryReviewRequest(input.Rating, input.Comment));
        return Json(new { ok = result.Ok, message = result.Message });
    }

    [HttpPost("/tai-khoan/don-hang/{code}/mua-lai")]
    public async Task<IActionResult> Reorder(string code)
    {
        var lines = await orders.GetReorderLinesAsync(UserId, code);

        if (lines.Count == 0)
        {
            TempData["AccountError"] = "Không còn sản phẩm nào của đơn này đang được bán.";
            return RedirectToAction(nameof(OrderDetail), new { code });
        }

        var added = 0;
        foreach (var line in lines)
        {
            // Giá được TÍNH LẠI theo bảng giá hiện tại (Lưu ý 5c) — mua lại đơn cũ không giữ giá cũ.
            var result = await cart.AddAsync(line.ProductId, line.OptionValueIds, line.Quantity);
            if (result.Ok)
            {
                added++;
            }
        }

        if (added == 0)
        {
            TempData["AccountError"] = "Các sản phẩm của đơn này hiện không mua lại được.";
            return RedirectToAction(nameof(OrderDetail), new { code });
        }

        TempData["AccountMessage"] = added < lines.Count
            ? $"Đã thêm {added}/{lines.Count} sản phẩm vào giỏ (một số món không còn bán)."
            : $"Đã thêm {added} sản phẩm vào giỏ hàng.";

        return Redirect("/gio-hang");
    }

    // ───────────────────────────── Sổ địa chỉ ─────────────────────────────

    [HttpGet("/tai-khoan/dia-chi")]
    public async Task<IActionResult> Addresses()
    {
        SetSeo("Sổ địa chỉ");
        return View(await addresses.GetMyAddressesAsync(UserId));
    }

    [HttpPost("/tai-khoan/dia-chi/luu")]
    public async Task<IActionResult> SaveAddress(AddressFormInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["AccountError"] = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return RedirectToAction(nameof(Addresses));
        }

        var result = await addresses.SaveAsync(UserId, input.ToInput());
        SetFlash(result);
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("/tai-khoan/dia-chi/xoa")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        SetFlash(await addresses.DeleteAsync(UserId, id));
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("/tai-khoan/dia-chi/mac-dinh")]
    public async Task<IActionResult> SetDefaultAddress(int id)
    {
        SetFlash(await addresses.SetDefaultAsync(UserId, id));
        return RedirectToAction(nameof(Addresses));
    }

    // ───────────────────────────── Yêu thích ─────────────────────────────

    [HttpGet("/tai-khoan/yeu-thich")]
    public async Task<IActionResult> Wishlist()
    {
        SetSeo("Sản phẩm yêu thích");
        return View(await wishlist.GetMyWishlistAsync(UserId));
    }

    [HttpPost("/tai-khoan/yeu-thich/bat-tat")]
    public async Task<IActionResult> ToggleWishlist([FromBody] ToggleWishlistRequest? request)
    {
        if (request is null || request.ProductId <= 0)
        {
            return Json(new { ok = false, message = "Dữ liệu không hợp lệ." });
        }

        var result = await wishlist.ToggleAsync(UserId, request.ProductId);
        return Json(new
        {
            ok = result.Ok,
            isFavorite = result.IsFavorite,
            count = result.Count,
            message = result.Message
        });
    }

    // ───────────────────────────── Tiện ích ─────────────────────────────

    /// <summary>
    /// Đặt tiêu đề + CHẶN INDEX cho mọi trang trong khu tài khoản. Gom vào một chỗ vì thiếu
    /// noindex ở một trang thôi là dữ liệu riêng của khách có thể lọt vào kết quả tìm kiếm.
    /// </summary>
    private void SetSeo(string title) =>
        ViewData["Seo"] = new SeoData { Title = title, NoIndex = true };

    private void SetFlash(AccountOpResult result)
    {
        if (result.Ok)
        {
            TempData["AccountMessage"] = result.Message;
        }
        else
        {
            TempData["AccountError"] = result.Message;
        }
    }

    /// <summary>
    /// Cookie hợp lệ nhưng user không còn trong DB (admin xóa tài khoản trong lúc khách đang mở
    /// trang). Đăng xuất rồi về trang chủ — nếu cứ để chạy tiếp sẽ ném NullReference thành 500.
    /// </summary>
    private async Task<IActionResult> ForceSignOutAsync()
    {
        await signInManager.SignOutAsync();
        return Redirect("/");
    }
}

/// <summary>Body JSON của POST /tai-khoan/don-hang/{code}/danh-gia-giao-hang.</summary>
public class DeliveryReviewInput
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class CancelOrderRequest
{
    public string? Reason { get; set; }
}

public class ToggleWishlistRequest
{
    public int ProductId { get; set; }
}
