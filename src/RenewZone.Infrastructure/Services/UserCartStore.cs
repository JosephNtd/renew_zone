using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <inheritdoc cref="IUserCartStore" />
public class UserCartStore(AppDbContext db) : IUserCartStore
{
    public Task<string?> LoadAsync(string userId) =>
        db.UserCarts.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => (string?)c.ItemsJson)
            .FirstOrDefaultAsync();

    public async Task SaveAsync(string userId, string itemsJson)
    {
        var cart = await db.UserCarts.FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null)
        {
            cart = new UserCart { UserId = userId };
            db.UserCarts.Add(cart);
        }

        cart.ItemsJson = itemsJson;
        cart.UpdatedAt = DateTime.Now; // UserCart là BaseEntity ⇒ không có hook audit

        await db.SaveChangesAsync();
    }

    public async Task ClearAsync(string userId)
    {
        // ExecuteDelete: xóa thẳng bằng 1 câu lệnh, không cần nạp entity. UserCart KHÔNG thuộc
        // ISoftDeletable nên không có gì để "xóa mềm" nhầm, và giỏ hàng cũng không nằm trong
        // bảng cache nào của CacheInvalidator ⇒ không phải invalidate tay (khác Thùng rác).
        await db.UserCarts.Where(c => c.UserId == userId).ExecuteDeleteAsync();
    }
}
