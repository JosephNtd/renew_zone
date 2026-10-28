using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Một giá trị trong nhóm tùy chọn của sản phẩm, vd "512GB" thuộc nhóm "Dung lượng".
/// Chữ hiển thị lấy từ danh sách chung <see cref="ProductAttributeValue"/>; phần riêng của sản phẩm
/// là PriceAdjustment cộng/trừ vào giá khi khách chọn (vd 512GB +3.000.000đ) và lựa chọn mặc định.
/// </summary>
public class ProductOptionValue : BaseEntity
{
    public int GroupId { get; set; }
    public int AttributeValueId { get; set; }
    public decimal PriceAdjustment { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }

    public ProductOptionGroup Group { get; set; } = null!;
    public ProductAttributeValue AttributeValue { get; set; } = null!;
}
