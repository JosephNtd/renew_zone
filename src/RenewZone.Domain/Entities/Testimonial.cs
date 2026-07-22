using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Đánh giá khách hàng — carousel testimonials trang chủ. Admin nhập tay, không phải review công khai.
/// </summary>
public class Testimonial : BaseEntity, ISoftDeletable
{
    public string CustomerName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Content { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
