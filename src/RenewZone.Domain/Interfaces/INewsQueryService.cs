using RenewZone.Domain.Entities;
using RenewZone.Domain.Models;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Truy vấn cho trang /tin-tuc. KHÔNG cache (giống <see cref="ICatalogQueryService"/>) —
/// dữ liệu phụ thuộc querystring nên cache theo trang là vô nghĩa.
/// </summary>
public interface INewsQueryService
{
    Task<NewsListPage> GetListAsync(string? categorySlug, string? keyword, int page, int pageSize);

    Task<IReadOnlyList<NewsCategory>> GetCategoriesAsync();
}
