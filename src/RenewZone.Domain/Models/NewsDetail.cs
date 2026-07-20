using RenewZone.Domain.Entities;

namespace RenewZone.Domain.Models;

/// <summary>Bài viết chi tiết kèm các bài đọc thêm hiển thị cuối trang.</summary>
public class NewsDetail
{
    public NewsArticle Article { get; init; } = null!;

    /// <summary>Bài cùng CategoryTag, thiếu thì bù bằng bài mới nhất.</summary>
    public IReadOnlyList<NewsArticle> Related { get; init; } = [];
}
