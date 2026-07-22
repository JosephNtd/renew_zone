using RenewZone.Domain.Entities;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Domain.Interfaces;

/// <summary>CRUD danh mục 2 cấp cho admin.</summary>
public interface ICategoryAdminService
{
    /// <summary>Cây 2 cấp kèm số sản phẩm từng nhánh (gồm cả danh mục đang tắt).</summary>
    Task<IReadOnlyList<CategoryTreeNode>> GetTreeAsync();

    Task<Category?> GetAsync(int id);

    /// <summary>
    /// Danh sách chọn "danh mục cha". Khi SỬA phải loại chính nó + toàn bộ con cháu,
    /// nếu không admin đặt được danh mục làm cha của chính nó ⇒ cây vòng lặp, trang danh mục treo.
    /// </summary>
    Task<IReadOnlyList<Category>> GetParentOptionsAsync(int? excludeId = null);

    Task<AdminResult> CreateAsync(CategoryInput input);
    Task<AdminResult> UpdateAsync(CategoryInput input);

    /// <summary>Xóa mềm. Chặn khi còn sản phẩm hoặc còn danh mục con (đúng ràng buộc Restrict của DB).</summary>
    Task<AdminResult> DeleteAsync(int id);

    Task<AdminResult> ToggleActiveAsync(int id);

    /// <summary>Lưu lại thứ tự sau khi kéo-thả. Mảng id theo đúng thứ tự mới, trong cùng một cấp.</summary>
    Task<AdminResult> ReorderAsync(int[] orderedIds);
}
