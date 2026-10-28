using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="IContactAdminService"/>.</summary>
public class ContactAdminService(AppDbContext db) : IContactAdminService
{
    public async Task<PagedResult<ContactListItem>> GetListAsync(ContactListRequest request)
    {
        var query = db.ContactMessages.AsNoTracking().AsQueryable();

        if (request.IsRead is { } isRead)
        {
            query = query.Where(c => c.IsRead == isRead);
        }

        if (!string.IsNullOrWhiteSpace(request.Subject))
        {
            query = query.Where(c => c.Subject == request.Subject);
        }

        if (request.NormalizedSearch is { } search)
        {
            query = query.Where(c =>
                c.FullName.Contains(search) ||
                c.Phone.Contains(search) ||
                c.Content.Contains(search));
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt) // mới nhất lên đầu, như mọi hộp thư
            .Skip(request.Skip)
            .Take(request.SafePageSize)
            .Select(c => new ContactListItem(
                c.Id, c.FullName, c.Phone, c.Email, c.Address,
                c.Subject, c.Content, c.AttachmentPath, c.IsRead, c.CreatedAt, c.UserId))
            .ToListAsync();

        return new PagedResult<ContactListItem>
        {
            Items = items,
            Page = request.SafePage,
            PageSize = request.SafePageSize,
            TotalItems = totalItems
        };
    }

    public async Task<ContactListItem?> GetAsync(int id) =>
        await db.ContactMessages.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ContactListItem(
                c.Id, c.FullName, c.Phone, c.Email, c.Address,
                c.Subject, c.Content, c.AttachmentPath, c.IsRead, c.CreatedAt, c.UserId))
            .FirstOrDefaultAsync();

    public async Task<AdminResult> MarkReadAsync(int id, bool isRead = true)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(c => c.Id == id);

        if (message is null)
        {
            return AdminResult.Fail("Không tìm thấy tin nhắn.");
        }

        if (message.IsRead == isRead)
        {
            return AdminResult.Success("Không có gì thay đổi."); // tránh UPDATE thừa khi mở lại
        }

        message.IsRead = isRead;
        await db.SaveChangesAsync();

        return AdminResult.Success(isRead ? "Đã đánh dấu đã đọc." : "Đã đánh dấu chưa đọc.");
    }

    public async Task<AdminResult> DeleteAsync(int id)
    {
        var message = await db.ContactMessages.FirstOrDefaultAsync(c => c.Id == id);

        if (message is null)
        {
            return AdminResult.Fail("Không tìm thấy tin nhắn.");
        }

        db.ContactMessages.Remove(message);
        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã chuyển tin nhắn của “{message.FullName}” vào Thùng rác.");
    }

    public async Task<IReadOnlyList<string>> GetSubjectsAsync() =>
        await db.ContactMessages.AsNoTracking()
            .Where(c => c.Subject != null && c.Subject != "")
            .Select(c => c.Subject!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
}
