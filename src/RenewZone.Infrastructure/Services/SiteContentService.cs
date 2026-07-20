using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>
/// Đọc menu + settings có cache in-memory 5 phút (layout gọi mỗi request — không được query DB liên tục).
/// Admin sửa settings/danh mục ở Giai đoạn 6 sẽ gọi Invalidate() để xóa cache.
/// </summary>
public class SiteContentService(AppDbContext db, IMemoryCache cache) : ISiteContentService
{
    private const string SettingsKey = "site:settings";
    private const string MenuKey = "site:menu";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyDictionary<string, string?>> GetSettingsAsync()
    {
        var result = await cache.GetOrCreateAsync(SettingsKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;
            return await db.SiteSettings.AsNoTracking()
                .ToDictionaryAsync(s => s.Key, s => s.Value);
        });
        return result!;
    }

    public async Task<IReadOnlyList<Category>> GetMenuCategoriesAsync()
    {
        var result = await cache.GetOrCreateAsync(MenuKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;
            return await db.Categories.AsNoTracking()
                .Where(c => c.IsActive && c.ParentId == null)
                .Include(c => c.Children.Where(ch => ch.IsActive).OrderBy(ch => ch.DisplayOrder))
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        });
        return result!;
    }

    public void Invalidate()
    {
        cache.Remove(SettingsKey);
        cache.Remove(MenuKey);
    }
}
