namespace RenewZone.Web.Models;

/// <summary>
/// Dữ liệu SEO cho một trang khách — controller/view gán qua <c>ViewData["Seo"]</c>.
/// Trang KHÔNG gán vẫn an toàn: <c>_SeoMeta</c> tự dựng mặc định từ SiteSettings
/// (StoreName + Tagline) và từ <c>ViewData["Title"]</c>/<c>["MetaDescription"]</c> cũ,
/// đúng triết lý Giai đoạn 5 (thiếu dữ liệu không được làm vỡ).
/// </summary>
public class SeoData
{
    public string? Title { get; set; }
    public string? Description { get; set; }

    /// <summary>Đường dẫn tương đối cho canonical (vd "/iphone-16"). Helper dựng thành absolute.
    /// Null ⇒ dùng <c>Request.Path</c> hiện tại (BỎ query để tránh trùng lặp nội dung).</summary>
    public string? CanonicalPath { get; set; }

    /// <summary>Ảnh share (đường dẫn tương đối hoặc absolute). Null ⇒ ảnh placeholder mặc định.</summary>
    public string? OgImage { get; set; }

    /// <summary>"website" (mặc định), "product" cho PDP, "article" cho tin.</summary>
    public string OgType { get; set; } = "website";

    /// <summary>Trang giao dịch/kết quả tìm kiếm không nên vào index của Google.</summary>
    public bool NoIndex { get; set; }
}
