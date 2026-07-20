using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>
/// Nạp TOÀN BỘ bảng slug (chỉ 2 cột Slug + Id của 4 bảng, cỡ vài chục dòng) vào một Dictionary
/// và cache 5 phút. Fallback route chạy trên mọi URL lạ — tra dictionary trong bộ nhớ
/// thay vì 4 query/request, kể cả khi bị dò URL rác.
/// </summary>
public class SlugResolver(AppDbContext db, IMemoryCache cache) : ISlugResolver
{
    private const string CacheKey = "slug:map";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task<SlugMatch?> ResolveAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var map = await GetMapAsync();
        return map.GetValueOrDefault(slug.Trim());
    }

    public void Invalidate() => cache.Remove(CacheKey);

    private async Task<IReadOnlyDictionary<string, SlugMatch>> GetMapAsync()
    {
        var map = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;

            var categories = await db.Categories.AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { c.Slug, c.Id })
                .ToListAsync();

            var products = await db.Products.AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => new { p.Slug, p.Id })
                .ToListAsync();

            var news = await db.NewsArticles.AsNoTracking()
                .Where(n => n.IsActive)
                .Select(n => new { n.Slug, n.Id })
                .ToListAsync();

            var pages = await db.StaticPages.AsNoTracking()
                .Where(s => s.IsActive)
                .Select(s => new { s.Slug, s.Id })
                .ToListAsync();

            // Slug lẽ ra unique toàn cục nhưng DB chỉ ép unique trong TỪNG bảng (Lưu ý 6.4).
            // TryAdd theo đúng thứ tự ưu tiên ⇒ nếu lỡ trùng thì kết quả vẫn tất định, không đổi theo request.
            var result = new Dictionary<string, SlugMatch>(StringComparer.OrdinalIgnoreCase);

            foreach (var c in categories) result.TryAdd(c.Slug, new SlugMatch(SlugTargetType.Category, c.Id));
            foreach (var p in products) result.TryAdd(p.Slug, new SlugMatch(SlugTargetType.Product, p.Id));
            foreach (var n in news) result.TryAdd(n.Slug, new SlugMatch(SlugTargetType.NewsArticle, n.Id));
            foreach (var s in pages) result.TryAdd(s.Slug, new SlugMatch(SlugTargetType.StaticPage, s.Id));

            return result;
        });

        return map!;
    }
}
