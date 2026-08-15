using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Web.Areas.Admin.Models;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// Quản lý tài khoản khách hàng (Giai đoạn 9).
/// <para>
/// Phạm vi cố ý hẹp — xem / khóa / đặt lại mật khẩu; xem lý do ở <see cref="ICustomerAdminService"/>.
/// Kế thừa <c>BaseAdminController</c> nên đã có sẵn <c>[Area]</c> + <c>[Authorize(Admin)]</c> +
/// <c>[AutoValidateAntiforgeryToken]</c>.
/// </para>
/// </summary>
public class CustomerController(ICustomerAdminService customers) : BaseAdminController
{
    [HttpGet("/admin/khach-hang")]
    public async Task<IActionResult> Index(
        string? trangThai = null, string? q = null,
        string? sap = null, bool giam = false, int page = 1)
    {
        bool? isLocked = trangThai switch { "khoa" => true, "hoatDong" => false, _ => null };

        var result = await customers.GetListAsync(new CustomerListRequest
        {
            IsLocked = isLocked,
            Search = q,
            SortBy = sap,
            SortDesc = giam,
            Page = page,
            PageSize = 20
        });

        return View(new CustomerListViewModel
        {
            Result = result,
            IsLocked = isLocked,
            Search = q,
            SortBy = sap,
            SortDesc = giam
        });
    }

    [HttpGet("/admin/khach-hang/{id}")]
    public async Task<IActionResult> Detail(string id)
    {
        var detail = await customers.GetDetailAsync(id);
        if (detail is null)
        {
            return NotFound();
        }

        return View(detail);
    }

    [HttpPost("/admin/khach-hang/{id}/khoa")]
    public async Task<IActionResult> ToggleLock(string id, [FromBody] LockRequest? request)
    {
        var result = await customers.SetLockedAsync(id, request?.Locked ?? true);
        return Json(new { ok = result.Ok, message = result.Message, locked = request?.Locked ?? true });
    }

    [HttpPost("/admin/khach-hang/{id}/dat-lai-mat-khau")]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var result = await customers.ResetPasswordAsync(id);

        // Mật khẩu tạm đi qua JSON để hiện MỘT LẦN trong hộp thoại rồi thôi. Cố ý KHÔNG dùng
        // TempData/redirect: như thế mật khẩu sẽ nằm lại trong cookie session của trình duyệt.
        return Json(new { ok = result.Ok, message = result.Message, tempPassword = result.TempPassword });
    }
}

public class LockRequest
{
    public bool Locked { get; set; }
}
