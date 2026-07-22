using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>
/// Đọc ContentBlock theo nhóm, cache 5 phút. Nhóm nào chưa có dòng nào ⇒ trả danh sách rỗng
/// (view tự ẩn khối, không throw) — đúng luật fallback ở CLAUDE.md mục 4.7.
/// </summary>
public class ContentBlockService(AppDbContext db, IMemoryCache cache) : IContentBlockService
{
    private const string KeyPrefix = "content:block:";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    /// <summary>Ghi lại các nhóm đã nạp để <see cref="Invalidate"/> xóa được hết (IMemoryCache không liệt kê key).</summary>
    private static readonly HashSet<string> KnownGroups = [];

    public async Task<IReadOnlyList<ContentBlock>> GetAsync(string groupKey)
    {
        if (string.IsNullOrWhiteSpace(groupKey))
        {
            return [];
        }

        lock (KnownGroups)
        {
            KnownGroups.Add(groupKey);
        }

        var result = await cache.GetOrCreateAsync(KeyPrefix + groupKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;

            return await db.ContentBlocks.AsNoTracking()
                .Where(b => b.GroupKey == groupKey && b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();
        });

        return result!;
    }

    public void Invalidate()
    {
        string[] groups;
        lock (KnownGroups)
        {
            groups = [.. KnownGroups];
        }

        foreach (var group in groups)
        {
            cache.Remove(KeyPrefix + group);
        }
    }
}
