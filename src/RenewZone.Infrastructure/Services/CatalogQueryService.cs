using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <inheritdoc cref="ICatalogQueryService" />
public class CatalogQueryService(AppDbContext db) : ICatalogQueryService
{
    private const int MaxRelated = 10;

    public async Task<CategoryPage?> GetCategoryPageAsync(int categoryId, string? seriesSlug, int page, int pageSize)
    {
        var category = await db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.IsActive);

        if (category is null)
        {
            return null;
        }

        // Chuẩn hóa về danh mục CHA: vào thẳng slug series con thì lấy cha của nó làm gốc trang.
        var parentId = category.ParentId ?? category.Id;
        var parent = category.ParentId is null
            ? category
            : await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == parentId && c.IsActive);

        if (parent is null)
        {
            return null; // series con của một danh mục cha đã ẩn ⇒ coi như không tồn tại
        }

        var series = await db.Categories.AsNoTracking()
            .Where(c => c.ParentId == parentId && c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        // Series đang chọn: ưu tiên seriesSlug trên querystring, sau đó tới trường hợp vào thẳng slug con.
        var activeSeries = seriesSlug is not null
            ? series.FirstOrDefault(s => string.Equals(s.Slug, seriesSlug, StringComparison.OrdinalIgnoreCase))
            : (category.ParentId is not null ? category : null);

        var query = db.Products.AsNoTracking().Where(p => p.IsActive);

        if (activeSeries is not null)
        {
            query = query.Where(p => p.CategoryId == activeSeries.Id);
        }
        else
        {
            // Cả cây: sản phẩm gắn thẳng vào cha HOẶC gắn vào bất kỳ series con nào.
            query = query.Where(p => p.CategoryId == parentId || p.Category.ParentId == parentId);
        }

        var products = await PaginateAsync(query.OrderByDescending(p => p.Id), page, pageSize);

        return new CategoryPage
        {
            Parent = parent,
            Series = series,
            ActiveSeries = activeSeries,
            Products = products
        };
    }

    public async Task<ProductDetail?> GetProductDetailAsync(int productId)
    {
        // AsSplitQuery: nạp 2 collection (Images + OptionGroups) bằng nhiều query thay vì 1 JOIN
        // tránh nhân bản hàng (cartesian) và cảnh báo MultipleCollectionInclude của EF.
        var product = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder))
            .Include(p => p.OptionGroups.OrderBy(g => g.DisplayOrder))
                .ThenInclude(g => g.Values.OrderBy(v => v.DisplayOrder))
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

        if (product is null)
        {
            return null;
        }

        var parentId = product.Category.ParentId ?? product.CategoryId;

        var parentCategory = product.Category.ParentId is null
            ? product.Category
            : await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == parentId);

        // Sản phẩm liên quan: cùng cây danh mục, còn active, trừ chính nó.
        var related = await db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Id != productId &&
                (p.CategoryId == parentId || p.Category.ParentId == parentId))
            .Include(p => p.Images.Where(i => i.IsMain))
            .OrderByDescending(p => p.IsBestSeller)
            .ThenByDescending(p => p.Id)
            .Take(MaxRelated)
            .ToListAsync();

        return new ProductDetail
        {
            Product = product,
            ParentCategory = parentCategory,
            Related = related
        };
    }

    public Task<PagedResult<Product>> GetAllProductsAsync(int page, int pageSize)
    {
        var query = db.Products.AsNoTracking().Where(p => p.IsActive);
        return PaginateAsync(query.OrderByDescending(p => p.Id), page, pageSize);
    }

    public async Task<PagedResult<Product>> SearchAsync(string? keyword, int page, int pageSize)
    {
        var query = db.Products.AsNoTracking().Where(p => p.IsActive);

        keyword = keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{keyword}%"));
        }
        else
        {
            // Không có từ khóa ⇒ trả rỗng, tránh liệt kê toàn bộ catalog ngoài ý muốn.
            return new PagedResult<Product> { Page = page, PageSize = pageSize };
        }

        return await PaginateAsync(query.OrderByDescending(p => p.Id), page, pageSize);
    }

    public Task<Product?> GetProductForCartAsync(int productId) =>
        db.Products.AsNoTracking()
            .Include(p => p.Images.Where(i => i.IsMain))
            .Include(p => p.OptionGroups.OrderBy(g => g.DisplayOrder))
                .ThenInclude(g => g.Values.OrderBy(v => v.DisplayOrder))
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

    private static async Task<PagedResult<Product>> PaginateAsync(IQueryable<Product> ordered, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 12;

        var total = await ordered.CountAsync();

        var items = await ordered
            .Include(p => p.Images.Where(i => i.IsMain))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }
}
