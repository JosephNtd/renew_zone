using Microsoft.AspNetCore.Mvc;

namespace RenewZone.Web.Areas.Admin.Controllers;

// TODO (Giai đoạn 6): thêm [Authorize(Roles = AppRoles.Admin)] khi đã có trang đăng nhập + seed tài khoản
[Area("Admin")]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
