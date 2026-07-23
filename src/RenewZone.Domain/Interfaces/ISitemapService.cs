using RenewZone.Domain.Models;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Gom các URL công khai (IsActive) cho sitemap.xml. Implementation phải CACHE — sitemap query
/// nhiều bảng, không nên chạy mỗi lượt crawler ghé.
/// </summary>
public interface ISitemapService
{
    /// <summary>Danh sách URL (đường dẫn tương đối) đủ điều kiện index: danh mục/SP/tin/trang tĩnh active.</summary>
    Task<IReadOnlyList<SitemapEntry>> GetEntriesAsync();
}
