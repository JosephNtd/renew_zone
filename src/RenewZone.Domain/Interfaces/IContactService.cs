using RenewZone.Domain.Entities;
using RenewZone.Domain.Models;

namespace RenewZone.Domain.Interfaces;

/// <summary>Ghi tin nhắn liên hệ xuống DB để Admin đọc ở Giai đoạn 6.</summary>
public interface IContactService
{
    Task<ContactMessage> SubmitAsync(ContactRequest request);
}
