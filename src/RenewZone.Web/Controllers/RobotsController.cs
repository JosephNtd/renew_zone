using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RenewZone.Web.Models.Options;

namespace RenewZone.Web.Controllers;

/// <summary>
/// robots.txt động (Giai đoạn 7). Controller động (không phải file tĩnh) vì dòng Sitemap cần
/// host TUYỆT ĐỐI dựng từ request. Route có dấu "." nên thắng fallback "{slug}".
/// </summary>
public class RobotsController(IOptions<SeoOptions> seoOptions) : Controller
{
    private readonly SeoOptions _seo = seoOptions.Value;

    [HttpGet("/robots.txt")]
    public IActionResult Index()
    {
        var origin = $"{Request.Scheme}://{Request.Host}";

        // Môi trường demo: chặn TOÀN BỘ và không công bố sitemap — trả sớm để không lẫn với
        // các dòng Allow bên dưới (crawler khớp rule cụ thể nhất, dễ hiểu nhầm nếu trộn).
        if (!_seo.AllowIndexing)
        {
            var demo = new StringBuilder();
            demo.AppendLine("# Ban demo/hoc tap - khong danh cho cong cu tim kiem");
            demo.AppendLine("User-agent: *");
            demo.AppendLine("Disallow: /");
            return Content(demo.ToString(), "text/plain", Encoding.UTF8);
        }

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
