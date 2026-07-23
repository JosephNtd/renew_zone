using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace RenewZone.Web.Controllers;

/// <summary>
/// robots.txt động (Giai đoạn 7). Controller động (không phải file tĩnh) vì dòng Sitemap cần
/// host TUYỆT ĐỐI dựng từ request. Route có dấu "." nên thắng fallback "{slug}".
/// </summary>
public class RobotsController : Controller
{
    [HttpGet("/robots.txt")]
    public IActionResult Index()
    {
        var origin = $"{Request.Scheme}://{Request.Host}";

        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        // Trang quản trị + giao dịch + kết quả tìm kiếm không nên vào index
        sb.AppendLine("Disallow: /admin/");
        sb.AppendLine("Disallow: /gio-hang");
        sb.AppendLine("Disallow: /dat-hang");
        sb.AppendLine("Disallow: /tim-kiem");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {origin}/sitemap.xml");

        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }
}
