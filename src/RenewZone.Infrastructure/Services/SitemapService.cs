using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="ISitemapService"/>. Cache IMemoryCache TTL 1 giờ.</summary>
public class SitemapService(AppDbContext db, IMemoryCache cache) : ISitemapService
{
    private const string CacheKey = "rz_sitemap_entries";

    // TTL 1 giờ: sitemap không cần realtime. Chọn cách này thay vì móc vào ICacheInvalidator vì
    // đơn giản hơn nhiều và crawler ghé thưa — trễ tối đa 1h khi admin thêm/sửa nội dung là vô hại.
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    public async Task<IReadOnlyList<SitemapEntry>> GetEntriesAsync()
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<SitemapEntry>? cached) && cached is not null)
        {
            return cached;
        }

        // Các trang tĩnh cố định của web khách
        var entries = new List<SitemapEntry>
        {
            new("/", DateTime.Now, "daily", 1.0),
            new("/san-pham", null, "daily", 0.8),
            new("/tin-tuc", null, "daily", 0.6),
            new("/lien-he", null, "monthly", 0.4),
        };

        // Global query filter tự loại bản ghi trong Thùng rác; lọc thêm IsActive ⇒ chỉ URL trả 200.
        // (Đưa slug đã ẩn/xóa vào sitemap là Google báo lỗi.)
        var cats = await db.Categories.AsNoTracking().Where(c => c.IsActive)
            .Select(c => new { c.Slug, c.UpdatedAt, c.CreatedAt }).ToListAsync();
        entries.AddRange(cats.Select(c => new SitemapEntry("/" + c.Slug, c.UpdatedAt ?? c.CreatedAt, "weekly", 0.7)));

        var products = await db.Products.AsNoTracking().Where(p => p.IsActive)
            .Select(p => new { p.Slug, p.UpdatedAt, p.CreatedAt }).ToListAsync();
        entries.AddRange(products.Select(p => new SitemapEntry("/" + p.Slug, p.UpdatedAt ?? p.CreatedAt, "weekly", 0.9)));

        var news = await db.NewsArticles.AsNoTracking().Where(n => n.IsActive)
            .Select(n => new { n.Slug, n.UpdatedAt, n.CreatedAt }).ToListAsync();
        entries.AddRange(news.Select(n => new SitemapEntry("/" + n.Slug, n.UpdatedAt ?? n.CreatedAt, "monthly", 0.6)));

        var pages = await db.StaticPages.AsNoTracking().Where(s => s.IsActive)
            .Select(s => new { s.Slug, s.UpdatedAt, s.CreatedAt }).ToListAsync();
        entries.AddRange(pages.Select(s => new SitemapEntry("/" + s.Slug, s.UpdatedAt ?? s.CreatedAt, "monthly", 0.5)));

        IReadOnlyList<SitemapEntry> result = entries;
        cache.Set(CacheKey, result, Ttl);
        return result;
    }
}
