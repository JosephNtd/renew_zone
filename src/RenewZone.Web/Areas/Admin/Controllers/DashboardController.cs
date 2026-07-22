using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// Trang tổng quan. Quyền truy cập kế thừa từ <see cref="BaseAdminController"/>
/// ([Authorize(Roles = Admin)] — trước Giai đoạn 6 controller này cố ý để mở vì chưa có trang đăng nhập).
/// </summary>
public class DashboardController(IDashboardService dashboard) : BaseAdminController
{
    [HttpGet("/admin")]
    public async Task<IActionResult> Index()
    {
        return View(await dashboard.GetSummaryAsync());
    }
}
