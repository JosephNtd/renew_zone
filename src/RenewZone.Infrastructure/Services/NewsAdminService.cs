using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="INewsAdminService"/>.</summary>
public class NewsAdminService(AppDbContext db, ISlugService slugs) : INewsAdminService
{
    public async Task<PagedResult<NewsListItem>> GetListAsync(NewsListRequest request)
    {
        var query = db.NewsArticles.AsNoTracking().AsQueryable();

        if (request.CategoryId is { } categoryId)
        {
            query = query.Where(a => a.CategoryId == categoryId);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(a => a.IsActive == isActive);
        }

        if (request.NormalizedSearch is { } search)
        {
            query = query.Where(a => a.Title.Contains(search) || a.Slug.Contains(search));
        }

        var totalItems = await query.CountAsync();

        query = (request.SortBy, request.SortDesc) switch
        {
            ("tieude", false) => query.OrderBy(a => a.Title),
            ("tieude", true) => query.OrderByDescending(a => a.Title),
            ("luotxem", false) => query.OrderBy(a => a.ViewCount),
            ("luotxem", true) => query.OrderByDescending(a => a.ViewCount),
            ("ngay", false) => query.OrderBy(a => a.PublishedAt),
            _ => query.OrderByDescending(a => a.PublishedAt)
        };

        var items = await query
            .Skip(request.Skip)
            .Take(request.SafePageSize)
            .Select(a => new NewsListItem(
                a.Id, a.Title, a.Slug, a.ThumbnailUrl,
                // Include qua Select: quên là Category null và tag biến mất âm thầm (Lưu ý 6.15)
                a.Category != null ? a.Category.Name : null,
                a.IsFeatured, a.IsActive, a.PublishedAt, a.ViewCount))
            .ToListAsync();

        return new PagedResult<NewsListItem>
        {
            Items = items,
            Page = request.SafePage,
            PageSize = request.SafePageSize,
            TotalItems = totalItems
        };
    }

    public async Task<NewsArticle?> GetAsync(int id) =>
        await db.NewsArticles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

    public async Task<AdminResult> CreateAsync(NewsInput input)
    {
        var slug = slugs.Slugify(input.Slug);

        var check = await slugs.CheckAsync(slug, SlugTargetType.NewsArticle);
        if (!check.IsAvailable)
        {
            return AdminResult.Fail(check.Message!);
        }

        var article = new NewsArticle
        {
            Title = input.Title.Trim(),
            Slug = slug,
            CategoryId = input.CategoryId,
            ThumbnailUrl = input.ThumbnailUrl,
            Excerpt = input.Excerpt,
            ContentHtml = input.ContentHtml,
            IsFeatured = input.IsFeatured,
            IsActive = input.IsActive,
            PublishedAt = input.PublishedAt
        };

        db.NewsArticles.Add(article);
        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã đăng bài “{article.Title}”.", article.Id);
    }

    public async Task<AdminResult> UpdateAsync(NewsInput input)
    {
        var article = await db.NewsArticles.FirstOrDefaultAsync(a => a.Id == input.Id);

        if (article is null)
        {
            return AdminResult.Fail("Không tìm thấy bài viết.");
        }

        var slug = slugs.Slugify(input.Slug);

        var check = await slugs.CheckAsync(slug, SlugTargetType.NewsArticle, article.Id);
        if (!check.IsAvailable)
        {
            return AdminResult.Fail(check.Message!);
        }

        article.Title = input.Title.Trim();
        article.Slug = slug;
        article.CategoryId = input.CategoryId;
        article.ThumbnailUrl = input.ThumbnailUrl;
        article.Excerpt = input.Excerpt;
        article.ContentHtml = input.ContentHtml;
        article.IsFeatured = input.IsFeatured;
        article.IsActive = input.IsActive;
        article.PublishedAt = input.PublishedAt;

        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã lưu bài “{article.Title}”.", article.Id);
    }

    public async Task<AdminResult> DeleteAsync(int id)
    {
        var article = await db.NewsArticles.FirstOrDefaultAsync(a => a.Id == id);

        if (article is null)
        {
            return AdminResult.Fail("Không tìm thấy bài viết.");
        }

        db.NewsArticles.Remove(article);
        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã chuyển bài “{article.Title}” vào Thùng rác.");
    }

    public async Task<AdminResult> ToggleActiveAsync(int id)
    {
        var article = await db.NewsArticles.FirstOrDefaultAsync(a => a.Id == id);

        if (article is null)
        {
            return AdminResult.Fail("Không tìm thấy bài viết.");
        }

        article.IsActive = !article.IsActive;
        await db.SaveChangesAsync();

        return AdminResult.Success(article.IsActive
            ? $"Đã hiện bài “{article.Title}”."
            : $"Đã ẩn bài “{article.Title}”.");
    }

    // Chuyên mục tin

    public async Task<IReadOnlyList<NewsCategoryItem>> GetCategoriesAsync() =>
        await db.NewsCategories.AsNoTracking()
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new NewsCategoryItem(
                c.Id, c.Name, c.Slug, c.DisplayOrder, c.IsActive, c.Articles.Count))
            .ToListAsync();

    public async Task<NewsCategory?> GetCategoryAsync(int id) =>
        await db.NewsCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<AdminResult> SaveCategoryAsync(NewsCategoryInput input)
    {
        var slug = slugs.Slugify(input.Slug);

        if (string.IsNullOrEmpty(slug))
        {
            return AdminResult.Fail("Slug không hợp lệ.");
        }

        // ⚠️ Slug chuyên mục tin KHÔNG nằm trong namespace slug toàn cục (nó lọc bằng
        // querystring ?chuyenMuc=, không phải route root) nên chỉ cần unique TRONG bảng này
        var duplicated = await db.NewsCategories
            .AnyAsync(c => c.Slug == slug && c.Id != input.Id);

        if (duplicated)
        {
            return AdminResult.Fail($"Slug “{slug}” đã được dùng bởi một chuyên mục khác.");
        }

        var category = input.Id is { } id
            ? await db.NewsCategories.FirstOrDefaultAsync(c => c.Id == id)
            : null;

        if (input.Id is not null && category is null)
        {
            return AdminResult.Fail("Không tìm thấy chuyên mục.");
        }

        if (category is null)
        {
            category = new NewsCategory();
            db.NewsCategories.Add(category);
        }

        category.Name = input.Name.Trim();
        category.Slug = slug;
        category.DisplayOrder = input.DisplayOrder;
        category.IsActive = input.IsActive;

        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã lưu chuyên mục “{category.Name}”.", category.Id);
    }

    public async Task<AdminResult> DeleteCategoryAsync(int id)
    {
        var category = await db.NewsCategories.FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return AdminResult.Fail("Không tìm thấy chuyên mục.");
        }

        // FK là SetNull nên xóa được, nhưng phải nói rõ hậu quả cho admin biết
        var articleCount = await db.NewsArticles.CountAsync(a => a.CategoryId == id);

        db.NewsCategories.Remove(category);
        await db.SaveChangesAsync();

        var note = articleCount > 0
            ? $" {articleCount} bài viết chuyển sang trạng thái chưa phân loại."
            : "";

        return AdminResult.Success($"Đã chuyển chuyên mục “{category.Name}” vào Thùng rác.{note}");
    }

    public async Task<AdminResult> ReorderCategoriesAsync(int[] orderedIds)
    {
        var categories = await db.NewsCategories
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
