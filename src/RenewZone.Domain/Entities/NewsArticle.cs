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

    /// <summary>Chuyên mục bài viết. Null = chưa phân loại (xóa chuyên mục ⇒ SetNull, không mất bài).</summary>
    public int? CategoryId { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime PublishedAt { get; set; }
    public int ViewCount { get; set; }

    public NewsCategory? Category { get; set; }
}
