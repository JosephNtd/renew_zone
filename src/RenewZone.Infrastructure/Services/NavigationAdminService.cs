using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="INavigationAdminService"/>.</summary>
public class NavigationAdminService(AppDbContext db, ISlugResolver slugResolver) : INavigationAdminService
{
    public async Task<IReadOnlyList<MenuTreeNode>> GetTreeAsync(MenuGroup group)
    {
        var all = await db.MenuItems.AsNoTracking()
            .Where(m => m.Group == group)
            .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Text)
            .ToListAsync();

        var byParent = all.ToLookup(m => m.ParentId);

        var roots = new List<MenuTreeNode>();

        foreach (var item in byParent[null])
        {
            var children = new List<MenuTreeNode>();

            foreach (var child in byParent[item.Id])
            {
                children.Add(await ToNodeAsync(child, []));
            }

            roots.Add(await ToNodeAsync(item, children));
        }

        return roots;
    }

    private async Task<MenuTreeNode> ToNodeAsync(MenuItem item, IReadOnlyList<MenuTreeNode> children) =>
        new(item.Id, item.Group, item.Type, item.Text, item.Url, item.Icon,
            item.DisplayOrder, item.IsActive, item.OpenInNewTab, children)
        {
            IsBroken = await IsBrokenAsync(item)
        };

    /// <summary>
    /// Kiểm URL nội bộ 1 đoạn có trỏ tới slug thật không.
    /// <para>
    /// Đây chính là loại lỗi Giai đoạn 2–4 từng mắc: menu trỏ <c>/sua-chua</c> khi trang chưa
    /// được seed ⇒ khách bấm vào là 404 mà không ai biết. Trả null khi KHÔNG kiểm được
    /// (link ngoài, route tĩnh nhiều đoạn, mục ProductMega) — thà không cảnh báo còn hơn
    /// cảnh báo sai làm admin mất tin tưởng.
    /// </para>
    /// </summary>
    private async Task<bool?> IsBrokenAsync(MenuItem item)
    {
        if (item.Type == MenuItemType.ProductMega || string.IsNullOrWhiteSpace(item.Url))
        {
            return null;
        }

        var url = item.Url.Trim();

        // Link ngoài site: không kiểm được
        if (url.StartsWith("http://") || url.StartsWith("https://") || url.StartsWith("//"))
        {
            return null;
        }

        var path = url.TrimStart('/').Split('?')[0].Split('#')[0];

        if (string.IsNullOrEmpty(path))
        {
            return false; // "/" = trang chủ, luôn tồn tại
        }

        // Route tĩnh của web khách (không đi qua fallback slug)
        string[] staticRoutes =
            ["san-pham", "tim-kiem", "gio-hang", "dat-hang", "tin-tuc", "lien-he", "dat-hang-thanh-cong"];

        if (staticRoutes.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        // Nhiều đoạn ⇒ không phải slug phẳng, không kết luận được
        if (path.Contains('/'))
        {
            return null;
        }

        return await slugResolver.ResolveAsync(path) is null;
    }

    public async Task<MenuItem?> GetAsync(int id) =>
        await db.MenuItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    public async Task<IReadOnlyList<MenuItem>> GetParentOptionsAsync(MenuGroup group, int? excludeId = null)
    {
        var query = db.MenuItems.AsNoTracking()
            .Where(m => m.Group == group && m.ParentId == null);

        if (excludeId is { } id)
        {
            query = query.Where(m => m.Id != id);
        }

        return await query.OrderBy(m => m.DisplayOrder).ToListAsync();
    }

    public async Task<AdminResult> SaveAsync(MenuItemInput input)
    {
        var item = input.Id is { } id
            ? await db.MenuItems.FirstOrDefaultAsync(m => m.Id == id)
            : null;

        if (input.Id is not null && item is null)
        {
            return AdminResult.Fail("Không tìm thấy mục menu.");
        }

        if (input.ParentId is { } parentId)
        {
            if (parentId == input.Id)
            {
                return AdminResult.Fail("Không thể đặt mục này làm cha của chính nó.");
            }

            var parent = await db.MenuItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == parentId);

            if (parent is null || parent.ParentId is not null)
            {
                return AdminResult.Fail("Mục cha không hợp lệ (chỉ được chọn mục cấp 1).");
            }

            if (parent.Group != input.Group)
            {
                return AdminResult.Fail("Mục cha phải nằm trong cùng nhóm menu.");
            }

            // Mục đang có con mà lại nhận cha ⇒ menu 3 cấp, layout không render kiểu đó
            if (input.Id is { } currentId && await db.MenuItems.AnyAsync(m => m.ParentId == currentId))
            {
                return AdminResult.Fail("Mục này đang có mục con nên không thể biến thành mục con.");
            }
        }

        if (item is null)
        {
            item = new MenuItem();
            db.MenuItems.Add(item);
        }

        item.Group = input.Group;
        item.Type = input.Type;
        item.Text = input.Text.Trim();
        item.Url = string.IsNullOrWhiteSpace(input.Url) ? null : input.Url.Trim();
        item.Icon = string.IsNullOrWhiteSpace(input.Icon) ? null : input.Icon.Trim();
        item.ParentId = input.ParentId;
        item.DisplayOrder = input.DisplayOrder;
        item.IsActive = input.IsActive;
        item.OpenInNewTab = input.OpenInNewTab;

        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã lưu mục menu “{item.Text}”.", item.Id);
    }

    public async Task<AdminResult> DeleteAsync(int id)
    {
        var item = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == id);

        if (item is null)
        {
            return AdminResult.Fail("Không tìm thấy mục menu.");
        }

        var childCount = await db.MenuItems.CountAsync(m => m.ParentId == id);
        if (childCount > 0)
        {
            return AdminResult.Fail($"Không thể xóa — mục này còn {childCount} mục con.");
        }

        db.MenuItems.Remove(item);
        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã chuyển mục “{item.Text}” vào Thùng rác.");
    }

    public async Task<AdminResult> ToggleActiveAsync(int id)
    {
        var item = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == id);

        if (item is null)
        {
            return AdminResult.Fail("Không tìm thấy mục menu.");
        }

        item.IsActive = !item.IsActive;
        await db.SaveChangesAsync();

        return AdminResult.Success(item.IsActive
            ? $"Đã hiện “{item.Text}” trên menu."
            : $"Đã ẩn “{item.Text}” khỏi menu.");
    }

    public async Task<AdminResult> ReorderAsync(int[] orderedIds)
    {
        var items = await db.MenuItems.Where(m => orderedIds.Contains(m.Id)).ToListAsync();

        for (var i = 0; i < orderedIds.Length; i++)
        {
            var item = items.FirstOrDefault(m => m.Id == orderedIds[i]);
            if (item is not null)
            {
                item.DisplayOrder = i;
            }
        }

        await db.SaveChangesAsync();

        return AdminResult.Success("Đã lưu thứ tự menu.");
    }
}
