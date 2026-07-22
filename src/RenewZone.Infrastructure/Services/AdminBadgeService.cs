using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="IAdminBadgeService"/>. Hai câu COUNT, không Include gì thêm.</summary>
public class AdminBadgeService(AppDbContext db) : IAdminBadgeService
{
    public async Task<AdminBadgeCounts> GetCountsAsync()
    {
        var newOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.New);

        // ContactMessages có global query filter ⇒ tin đã xóa mềm tự động không được đếm
        var unreadContacts = await db.ContactMessages.CountAsync(c => !c.IsRead);

        return new AdminBadgeCounts(newOrders, unreadContacts);
    }
}
