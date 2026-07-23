using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Web.Models;

namespace RenewZone.Web.Controllers;

/// <summary>
/// Trang lỗi tùy chỉnh (Giai đoạn 7). Được gọi qua re-execute từ pipeline (Program.cs):
///   • <c>UseStatusCodePagesWithReExecute("/loi/{0}")</c> cho 404/403... (GIỮ status code + URL gốc)
///   • <c>UseExceptionHandler("/loi/500")</c> cho exception chưa bắt.
/// KHÔNG thuộc Area Admin. Route 2 đoạn (/loi/{code}) nên không lọt fallback "{slug}".
/// </summary>
public class ErrorController(ISiteContentService siteContent) : Controller
{
    [Route("/loi/{code:int}")]
    public async Task<IActionResult> Status(int code)
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        // 500+ : lỗi máy chủ — layout tối giản tự chủ (DB có thể đang chết)
        if (code >= 500)
        {
            // Giữ đúng 500 trên response (re-execute có thể đã đổi). Thử đọc hotline thật, hỏng thì
            // dùng hằng — trang 500 phải render được kể cả khi DB lỗi.
            Response.StatusCode = code;
            var hotline = "0900.000.000";
            try
            {
                var settings = await siteContent.GetSettingsAsync();
                var v = settings.GetValueOrDefault(SettingKeys.Hotline);
                if (!string.IsNullOrWhiteSpace(v)) hotline = v;
            }
            catch
            {
                // DB/service đang lỗi — im lặng dùng hằng, tuyệt đối không để trang 500 tự ném.
            }

            return View("ServerError", new ErrorViewModel
            {
                RequestId = requestId,
                StatusCode = code,
                Hotline = hotline
            });
        }

        Response.StatusCode = code;

        // 404 : không tìm thấy — DB đang sống, dùng full layout để khách tìm lại đường
        if (code == 404)
        {
            return View("NotFound", new ErrorViewModel { RequestId = requestId, StatusCode = code });
        }

        // 403/400/... : trang chung, dùng lại giao diện 404 với thông điệp khác
        return View("GenericError", new ErrorViewModel { RequestId = requestId, StatusCode = code });
    }
}
