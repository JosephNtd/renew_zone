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
        var article = await db.NewsArticles.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id && n.IsActive);

        if (article is null)
        {
            return null;
        }

        // Ưu tiên bài cùng chuyên mục; thiếu thì bù bằng bài mới nhất để khối "Đọc thêm" luôn đủ 4 ô.
        var sameTag = await db.NewsArticles.AsNoTracking()
            .Where(n => n.IsActive && n.Id != id && n.CategoryTag == article.CategoryTag)
            .OrderByDescending(n => n.PublishedAt)
            .Take(MaxRelated)
            .ToListAsync();

        if (sameTag.Count < MaxRelated)
        {
            var excluded = sameTag.Select(n => n.Id).Append(id).ToList();

            var filler = await db.NewsArticles.AsNoTracking()
                .Where(n => n.IsActive && !excluded.Contains(n.Id))
                .OrderByDescending(n => n.PublishedAt)
                .Take(MaxRelated - sameTag.Count)
                .ToListAsync();

            sameTag.AddRange(filler);
        }

        return new NewsDetail { Article = article, Related = sameTag };
    }
}
