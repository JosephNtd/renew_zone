using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Một NHÓM tùy chọn của sản phẩm, vd "Dung lượng", "Màu sắc", "Tình trạng", "Loại SIM".
/// Trang chi tiết render mỗi group thành 1 hàng nút chọn — đúng UI site gốc.
/// </summary>
public class ProductOptionGroup : BaseEntity
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public int DisplayOrder { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductOptionValue> Values { get; set; } = [];
}
