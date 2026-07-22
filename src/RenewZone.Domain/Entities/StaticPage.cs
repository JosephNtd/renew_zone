using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Trang nội dung tĩnh render theo slug: /gioi-thieu + toàn bộ trang chính sách.
/// Thêm trang mới chỉ cần thêm bản ghi, không cần code.
/// </summary>
public class StaticPage : AuditableEntity, ISoftDeletable
{
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string ContentHtml { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
