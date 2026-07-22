using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <inheritdoc cref="INewsQueryService" />
public class NewsQueryService(AppDbContext db) : INewsQueryService
{
    /// <summary>Số bài trong khối nổi bật đầu trang 1 (1 bài chính + 2 bài phụ).</summary>
    private const int FeaturedCount = 3;

    public async Task<NewsListPage> GetListAsync(string? categorySlug, string? keyword, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        var trimmedKeyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();

        NewsCategory? activeCategory = null;
        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            // Slug không tồn tại ⇒ activeCategory null và KHÔNG lọc gì thêm ở dưới;
            // controller vẫn render trang bình thường (trạng thái rỗng), không 500.
            activeCategory = await db.NewsCategories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == categorySlug && c.IsActive);

            if (activeCategory is null)
            {
                return new NewsListPage
                {
                    Articles = new PagedResult<NewsArticle> { Items = [], Page = 1, PageSize = pageSize, TotalItems = 0 },
                    Categories = await GetCategoryCountsAsync(),
                    Keyword = trimmedKeyword
                };
            }
        }

        // Khối nổi bật CHỈ hiện ở trang 1 khi không lọc chuyên mục và không tìm kiếm
        var showFeatured = page == 1 && activeCategory is null && trimmedKeyword is null;

        var featured = showFeatured
            ? await db.NewsArticles.AsNoTracking()
                .Include(a => a.Category)
                .Where(a => a.IsActive && a.IsFeatured)
                .OrderByDescending(a => a.PublishedAt)
                .Take(FeaturedCount)
                .ToListAsync()
            : [];

        // Loại bài nổi bật khỏi danh sách dưới để không hiện 2 lần — loại TRƯỚC khi đếm
        // để TotalItems (và do đó số trang) khớp đúng với số bài thực sự liệt kê.
        var featuredIds = featured.Select(a => a.Id).ToList();

        var query = db.NewsArticles.AsNoTracking()
            .Include(a => a.Category)
            .Where(a => a.IsActive);

        if (featuredIds.Count > 0)
        {
            query = query.Where(a => !featuredIds.Contains(a.Id));
        }

        if (activeCategory is not null)
        {
            query = query.Where(a => a.CategoryId == activeCategory.Id);
        }

        if (trimmedKeyword is not null)
        {
            query = query.Where(a => a.Title.Contains(trimmedKeyword) || a.Excerpt!.Contains(trimmedKeyword));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new NewsListPage
        {
            Featured = featured,
            Articles = new PagedResult<NewsArticle>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            },
            Categories = await GetCategoryCountsAsync(),
            ActiveCategory = activeCategory,
            Keyword = trimmedKeyword
        };
    }

    public async Task<IReadOnlyList<NewsCategory>> GetCategoriesAsync() =>
        await db.NewsCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

    /// <summary>Chuyên mục + số bài đang hiển thị (chip lọc ghi "Thiết bị (3)" — số lấy từ DB).</summary>
    private async Task<IReadOnlyList<NewsCategoryCount>> GetCategoryCountsAsync() =>
        await db.NewsCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new NewsCategoryCount
            {
                Category = c,
                ArticleCount = c.Articles.Count(a => a.IsActive)
            })
            .ToListAsync();
}
