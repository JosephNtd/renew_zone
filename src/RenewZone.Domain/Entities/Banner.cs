using RenewZone.Domain.Common;
using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Banner quảng cáo — Position quyết định chỗ render (slider đầu trang, khối giữa trang chủ...).
/// </summary>
public class Banner : BaseEntity, ISoftDeletable
{
    public string? Title { get; set; }
    public string ImageUrl { get; set; } = null!;

    /// <summary>Link đích khi bấm banner (thường trỏ trang sản phẩm/khuyến mãi). Null ⇒ không bấm được.</summary>
    public string? LinkUrl { get; set; }

    public BannerPosition Position { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
