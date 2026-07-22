using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Chuyên mục bài viết ("Thiết bị", "Thủ thuật"...). Thay <c>NewsArticle.CategoryTag</c> dạng
/// chuỗi tự do để admin quản lý được danh sách và trang /tin-tuc lọc theo slug.
/// ⚠️ Slug này KHÔNG nằm trong <c>ISlugResolver</c> (chỉ 4 bảng Category/Product/News/StaticPage) —
/// lọc đi bằng querystring <c>?chuyen-muc=</c> nên không đụng namespace slug toàn cục.
/// </summary>
public class NewsCategory : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<NewsArticle> Articles { get; set; } = [];
}
