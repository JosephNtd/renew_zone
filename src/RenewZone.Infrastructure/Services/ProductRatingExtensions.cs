using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Enums;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Dùng chung cho màn Đánh giá sản phẩm và Thùng rác (khôi phục / xóa vĩnh viễn đánh giá).</summary>
internal static class ProductRatingExtensions
{
    /// <summary>
    /// Tính lại điểm trung bình + số lượng đánh giá ĐÃ DUYỆT của sản phẩm.
    /// Gọi SAU khi thay đổi review đã được lưu (query dưới đây chạy trên DB, nên phải phản ánh
    /// trạng thái mới nhất; review vừa xóa mềm tự bị loại bởi global query filter). Việc này ghi
    /// vào cột phi chuẩn hóa mà card + PDP đọc thẳng.
    /// </summary>
    public static async Task RecomputeProductRatingAsync(this AppDbContext db, int productId)
    {
        var product = await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
        {
            return; // sản phẩm đã bị xóa cứng ⇒ không còn gì để cập nhật
        }

        var approved = db.ProductReviews.Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved);
        var count = await approved.CountAsync();

        product.ReviewCount = count;
        product.AvgRating = count == 0
            ? 0m
            : Math.Round((decimal)await approved.AverageAsync(r => r.Rating), 2);

        await db.SaveChangesAsync();
    }
}
