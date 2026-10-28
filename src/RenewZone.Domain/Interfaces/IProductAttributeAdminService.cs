using RenewZone.Domain.Models.Admin;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Màn Thuộc tính sản phẩm (Giai đoạn 10): danh sách chung các nhóm biến thể (Dung lượng, Màu sắc…)
/// và giá trị của chúng. Form sản phẩm chỉ chọn từ danh sách này.
/// </summary>
public interface IProductAttributeAdminService
{
    /// <summary>Toàn bộ thuộc tính kèm giá trị, theo thứ tự hiển thị — dùng cho màn quản lý và form sản phẩm.</summary>
    Task<IReadOnlyList<AttributeItem>> GetAllAsync();

    Task<AdminResult> SaveAttributeAsync(AttributeInput input);

    /// <summary>Chỉ xóa được khi chưa sản phẩm nào dùng; xóa thuộc tính xóa luôn các giá trị của nó.</summary>
    Task<AdminResult> DeleteAttributeAsync(int id);

    Task<AdminResult> SaveValueAsync(AttributeValueInput input);

    /// <summary>Chỉ xóa được khi chưa sản phẩm nào dùng giá trị này.</summary>
    Task<AdminResult> DeleteValueAsync(int id);
}
