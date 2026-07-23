using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;

namespace RenewZone.Web.Controllers;

/// <summary>
/// sitemap.xml động (Giai đoạn 7). Route có dấu "." nên attribute route thắng fallback "{slug}".
/// XML build bằng <see cref="XDocument"/> (KHÔNG nối chuỗi tay) để tự escape ký tự đặc biệt.
/// </summary>
public class SitemapController(ISitemapService sitemap) : Controller
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Index()
    {
        var entries = await sitemap.GetEntriesAsync();
        var origin = $"{Request.Scheme}://{Request.Host}";

        var urlset = new XElement(Ns + "urlset",
            entries.Select(e =>
            {
                var url = new XElement(Ns + "url",
                    new XElement(Ns + "loc", origin + e.Path));

                if (e.LastModified is { } lastmod)
                {
                    url.Add(new XElement(Ns + "lastmod", lastmod.ToString("yyyy-MM-dd")));
                }

                url.Add(new XElement(Ns + "changefreq", e.ChangeFreq));
                url.Add(new XElement(Ns + "priority", e.Priority.ToString("0.0", CultureInfo.InvariantCulture)));
                return url;
            }));

        // XDocument.ToString() KHÔNG kèm khai báo <?xml?> — tự thêm dòng UTF-8 (StringWriter mặc định
        // UTF-16 nên không dùng XmlWriter+StringBuilder để tránh ghi encoding="utf-16" sai).
        var xml = new StringBuilder();
        xml.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        xml.Append(urlset);

        return Content(xml.ToString(), "application/xml", Encoding.UTF8);
    }
}
