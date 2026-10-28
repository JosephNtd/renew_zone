using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Danh sách chung các loại thuộc tính biến thể, vd "Dung lượng", "Màu sắc", "Tình trạng", "Loại SIM".
/// Admin quản lý ở màn Thuộc tính sản phẩm; form sản phẩm chỉ CHỌN từ danh sách này chứ không gõ tay,
/// nên cùng một nhóm không bị viết thành nhiều kiểu ("Màu sắc" / "Màu" / "màu sắc").
/// </summary>
public class ProductAttribute : BaseEntity
{
    public string Name { get; set; } = null!;
    public int DisplayOrder { get; set; }

    public ICollection<ProductAttributeValue> Values { get; set; } = [];
}
