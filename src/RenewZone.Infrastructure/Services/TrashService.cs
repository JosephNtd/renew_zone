using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RenewZone.Domain.Common;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>
/// Xem <see cref="ITrashService"/>.
/// <para>
/// ⚠️ MỌI query ở đây đều phải <c>IgnoreQueryFilters()</c> — không có nó thì global query filter
/// lọc sạch bản ghi đã xóa mềm và màn Thùng rác luôn rỗng.
/// </para>
/// </summary>
public class TrashService(
    AppDbContext db,
    ICacheInvalidator cache,
    ILogger<TrashService> logger) : ITrashService
{
    public async Task<IReadOnlyDictionary<TrashType, int>> GetCountsAsync() =>
        new Dictionary<TrashType, int>
        {
            [TrashType.Product] = await db.Products.IgnoreQueryFilters().CountAsync(x => x.IsDeleted),
            [TrashType.Category] = await db.Categories.IgnoreQueryFilters().CountAsync(x => x.IsDeleted),
            [TrashType.NewsArticle] = await db.NewsArticles.IgnoreQueryFilters().CountAsync(x => x.IsDeleted),
            [TrashType.NewsCategory] = await db.NewsCategories.IgnoreQueryFilters().CountAsync(x => x.IsDeleted),
            [TrashType.StaticPage] = await db.StaticPages.IgnoreQueryFilters().CountAsync(x => x.IsDeleted),
            [TrashType.Banner] = await db.Banners.IgnoreQueryFilters().CountAsync(x => x.IsDeleted),
            [TrashType.Testimonial] = await db.Testimonials.IgnoreQueryFilters().CountAsync(x => x.IsDeleted),
            [TrashType.MenuItem] = await db.MenuItems.IgnoreQueryFilters().CountAsync(x => x.IsDeleted),
            [TrashType.ContentBlock] = await db.ContentBlocks.IgnoreQueryFilters().CountAsync(x => x.IsDeleted),
            [TrashType.ContactMessage] = await db.ContactMessages.IgnoreQueryFilters().CountAsync(x => x.IsDeleted),
            [TrashType.ProductReview] = await db.ProductReviews.IgnoreQueryFilters().CountAsync(x => x.IsDeleted)
        };

    public async Task<IReadOnlyList<TrashItem>> GetItemsAsync(TrashType type) => type switch
    {
        TrashType.Product => await db.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.IsDeleted)
            .Select(x => new TrashItem(x.Id, TrashType.Product, x.Name, "/" + x.Slug, x.DeletedAt))
            .ToListAsync(),

        TrashType.Category => await db.Categories.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.IsDeleted)
            .Select(x => new TrashItem(x.Id, TrashType.Category, x.Name, "/" + x.Slug, x.DeletedAt))
            .ToListAsync(),

        TrashType.NewsArticle => await db.NewsArticles.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.IsDeleted)
            .Select(x => new TrashItem(x.Id, TrashType.NewsArticle, x.Title, "/" + x.Slug, x.DeletedAt))
            .ToListAsync(),

        TrashType.NewsCategory => await db.NewsCategories.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.IsDeleted)
            .Select(x => new TrashItem(x.Id, TrashType.NewsCategory, x.Name, null, x.DeletedAt))
            .ToListAsync(),

        TrashType.StaticPage => await db.StaticPages.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.IsDeleted)
            .Select(x => new TrashItem(x.Id, TrashType.StaticPage, x.Title, "/" + x.Slug, x.DeletedAt))
            .ToListAsync(),

        TrashType.Banner => await db.Banners.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.IsDeleted)
            .Select(x => new TrashItem(x.Id, TrashType.Banner,
                x.Title ?? "Banner #" + x.Id, x.ImageUrl, x.DeletedAt))
            .ToListAsync(),

        TrashType.Testimonial => await db.Testimonials.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.IsDeleted)
            .Select(x => new TrashItem(x.Id, TrashType.Testimonial, x.CustomerName, x.Content, x.DeletedAt))
            .ToListAsync(),

        TrashType.MenuItem => await db.MenuItems.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.IsDeleted)
            .Select(x => new TrashItem(x.Id, TrashType.MenuItem, x.Text, x.Url, x.DeletedAt))
            .ToListAsync(),

        TrashType.ContentBlock => await db.ContentBlocks.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.IsDeleted)
            .Select(x => new TrashItem(x.Id, TrashType.ContentBlock, x.Title, x.GroupKey, x.DeletedAt))
            .ToListAsync(),

        TrashType.ContactMessage => await db.ContactMessages.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.IsDeleted)
            .Select(x => new TrashItem(x.Id, TrashType.ContactMessage, x.FullName, x.Subject, x.DeletedAt))
            .ToListAsync(),
        // IgnoreQueryFilters áp cho cả câu ⇒ tên sản phẩm vẫn lấy được khi sản phẩm cũng đang trong Thùng rác
        TrashType.ProductReview => await db.ProductReviews.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.IsDeleted)
            .Select(x => new TrashItem(x.Id, TrashType.ProductReview,
                x.AuthorName + " · " + x.Rating + "★ · " + x.Product.Name, x.Content, x.DeletedAt))
            .ToListAsync(),

        _ => []
    };

    public async Task<AdminResult> RestoreAsync(TrashType type, int id)
    {
        var entity = await FindAsync(type, id);

        if (entity is null)
        {
            return AdminResult.Fail("Không tìm thấy mục cần khôi phục.");
        }

        // Khôi phục danh mục con mà cha vẫn trong Thùng rác ⇒ danh mục "mồ côi",
        // hiện lên admin nhưng web khách không render được vì cây bị đứt
        if (entity is Category category && category.ParentId is { } parentId)
        {
            var parentDeleted = await db.Categories.IgnoreQueryFilters()
                .AnyAsync(c => c.Id == parentId && c.IsDeleted);

            if (parentDeleted)
            {
                return AdminResult.Fail("Hãy khôi phục danh mục cha trước.");
            }
        }

        if (entity is MenuItem menu && menu.ParentId is { } menuParentId)
        {
            var parentDeleted = await db.MenuItems.IgnoreQueryFilters()
                .AnyAsync(m => m.Id == menuParentId && m.IsDeleted);

            if (parentDeleted)
            {
                return AdminResult.Fail("Hãy khôi phục mục menu cha trước.");
            }
        }

        // Sản phẩm khôi phục vào danh mục đang nằm trong Thùng rác ⇒ web khách vỡ cây danh mục
        if (entity is Product product)
        {
            var categoryDeleted = await db.Categories.IgnoreQueryFilters()
                .AnyAsync(c => c.Id == product.CategoryId && c.IsDeleted);

            if (categoryDeleted)
            {
                return AdminResult.Fail("Hãy khôi phục danh mục của sản phẩm này trước.");
            }
        }

        entity.IsDeleted = false;
        entity.DeletedAt = null;

        await db.SaveChangesAsync(); // đi qua SaveChanges ⇒ cache tự xóa

        // Đánh giá đã duyệt quay lại ⇒ điểm sao + số đánh giá của sản phẩm phải tính lại
        if (entity is ProductReview review)
        {
            await db.RecomputeProductRatingAsync(review.ProductId);
        }

        logger.LogInformation("Khôi phục {Type} id {Id}", type, id);

        return AdminResult.Success("Đã khôi phục.");
    }

    public async Task<AdminResult> PurgeAsync(TrashType type, int id)
    {
        var entity = await FindAsync(type, id);

        if (entity is null)
        {
            return AdminResult.Fail("Không tìm thấy mục cần xóa.");
        }

        // Ràng buộc Restrict của DB: xóa CỨNG danh mục còn sản phẩm (kể cả sản phẩm đang trong
        // Thùng rác) sẽ ném lỗi khóa ngoại thô — kiểm trước để báo cho tử tế
        if (entity is Category)
        {
            var productCount = await db.Products.IgnoreQueryFilters().CountAsync(p => p.CategoryId == id);
            if (productCount > 0)
            {
                return AdminResult.Fail(
                    $"Không thể xóa vĩnh viễn — vẫn còn {productCount} sản phẩm thuộc danh mục này " +
                    "(kể cả sản phẩm đang trong Thùng rác).");
            }

            var childCount = await db.Categories.IgnoreQueryFilters().CountAsync(c => c.ParentId == id);
            if (childCount > 0)
            {
                return AdminResult.Fail($"Không thể xóa vĩnh viễn — vẫn còn {childCount} danh mục con.");
            }
        }

        if (entity is MenuItem)
        {
            var childCount = await db.MenuItems.IgnoreQueryFilters().CountAsync(m => m.ParentId == id);
            if (childCount > 0)
            {
                return AdminResult.Fail($"Không thể xóa vĩnh viễn — vẫn còn {childCount} mục menu con.");
            }
        }

        // ⚠️ ExecuteDeleteAsync đi VÒNG QUA change tracker: đây là cách duy nhất xóa cứng được
        // (ApplySoftDelete trong SaveChanges sẽ chặn Remove()). Đổi lại, nó KHÔNG kích hoạt
        // ICacheInvalidator ⇒ phải xóa cache TAY ở cuối hàm này.
        var affected = await DeleteHardAsync(type, id);

        if (affected == 0)
        {
            return AdminResult.Fail("Không xóa được, có thể mục này vừa bị xóa bởi thao tác khác.");
        }

        cache.Invalidate(ScopeOf(type));

        logger.LogWarning("XÓA VĨNH VIỄN {Type} id {Id}", type, id);

        return AdminResult.Success("Đã xóa vĩnh viễn.");
    }

    private async Task<ISoftDeletable?> FindAsync(TrashType type, int id) => type switch
    {
        TrashType.Product => await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id),
        TrashType.Category => await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id),
        TrashType.NewsArticle => await db.NewsArticles.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id),
        TrashType.NewsCategory => await db.NewsCategories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id),
        TrashType.StaticPage => await db.StaticPages.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id),
        TrashType.Banner => await db.Banners.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id),
        TrashType.Testimonial => await db.Testimonials.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id),
        TrashType.MenuItem => await db.MenuItems.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id),
        TrashType.ContentBlock => await db.ContentBlocks.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id),
        TrashType.ContactMessage => await db.ContactMessages.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id),
        TrashType.ProductReview => await db.ProductReviews.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id),
        _ => null
    };

    private async Task<int> DeleteHardAsync(TrashType type, int id) => type switch
    {
        TrashType.Product => await db.Products.IgnoreQueryFilters()
            .Where(x => x.Id == id).ExecuteDeleteAsync(),
        TrashType.Category => await db.Categories.IgnoreQueryFilters()
            .Where(x => x.Id == id).ExecuteDeleteAsync(),
        TrashType.NewsArticle => await db.NewsArticles.IgnoreQueryFilters()
            .Where(x => x.Id == id).ExecuteDeleteAsync(),
        TrashType.NewsCategory => await db.NewsCategories.IgnoreQueryFilters()
            .Where(x => x.Id == id).ExecuteDeleteAsync(),
        TrashType.StaticPage => await db.StaticPages.IgnoreQueryFilters()
            .Where(x => x.Id == id).ExecuteDeleteAsync(),
        TrashType.Banner => await db.Banners.IgnoreQueryFilters()
            .Where(x => x.Id == id).ExecuteDeleteAsync(),
        TrashType.Testimonial => await db.Testimonials.IgnoreQueryFilters()
            .Where(x => x.Id == id).ExecuteDeleteAsync(),
        TrashType.MenuItem => await db.MenuItems.IgnoreQueryFilters()
            .Where(x => x.Id == id).ExecuteDeleteAsync(),
        TrashType.ContentBlock => await db.ContentBlocks.IgnoreQueryFilters()
            .Where(x => x.Id == id).ExecuteDeleteAsync(),
        TrashType.ContactMessage => await db.ContactMessages.IgnoreQueryFilters()
            .Where(x => x.Id == id).ExecuteDeleteAsync(),
        TrashType.ProductReview => await db.ProductReviews.IgnoreQueryFilters()
            .Where(x => x.Id == id).ExecuteDeleteAsync(),
        _ => 0
    };

    /// <summary>Vùng cache tương ứng — phải khớp bảng tra trong <c>CacheInvalidator</c>.</summary>
    private static CacheScope ScopeOf(TrashType type) => type switch
    {
        TrashType.Product => CacheScope.Home | CacheScope.Slugs,
        TrashType.Category => CacheScope.SiteContent | CacheScope.Home | CacheScope.Slugs,
        TrashType.NewsArticle => CacheScope.Home | CacheScope.Slugs,
        TrashType.NewsCategory => CacheScope.Home,
        TrashType.StaticPage => CacheScope.Slugs,
        TrashType.Banner or TrashType.Testimonial => CacheScope.Home,
        TrashType.MenuItem => CacheScope.Navigation,
        TrashType.ContentBlock => CacheScope.ContentBlocks,
        TrashType.ProductReview => CacheScope.Home,
        _ => CacheScope.None
    };
}
