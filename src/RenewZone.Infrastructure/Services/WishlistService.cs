using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Account;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <inheritdoc cref="IWishlistService" />
public class WishlistService(AppDbContext db) : IWishlistService
{
    /// <summary>Trần số mục yêu thích — chặn kịch bản bot spam bấm tim làm phình bảng.</summary>
    private const int MaxItemsPerUser = 200;

    public async Task<IReadOnlyList<WishlistEntry>> GetMyWishlistAsync(string userId)
    {
        // JOIN tường minh với db.Products thay vì đi qua navigation w.Product: Product có global
        // query filter xóa mềm, navigation sẽ sinh LEFT JOIN và trả về null cho sản phẩm đã bị
        // đưa vào Thùng rác ⇒ projection nổ NullReference. INNER JOIN thì hàng đó biến mất êm.
        return await (
            from w in db.WishlistItems.AsNoTracking()
            join p in db.Products.AsNoTracking() on w.ProductId equals p.Id
            where w.UserId == userId
            orderby w.CreatedAt descending
            select new WishlistEntry(
                p.Id,
                p.Name,
                p.Slug,
                p.Images.Where(i => i.IsMain).Select(i => i.Url).FirstOrDefault()
                    ?? p.Images.Select(i => i.Url).FirstOrDefault(),
                p.OriginalPrice,
                p.SalePrice,
                p.IsContactPrice,
                p.AvgRating,
                p.ReviewCount,
                p.IsActive,
                w.CreatedAt)).ToListAsync();
    }

    public async Task<WishlistToggleResult> ToggleAsync(string userId, int productId)
    {
        var existing = await db.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

        if (existing is not null)
        {
            db.WishlistItems.Remove(existing); // WishlistItem KHÔNG xóa mềm ⇒ DELETE thật
            await db.SaveChangesAsync();

            return new WishlistToggleResult(true, false, await CountAsync(userId),
                "Đã bỏ khỏi danh sách yêu thích.");
        }

        // Không tin productId từ client: phải là sản phẩm CÓ THẬT và đang bán
        var exists = await db.Products.AnyAsync(p => p.Id == productId && p.IsActive);
        if (!exists)
        {
            return new WishlistToggleResult(false, false, await CountAsync(userId),
                "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");
        }

        if (await CountAsync(userId) >= MaxItemsPerUser)
        {
            return new WishlistToggleResult(false, false, await CountAsync(userId),
                $"Danh sách yêu thích đã đạt tối đa {MaxItemsPerUser} sản phẩm.");
        }

        db.WishlistItems.Add(new WishlistItem
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.Now // WishlistItem là BaseEntity ⇒ không có hook audit, phải set tay
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Đụng unique index (UserId, ProductId): hai request bấm tim gần như đồng thời (double
            // click, hoặc 2 tab). Kết quả mong muốn — "đã thích" — vẫn đạt được, nên báo thành công
            // thay vì ném 500 vào mặt khách.
            return new WishlistToggleResult(true, true, await CountAsync(userId),
                "Đã thêm vào danh sách yêu thích.");
        }

        return new WishlistToggleResult(true, true, await CountAsync(userId),
            "Đã thêm vào danh sách yêu thích.");
    }

    public async Task<IReadOnlySet<int>> GetFavoriteProductIdsAsync(string userId) =>
        (await db.WishlistItems.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => w.ProductId)
            .ToListAsync())
        .ToHashSet();

    public Task<int> CountAsync(string userId) =>
        db.WishlistItems.CountAsync(w => w.UserId == userId);
}
