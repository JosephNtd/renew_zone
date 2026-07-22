using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="ICategoryAdminService"/>.</summary>
public class CategoryAdminService(
    AppDbContext db,
    ISlugService slugs,
    ILogger<CategoryAdminService> logger) : ICategoryAdminService
{
    public async Task<IReadOnlyList<CategoryTreeNode>> GetTreeAsync()
    {
        // Nạp 1 lần rồi dựng cây trong bộ nhớ (danh mục chỉ vài chục dòng) — tránh N+1
        var all = await db.Categories.AsNoTracking()
            .Select(c => new
            {
                c.Id, c.Name, c.Slug, c.ImageUrl, c.IconClass,
                c.DisplayOrder, c.IsActive, c.ParentId,
                ProductCount = c.Products.Count()
            })
            .ToListAsync();

        var byParent = all.ToLookup(c => c.ParentId);

        return [.. byParent[null]
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(parent => new CategoryTreeNode(
                parent.Id, parent.Name, parent.Slug, parent.ImageUrl, parent.IconClass,
                parent.DisplayOrder, parent.IsActive, parent.ProductCount,
                [.. byParent[parent.Id]
                    .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
                    .Select(child => new CategoryTreeNode(
                        child.Id, child.Name, child.Slug, child.ImageUrl, child.IconClass,
                        child.DisplayOrder, child.IsActive, child.ProductCount, []))]))];
    }

    public async Task<Category?> GetAsync(int id) =>
        await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IReadOnlyList<Category>> GetParentOptionsAsync(int? excludeId = null)
    {
        // Chỉ danh mục CẤP 1 mới được làm cha — cây cố định 2 cấp (CLAUDE.md mục 4.2)
        var query = db.Categories.AsNoTracking().Where(c => c.ParentId == null);

        if (excludeId is { } id)
        {
            // Loại chính nó: cha của chính mình ⇒ cây vòng lặp
            query = query.Where(c => c.Id != id);
        }

        return await query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();
    }

    public async Task<AdminResult> CreateAsync(CategoryInput input)
    {
        var slug = slugs.Slugify(input.Slug);

        var check = await slugs.CheckAsync(slug, SlugTargetType.Category);
        if (!check.IsAvailable)
        {
            return AdminResult.Fail(check.Message!);
        }

        // Danh mục cha phải tồn tại VÀ là cấp 1 — chống việc sửa querystring tạo cây 3 cấp
        if (input.ParentId is { } parentId)
        {
            var parentIsRoot = await db.Categories
                .AnyAsync(c => c.Id == parentId && c.ParentId == null);

            if (!parentIsRoot)
            {
                return AdminResult.Fail("Danh mục cha không hợp lệ (chỉ được chọn danh mục cấp 1).");
            }
        }

        var category = new Category
        {
            Name = input.Name.Trim(),
            Slug = slug,
            ParentId = input.ParentId,
            ImageUrl = input.ImageUrl,
            IconClass = string.IsNullOrWhiteSpace(input.IconClass) ? null : input.IconClass.Trim(),
            DisplayOrder = input.DisplayOrder,
            IsActive = input.IsActive
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(); // cache menu/trang chủ/slug tự xóa (ICacheInvalidator)

        logger.LogInformation("Tạo danh mục {Name} (id {Id})", category.Name, category.Id);

        return AdminResult.Success($"Đã thêm danh mục “{category.Name}”.", category.Id);
    }

    public async Task<AdminResult> UpdateAsync(CategoryInput input)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == input.Id);

        if (category is null)
        {
            return AdminResult.Fail("Không tìm thấy danh mục.");
        }

        var slug = slugs.Slugify(input.Slug);

        var check = await slugs.CheckAsync(slug, SlugTargetType.Category, category.Id);
        if (!check.IsAvailable)
        {
            return AdminResult.Fail(check.Message!);
        }

        if (input.ParentId is { } parentId)
        {
            if (parentId == category.Id)
            {
                return AdminResult.Fail("Không thể đặt danh mục làm cha của chính nó.");
            }

            var parentIsRoot = await db.Categories
                .AnyAsync(c => c.Id == parentId && c.ParentId == null);

            if (!parentIsRoot)
            {
                return AdminResult.Fail("Danh mục cha không hợp lệ (chỉ được chọn danh mục cấp 1).");
            }

            // Đang có con mà lại nhận cha ⇒ thành cây 3 cấp, web khách không render kiểu đó
            var hasChildren = await db.Categories.AnyAsync(c => c.ParentId == category.Id);
            if (hasChildren)
            {
                return AdminResult.Fail(
                    "Danh mục này đang có danh mục con nên không thể chuyển thành danh mục con của mục khác.");
            }
        }

        category.Name = input.Name.Trim();
        category.Slug = slug;
        category.ParentId = input.ParentId;
        category.ImageUrl = input.ImageUrl;
        category.IconClass = string.IsNullOrWhiteSpace(input.IconClass) ? null : input.IconClass.Trim();
        category.DisplayOrder = input.DisplayOrder;
        category.IsActive = input.IsActive;

        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã lưu danh mục “{category.Name}”.", category.Id);
    }

    public async Task<AdminResult> DeleteAsync(int id)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return AdminResult.Fail("Không tìm thấy danh mục.");
        }

        // Kiểm TRƯỚC và báo lỗi tử tế thay vì để DB ném lỗi khóa ngoại thô (ràng buộc Restrict)
        var productCount = await db.Products.CountAsync(p => p.CategoryId == id);
        if (productCount > 0)
        {
            return AdminResult.Fail(
                $"Không thể xóa — danh mục còn {productCount} sản phẩm. " +
                "Hãy chuyển các sản phẩm này sang danh mục khác trước.");
        }

        var childCount = await db.Categories.CountAsync(c => c.ParentId == id);
        if (childCount > 0)
        {
            return AdminResult.Fail(
                $"Không thể xóa — danh mục còn {childCount} danh mục con. Hãy xóa các mục con trước.");
        }

        db.Categories.Remove(category); // SaveChanges biến thành xóa MỀM (ISoftDeletable)
        await db.SaveChangesAsync();

        logger.LogInformation("Xóa mềm danh mục {Name} (id {Id})", category.Name, id);

        return AdminResult.Success($"Đã chuyển danh mục “{category.Name}” vào Thùng rác.");
    }

    public async Task<AdminResult> ToggleActiveAsync(int id)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return AdminResult.Fail("Không tìm thấy danh mục.");
        }

        category.IsActive = !category.IsActive;
        await db.SaveChangesAsync();

        return AdminResult.Success(category.IsActive
            ? $"Đã hiện “{category.Name}” trên website."
            : $"Đã ẩn “{category.Name}” khỏi website.");
    }

    public async Task<AdminResult> ReorderAsync(int[] orderedIds)
    {
        if (orderedIds.Length == 0)
        {
            return AdminResult.Success("Không có gì thay đổi.");
        }

        var categories = await db.Categories
            .Where(c => orderedIds.Contains(c.Id))
            .ToListAsync();

        for (var i = 0; i < orderedIds.Length; i++)
        {
            var category = categories.FirstOrDefault(c => c.Id == orderedIds[i]);
            if (category is not null)
            {
                category.DisplayOrder = i;
            }
        }

        await db.SaveChangesAsync();

        return AdminResult.Success("Đã lưu thứ tự mới.");
    }
}
