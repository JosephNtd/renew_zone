using RenewZone.Domain.Entities;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Domain.Interfaces;

/// <summary>CRUD sản phẩm + ảnh + biến thể.</summary>
public interface IProductAdminService
{
    Task<PagedResult<ProductListItem>> GetListAsync(ProductListRequest request);

    /// <summary>Nạp đủ ảnh + nhóm biến thể để dựng form 5 tab.</summary>
    Task<Product?> GetForEditAsync(int id);

    Task<AdminResult> CreateAsync(ProductInput input);
    Task<AdminResult> UpdateAsync(ProductInput input);

    /// <summary>Xóa mềm — đơn hàng cũ vẫn giữ snapshot tên/giá nên không mất lịch sử.</summary>
    Task<AdminResult> DeleteAsync(int id);

    Task<AdminResult> ToggleActiveAsync(int id);

    // ----- Ảnh (thao tác AJAX riêng, không đi qua form chính) -----

    Task<IReadOnlyList<ProductImageItem>> GetImagesAsync(int productId);

    /// <summary>Thêm ảnh đã upload. Sản phẩm chưa có ảnh nào ⇒ ảnh đầu tiên tự thành ảnh chính.</summary>
    Task<AdminResult> AddImagesAsync(int productId, IReadOnlyList<string> urls);

    /// <summary>Trả về đường dẫn file để controller nhờ IMediaService xóa file vật lý.</summary>
    Task<(AdminResult Result, string? RemovedUrl)> DeleteImageAsync(int productId, int imageId);

    Task<AdminResult> ReorderImagesAsync(int productId, int[] orderedIds);
    Task<AdminResult> SetMainImageAsync(int productId, int imageId);
}
