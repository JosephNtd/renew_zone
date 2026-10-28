using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Một NHÓM tùy chọn của sản phẩm, vd "Dung lượng", "Màu sắc", "Tình trạng", "Loại SIM".
/// Trang chi tiết render mỗi group thành 1 hàng nút chọn — đúng UI site gốc.
/// <para>
/// Tên nhóm lấy từ danh sách chung <see cref="ProductAttribute"/> (không lưu chữ riêng ở đây),
/// mỗi sản phẩm chỉ có tối đa một nhóm cho mỗi thuộc tính.
/// </para>
/// </summary>
public class ProductOptionGroup : BaseEntity
{
    public int ProductId { get; set; }
    public int AttributeId { get; set; }
    public int DisplayOrder { get; set; }

    public Product Product { get; set; } = null!;
    public ProductAttribute Attribute { get; set; } = null!;
    public ICollection<ProductOptionValue> Values { get; set; } = [];
}
