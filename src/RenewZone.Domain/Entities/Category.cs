using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Danh mục sản phẩm 2 cấp: cha (iPhone, MacBook...) → con là dòng/series (iPhone 16 Series...).
/// Cấp con hiển thị thành tab lọc trên trang danh mục — đúng UI site gốc.
/// </summary>
public class Category : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = null!;

    /// <summary>Slug phẳng tại root (vd "iphone") — unique toàn cục, xem Lưu ý 6.4 trong CLAUDE.md.</summary>
    public string Slug { get; set; } = null!;

    public int? ParentId { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>Class icon Bootstrap Icons hiện ở menu, vd "bi-phone". Null ⇒ view dùng icon mặc định.</summary>
    public string? IconClass { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}
