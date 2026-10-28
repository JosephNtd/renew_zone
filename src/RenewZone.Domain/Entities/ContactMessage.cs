using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Tin nhắn từ form /lien-he (đủ field như site gốc, kèm file đính kèm).
/// Admin đọc và đánh dấu IsRead ở Giai đoạn 6.
/// </summary>
public class ContactMessage : AuditableEntity, ISoftDeletable
{
    /// <summary>
    /// Tài khoản đã gửi tin (khóa ngoại <c>AspNetUsers</c>, không navigation — xem <c>Order.UserId</c>).
    /// NULL = khách vãng lai gửi, hoặc tài khoản đã bị xóa (SetNull). Nhờ cột này admin biết tin
    /// của khách nào và mở được hồ sơ khách từ tin nhắn.
    /// </summary>
    public string? UserId { get; set; }

    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Subject { get; set; }
    public string Content { get; set; } = null!;

    /// <summary>Đường dẫn file khách đính kèm, lưu trong wwwroot/uploads.</summary>
    public string? AttachmentPath { get; set; }

    public bool IsRead { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
