using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>
/// Đọc menu từ bảng MenuItems, cache 5 phút giống <see cref="SiteContentService"/>
/// (layout gọi 3 lần mỗi request: nav chính + 2 cột footer).
/// </summary>
public class NavigationService(AppDbContext db, IMemoryCache cache) : INavigationService
{
    private const string KeyPrefix = "nav:menu:";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<MenuItem>> GetMenuAsync(MenuGroup group)
    {
        var result = await cache.GetOrCreateAsync(KeyPrefix + group, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;

            return await db.MenuItems.AsNoTracking()
                .Where(m => m.Group == group && m.IsActive && m.ParentId == null)
                .Include(m => m.Children.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder))
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();
        });

        return result!;
    }

    public void Invalidate()
    {
        foreach (var group in Enum.GetValues<MenuGroup>())
        {
            cache.Remove(KeyPrefix + group);
        }
    }
}
