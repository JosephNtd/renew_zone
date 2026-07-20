using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Một giá trị trong nhóm tùy chọn, vd "512GB" thuộc nhóm "Dung lượng".
/// PriceAdjustment cộng/trừ vào giá sản phẩm khi khách chọn (vd 512GB +3.000.000đ).
/// </summary>
public class ProductOptionValue : BaseEntity
{
    public int GroupId { get; set; }
    public string Value { get; set; } = null!;
    public decimal PriceAdjustment { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }

    public ProductOptionGroup Group { get; set; } = null!;
}
