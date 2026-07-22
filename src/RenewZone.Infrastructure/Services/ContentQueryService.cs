using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <inheritdoc cref="IContentQueryService" />
public class ContentQueryService(AppDbContext db) : IContentQueryService
{
    private const int MaxRelated = 4;

    public Task<StaticPage?> GetStaticPageAsync(int id) =>
        db.StaticPages.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

    public async Task<NewsDetail?> GetNewsDetailAsync(int id)
    {
        // Include Category: view render tên chuyên mục + link về /tin-tuc?chuyen-muc=
        var article = await db.NewsArticles.AsNoTracking()
            .Include(n => n.Category)
            .FirstOrDefaultAsync(n => n.Id == id && n.IsActive);

        if (article is null)
        {
            return null;
        }

        // Ưu tiên bài cùng chuyên mục; thiếu thì bù bằng bài mới nhất để khối "Đọc thêm" luôn đủ 4 ô.
        List<NewsArticle> sameCategory = article.CategoryId is null
            ? []
            : await db.NewsArticles.AsNoTracking()
                .Include(n => n.Category)
                .Where(n => n.IsActive && n.Id != id && n.CategoryId == article.CategoryId)
                .OrderByDescending(n => n.PublishedAt)
                .Take(MaxRelated)
                .ToListAsync();

        if (sameCategory.Count < MaxRelated)
        {
            var excluded = sameCategory.Select(n => n.Id).Append(id).ToList();

            var filler = await db.NewsArticles.AsNoTracking()
                .Include(n => n.Category)
                .Where(n => n.IsActive && !excluded.Contains(n.Id))
                .OrderByDescending(n => n.PublishedAt)
                .Take(MaxRelated - sameCategory.Count)
                .ToListAsync();

            sameCategory.AddRange(filler);
        }

        return new NewsDetail { Article = article, Related = sameCategory };
    }
}
