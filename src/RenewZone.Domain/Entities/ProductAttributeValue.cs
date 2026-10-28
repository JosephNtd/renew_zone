using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Một giá trị trong danh sách chung của thuộc tính, vd "256GB" thuộc "Dung lượng".
/// Sản phẩm dùng giá trị này qua <see cref="ProductOptionValue"/> (kèm chênh lệch giá riêng của
/// từng sản phẩm). Đổi tên ở đây thì mọi sản phẩm đang dùng đều đổi theo.
/// </summary>
public class ProductAttributeValue : BaseEntity
{
    public int AttributeId { get; set; }
    public string Value { get; set; } = null!;
    public int DisplayOrder { get; set; }

    public ProductAttribute Attribute { get; set; } = null!;
}
