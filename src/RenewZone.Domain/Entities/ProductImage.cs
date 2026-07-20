using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Ảnh sản phẩm — gallery + thumbnails ở trang chi tiết. Ảnh IsMain dùng cho card sản phẩm.
/// </summary>
public class ProductImage : BaseEntity
{
    public int ProductId { get; set; }
    public string Url { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsMain { get; set; }

    public Product Product { get; set; } = null!;
}
