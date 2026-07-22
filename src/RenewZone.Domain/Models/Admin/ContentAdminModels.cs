using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Models.Admin;

// ===================== Bài viết =====================

public class NewsListRequest : AdminListRequest
{
    public int? CategoryId { get; init; }
    public bool? IsActive { get; init; }
}

public record NewsListItem(
    int Id, string Title, string Slug, string? ThumbnailUrl,
    string? CategoryName, bool IsFeatured, bool IsActive,
    DateTime PublishedAt, int ViewCount);

public class NewsInput
{
    public int? Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public int? CategoryId { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? Excerpt { get; init; }
    public required string ContentHtml { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsActive { get; init; }
    public DateTime PublishedAt { get; init; }
}

// ===================== Chuyên mục tin =====================

public record NewsCategoryItem(int Id, string Name, string Slug, int DisplayOrder, bool IsActive, int ArticleCount);

public class NewsCategoryInput
{
    public int? Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}

// ===================== Trang tĩnh =====================

public record StaticPageItem(int Id, string Title, string Slug, bool IsActive, DateTime? UpdatedAt);

public class StaticPageInput
{
    public int? Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string ContentHtml { get; init; }
    public bool IsActive { get; init; }
}

// ===================== Banner =====================

public record BannerItem(int Id, string? Title, string ImageUrl, string? LinkUrl,
    BannerPosition Position, int DisplayOrder, bool IsActive);

public class BannerInput
{
    public int? Id { get; init; }
    public string? Title { get; init; }
    public required string ImageUrl { get; init; }
    public string? LinkUrl { get; init; }
    public BannerPosition Position { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}

// ===================== Đánh giá khách hàng =====================

public record TestimonialItem(int Id, string CustomerName, string? AvatarUrl, string Content,
    int DisplayOrder, bool IsActive);

public class TestimonialInput
{
    public int? Id { get; init; }
    public required string CustomerName { get; init; }
    public string? AvatarUrl { get; init; }
    public required string Content { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}

// ===================== Menu =====================

public record MenuTreeNode(
    int Id, MenuGroup Group, MenuItemType Type, string Text, string? Url, string? Icon,
    int DisplayOrder, bool IsActive, bool OpenInNewTab,
    IReadOnlyList<MenuTreeNode> Children)
{
    /// <summary>
    /// URL trỏ tới slug không tồn tại ⇒ admin cần được cảnh báo NGAY tại màn menu.
    /// Đây chính là loại lỗi Giai đoạn 2–4 từng mắc (menu trỏ /sua-chua khi chưa seed trang).
    /// null = không kiểm được (link ngoài, trống, hoặc mục ProductMega).
    /// </summary>
    public bool? IsBroken { get; init; }
}

public class MenuItemInput
{
    public int? Id { get; init; }
    public MenuGroup Group { get; init; }
    public MenuItemType Type { get; init; }
    public required string Text { get; init; }
    public string? Url { get; init; }
    public string? Icon { get; init; }
    public int? ParentId { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public bool OpenInNewTab { get; init; }
}

// ===================== Khối nội dung =====================

public class ContentBlockInput
{
    public int? Id { get; init; }
    public required string GroupKey { get; init; }
    public string? Icon { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public string? LinkUrl { get; init; }
    public string? ImageUrl { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}

// ===================== Hộp thư =====================

public class ContactListRequest : AdminListRequest
{
    public bool? IsRead { get; init; }
    public string? Subject { get; init; }
}

public record ContactListItem(
    int Id, string FullName, string Phone, string? Email, string? Address,
    string? Subject, string Content, string? AttachmentPath, bool IsRead, DateTime CreatedAt);

// ===================== Thùng rác =====================

/// <summary>Các loại nội dung có thể nằm trong Thùng rác — dùng làm tab và tham số route.</summary>
public enum TrashType
{
    Product,
    Category,
    NewsArticle,
    NewsCategory,
    StaticPage,
    Banner,
    Testimonial,
    MenuItem,
    ContentBlock,
    ContactMessage
}

public record TrashItem(int Id, TrashType Type, string Name, string? Extra, DateTime? DeletedAt);
