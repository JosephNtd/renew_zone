using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <inheritdoc cref="IReviewService" />
public class ReviewService(AppDbContext db) : IReviewService
{
    public async Task<ProductReview?> SubmitAsync(ReviewRequest request)
    {
        // Không tin ProductId client gửi: chỉ nhận đánh giá cho sản phẩm còn tồn tại + đang bán
        var exists = await db.Products.AnyAsync(p => p.Id == request.ProductId && p.IsActive);
        if (!exists)
        {
            return null;
        }

        var review = new ProductReview
        {
            ProductId = request.ProductId,
            UserId = request.UserId,
            IsVerifiedPurchase = request.IsVerifiedPurchase,
            AuthorName = request.AuthorName,
            Phone = request.Phone,
            Rating = request.Rating,
            Content = request.Content,
            Status = ReviewStatus.Pending
            // CreatedAt do AppDbContext tự gán (AuditableEntity)
        };

        db.ProductReviews.Add(review);
        await db.SaveChangesAsync();

        return review;
    }

    public async Task<IReadOnlyList<ProductReview>> GetApprovedAsync(int productId) =>
        await db.ProductReviews.AsNoTracking()
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
}
