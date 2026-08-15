using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RenewZone.Infrastructure.Identity;
using RenewZone.Web.Areas.Admin.Models;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// Lớp cơ sở của MỌI controller trong Area Admin.
/// <para>
/// <b>Vì sao gom [Authorize] về đây thay vì gắn từng controller:</b> quên gắn ở một controller là
/// hở toàn bộ màn hình đó, và không có gì trong quá trình build hay test báo cho biết. Kế thừa
/// thì mặc định là AN TOÀN — muốn mở public phải cố ý gắn <c>[AllowAnonymous]</c>, một hành động
/// nhìn thấy được khi đọc code.
/// </para>
/// <para>
/// <c>[AutoValidateAntiforgeryToken]</c> bảo vệ MỌI POST/PUT/DELETE kể cả khi quên gắn
/// <c>[ValidateAntiForgeryToken]</c> — AJAX phải gửi token qua header <c>RequestVerificationToken</c>.
/// </para>
/// </summary>
/// <remarks>
/// ⚠️ <c>AuthenticationSchemes</c> là BẮT BUỘC từ 2026-07-28: khu quản trị có cookie riêng
/// (<see cref="AppAuthSchemes.Admin"/>). Thiếu nó thì <c>[Authorize]</c> sẽ soi cookie của KHÁCH
/// HÀNG — tức là admin đã đăng nhập vẫn bị đá ra, còn khách thì không bao giờ vào được.
/// Khai ở lớp cơ sở nên mọi controller admin tự có; thêm màn mới không phải nhớ gì.
/// </remarks>
[Area("Admin")]
[Authorize(AuthenticationSchemes = AppAuthSchemes.Admin, Roles = AppRoles.Admin)]
[AutoValidateAntiforgeryToken]
public abstract class BaseAdminController : Controller
{
    protected void ToastSuccess(string message) => SetToast("success", message);

    protected void ToastError(string message) => SetToast("error", message);

    protected void ToastWarning(string message) => SetToast("warning", message);

    private void SetToast(string type, string message)
    {
        TempData[AdminToast.TempDataKey] = JsonSerializer.Serialize(
            new AdminToast { Type = type, Message = message });
    }
}
