namespace RenewZone.Domain.Enums;

/// <summary>
/// Các "vùng cache" của web khách. Dùng dạng cờ (flags) vì một entity thường ảnh hưởng
/// nhiều vùng cùng lúc — vd sửa <c>Category</c> làm hỏng cả menu layout, tile trang chủ và bản đồ slug.
/// </summary>
[Flags]
public enum CacheScope
{
    None = 0,

    /// <summary>ISiteContentService — settings + cây danh mục cho layout (topbar, header, footer).</summary>
    SiteContent = 1,

    /// <summary>INavigationService — menu chính, offcanvas mobile, 2 cột link footer.</summary>
    Navigation = 2,

    /// <summary>IContentBlockService — cam kết PDP, trấn an giỏ/checkout, tile /lien-he.</summary>
    ContentBlocks = 4,

    /// <summary>ISlugResolver — bản đồ slug → loại nội dung của fallback route "{slug}".</summary>
    Slugs = 8,

    /// <summary>IHomeQueryService — các section trang chủ.</summary>
    Home = 16
}
