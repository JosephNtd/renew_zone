namespace RenewZone.Domain.Common;

/// <summary>
/// Entity cần theo dõi thời điểm tạo/sửa (Product, Order, NewsArticle...).
/// CreatedAt/UpdatedAt được AppDbContext tự gán khi SaveChanges — không set tay.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
