using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="IReviewAdminService"/>.</summary>
public class ReviewAdminService(AppDbContext db) : IReviewAdminService
{
    public async Task<PagedResult<ReviewListItem>> GetListAsync(ReviewListRequest request)
    {
        var query = db.ProductReviews.AsNoTracking().AsQueryable();

        if (request.Status is { } status)
        {
            query = query.Where(r => r.Status == status);
        }

        if (request.NormalizedSearch is { } search)
        {
            query = query.Where(r =>
                r.AuthorName.Contains(search) ||
                (r.Phone != null && r.Phone.Contains(search)) ||
                r.Content.Contains(search) ||
                r.Product.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt) // mới nhất lên đầu
            .Skip(request.Skip)
            .Take(request.SafePageSize)
            .Select(r => new ReviewListItem(
                r.Id, r.ProductId, r.Product.Name, r.Product.Slug,
                r.AuthorName, r.Phone, r.Rating, r.Content, r.Status,
                r.AdminReply, r.RepliedAt, r.CreatedAt))
            .ToListAsync();

        return new PagedResult<ReviewListItem>
        {
            Items = items,
            Page = request.SafePage,
            PageSize = request.SafePageSize,
            TotalItems = totalItems
        };
    }

    public async Task<ReviewStatusCounts> GetCountsAsync()
    {
        // 1 query GROUP BY thay vì 3 câu COUNT
        var grouped = await db.ProductReviews.AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        int CountOf(ReviewStatus s) => grouped.FirstOrDefault(x => x.Key == s)?.Count ?? 0;

        return new ReviewStatusCounts(
            CountOf(ReviewStatus.Pending),
            CountOf(ReviewStatus.Approved),
            CountOf(ReviewStatus.Rejected));
    }

    public async Task<AdminResult> SetStatusAsync(int id, ReviewStatus status)
    {
        var review = await db.ProductReviews.FirstOrDefaultAsync(r => r.Id == id);

        if (review is null)
        {
            return AdminResult.Fail("Không tìm thấy đánh giá.");
        }

        if (review.Status == status)
        {
            return AdminResult.Success("Không có gì thay đổi.");
        }

        review.Status = status;
        await db.SaveChangesAsync();
        await RecomputeRatingAsync(review.ProductId);

        return AdminResult.Success(status switch
        {
            ReviewStatus.Approved => "Đã duyệt đánh giá — hiện trên trang sản phẩm.",
            ReviewStatus.Rejected => "Đã từ chối đánh giá.",
            _ => "Đã chuyển đánh giá về chờ duyệt."
        });
    }

    public async Task<AdminResult> ReplyAsync(int id, string? reply)
    {
        var review = await db.ProductReviews.FirstOrDefaultAsync(r => r.Id == id);

        if (review is null)
        {
            return AdminResult.Fail("Không tìm thấy đánh giá.");
        }

        var trimmed = reply?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            review.AdminReply = null;
            review.RepliedAt = null;
            await db.SaveChangesAsync();
            return AdminResult.Success("Đã gỡ phản hồi.");
        }

        review.AdminReply = trimmed;
        review.RepliedAt = DateTime.Now;
        await db.SaveChangesAsync();

        return AdminResult.Success("Đã lưu phản hồi từ shop.");
    }

    public async Task<AdminResult> DeleteAsync(int id)
    {
        var review = await db.ProductReviews.FirstOrDefaultAsync(r => r.Id == id);

        if (review is null)
        {
            return AdminResult.Fail("Không tìm thấy đánh giá.");
        }

        db.ProductReviews.Remove(review); // soft delete qua AppDbContext
        await db.SaveChangesAsync();
        await RecomputeRatingAsync(review.ProductId);

        return AdminResult.Success("Đã xóa đánh giá.");
    }

    private Task RecomputeRatingAsync(int productId) => db.RecomputeProductRatingAsync(productId);
}
