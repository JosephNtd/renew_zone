using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Bài viết "Tin công nghệ". IsFeatured ⇒ nằm khối grid nổi bật đầu trang /tin-tuc.
/// </summary>
public class NewsArticle : AuditableEntity
{
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }

    /// <summary>Đoạn trích 2–3 câu hiển thị ở listing.</summary>
    public string? Excerpt { get; set; }

    public string ContentHtml { get; set; } = null!;

    /// <summary>Tag chuyên mục dạng chuỗi đơn giản, vd "Khoa học công nghệ", "Thiết bị".</summary>
    public string? CategoryTag { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime PublishedAt { get; set; }
    public int ViewCount { get; set; }
}
