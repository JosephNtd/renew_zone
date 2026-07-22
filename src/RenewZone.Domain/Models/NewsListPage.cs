using RenewZone.Domain.Entities;

namespace RenewZone.Domain.Models;

/// <summary>Dữ liệu trang /tin-tuc: khối nổi bật + danh sách phân trang + chuyên mục để lọc.</summary>
public class NewsListPage
{
    /// <summary>
    /// Bài nổi bật — CHỈ có ở trang 1 khi không lọc chuyên mục và không tìm kiếm.
    /// Các bài này đã bị loại khỏi <see cref="Articles"/> để không hiện 2 lần.
    /// </summary>
    public IReadOnlyList<NewsArticle> Featured { get; init; } = [];

    public PagedResult<NewsArticle> Articles { get; init; } = new();

    /// <summary>Toàn bộ chuyên mục đang bật, kèm số bài để render chip "Thiết bị (3)".</summary>
    public IReadOnlyList<NewsCategoryCount> Categories { get; init; } = [];

    /// <summary>Chuyên mục đang lọc; null nếu không lọc hoặc slug không tồn tại.</summary>
    public NewsCategory? ActiveCategory { get; init; }

    public string? Keyword { get; init; }

    /// <summary>Đang ở chế độ lọc/tìm kiếm ⇒ view hiện dòng kết quả + nút xóa bộ lọc.</summary>
    public bool IsFiltered => ActiveCategory is not null || !string.IsNullOrWhiteSpace(Keyword);
}

/// <summary>Chuyên mục kèm số bài viết đang hiển thị (đếm từ DB, không hardcode).</summary>
public class NewsCategoryCount
{
    public required NewsCategory Category { get; init; }
    public required int ArticleCount { get; init; }
}
