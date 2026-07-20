namespace RenewZone.Domain.Enums;

/// <summary>
/// Loại nội dung mà một slug phẳng ở root trỏ tới. Thứ tự khai báo CŨNG LÀ thứ tự ưu tiên
/// khi có slug trùng giữa các bảng (xem CLAUDE.md mục 4.3 / Lưu ý 6.4).
/// </summary>
public enum SlugTargetType
{
    Category = 1,
    Product = 2,
    NewsArticle = 3,
    StaticPage = 4
}
