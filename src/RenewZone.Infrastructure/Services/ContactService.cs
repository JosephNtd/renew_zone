using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <inheritdoc cref="IContactService" />
public class ContactService(AppDbContext db) : IContactService
{
    public async Task<ContactMessage> SubmitAsync(ContactRequest request)
    {
        var message = new ContactMessage
        {
            UserId = request.UserId,
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            Subject = request.Subject,
            Content = request.Content,
            AttachmentPath = request.AttachmentPath,
            IsRead = false
            // CreatedAt do AppDbContext tự gán (AuditableEntity) — xem CLAUDE.md Lưu ý 6.10
        };

        db.ContactMessages.Add(message);
        await db.SaveChangesAsync();

        return message;
    }
}
