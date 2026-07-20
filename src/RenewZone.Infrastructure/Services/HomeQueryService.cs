using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>
/// Dựng HomeContent cho trang chủ với cache in-memory 2 phút — ngắn hơn SiteContentService (5 phút)
/// vì dữ liệu sản phẩm nổi bật/bán chạy đổi thường xuyên hơn menu/settings.
/// </summary>
public class HomeQueryService(AppDbContext db, IMemoryCache cache) : IHomeQueryService
{
    private const string CacheKey = "home:content";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    private const int MaxPerCategory = 8;
    private const int MaxFeatured = 8;
    private const int MaxBestSeller = 10;
    private const int MaxNews = 4;

    public async Task<HomeContent> GetHomeContentAsync()
    {
        var result = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;

            var parents = await db.Categories.AsNoTracking()
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            // Tải một lần toàn bộ sản phẩm active (kèm danh mục + ảnh chính) rồi chia nhóm trong bộ nhớ —
            // catalog nhỏ, tránh N+1 query cho từng danh mục/khối nổi bật/bán chạy.
            var products = await db.Products.AsNoTracking()
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.Images.Where(i => i.IsMain))
                .OrderBy(p => p.Id)
                .ToListAsync();

            var byParent = products
                .GroupBy(p => p.Category.ParentId ?? p.CategoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var sections = parents
                .Where(parent => byParent.ContainsKey(parent.Id))
                .Select(parent => new CategorySection
                {
                    Category = parent,
                    Products = byParent[parent.Id].Take(MaxPerCategory).ToList()
                })
                .ToList();

            var banners = await db.Banners.AsNoTracking()
                .Where(b => b.IsActive &&
                    (b.Position == BannerPosition.HomeSlider || b.Position == BannerPosition.HomeMiddle))
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            var testimonials = await db.Testimonials.AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            var news = await db.NewsArticles.AsNoTracking()
                .Where(n => n.IsActive)
                .OrderByDescending(n => n.PublishedAt)
                .Take(MaxNews)
                .ToListAsync();

            return new HomeContent
            {
                HeroBanners = banners.Where(b => b.Position == BannerPosition.HomeSlider).ToList(),
                SideBanners = banners.Where(b => b.Position == BannerPosition.HomeMiddle).ToList(),
                Categories = parents,
                CategorySections = sections,
                FeaturedProducts = products.Where(p => p.IsFeatured).Take(MaxFeatured).ToList(),
                BestSellers = products.Where(p => p.IsBestSeller).Take(MaxBestSeller).ToList(),
                Testimonials = testimonials,
                LatestNews = news
            };
        });

        return result!;
    }

    public void Invalidate() => cache.Remove(CacheKey);
}
