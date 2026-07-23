using System.Globalization;
using System.Text.Json;

namespace RenewZone.Web.Helpers;

/// <summary>
/// Dựng chuỗi JSON-LD (schema.org) cho SEO — nói với Google/Facebook đúng thứ đã hiển thị cho người.
/// <para>
/// Build bằng <see cref="JsonSerializer"/> (KHÔNG nối chuỗi tay) để tên sản phẩm có dấu " hay ký tự
/// đặc biệt không làm vỡ script. Dùng encoder MẶC ĐỊNH (không Relaxed) — nó escape <c>&lt; &gt; &amp;</c>
/// thành <c><…</c> nên chuỗi <c>&lt;/script&gt;</c> trong dữ liệu KHÔNG thể phá vỡ thẻ
/// <c>&lt;script&gt;</c> bao ngoài. Ký tự tiếng Việt bị escape thành \uXXXX là JSON hợp lệ, Google đọc bình thường.
/// </para>
/// Khóa <c>@context</c>/<c>@type</c> có ký tự @ nên phải dùng Dictionary (không đặt được tên thuộc tính C#).
/// </summary>
public static class StructuredData
{
    /// <summary>Product cho PDP — cho rich result (giá + ảnh hiện ngay trong kết quả Google).</summary>
    public static string Product(
        string name, string? description, IEnumerable<string> images,
        decimal? price, bool isContactPrice, string url, string brand = "Apple")
    {
        var obj = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = name,
            ["image"] = images.Where(i => !string.IsNullOrWhiteSpace(i)).ToArray(),
            ["description"] = description,
            ["brand"] = new Dictionary<string, object?> { ["@type"] = "Brand", ["name"] = brand },
            ["url"] = url
        };

        // Sản phẩm "Liên hệ" (không công khai giá) ⇒ bỏ offers thay vì đánh dấu giá sai
        if (!isContactPrice && price is > 0)
        {
            obj["offers"] = new Dictionary<string, object?>
            {
                ["@type"] = "Offer",
                ["priceCurrency"] = "VND",
                ["price"] = price.Value.ToString("0", CultureInfo.InvariantCulture),
                ["availability"] = "https://schema.org/InStock",
                ["url"] = url
            };
        }

        return JsonSerializer.Serialize(obj);
    }

    /// <summary>Organization cho trang chủ — tên, logo, hotline, các trang social.</summary>
    public static string Organization(
        string name, string url, string logo, string? phone, IEnumerable<string> sameAs)
    {
        var obj = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Organization",
            ["name"] = name,
            ["url"] = url,
            ["logo"] = logo
        };

        if (!string.IsNullOrWhiteSpace(phone))
        {
            obj["contactPoint"] = new Dictionary<string, object?>
            {
                ["@type"] = "ContactPoint",
                ["telephone"] = phone,
                ["contactType"] = "customer service",
                ["areaServed"] = "VN",
                ["availableLanguage"] = "Vietnamese"
            };
        }

        var links = sameAs.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (links.Length > 0)
        {
            obj["sameAs"] = links;
        }

        return JsonSerializer.Serialize(obj);
    }

    /// <summary>BreadcrumbList — khớp breadcrumb đang hiển thị (PDP + trang danh mục).</summary>
    public static string Breadcrumb(IEnumerable<(string Name, string Url)> items)
    {
        var list = items.ToArray();
        var elements = list.Select((it, i) => (object)new Dictionary<string, object?>
        {
            ["@type"] = "ListItem",
            ["position"] = i + 1,
            ["name"] = it.Name,
            ["item"] = it.Url
        }).ToArray();

        var obj = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = elements
        };

        return JsonSerializer.Serialize(obj);
    }
}
