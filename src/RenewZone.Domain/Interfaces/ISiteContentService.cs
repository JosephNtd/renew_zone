using RenewZone.Domain.Entities;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Nội dung dùng chung toàn site cho _Layout (menu danh mục 2 cấp + SiteSettings).
/// Implementation phải cache — layout gọi ở MỌI request.
/// </summary>
public interface ISiteContentService
{
    Task<IReadOnlyDictionary<string, string?>> GetSettingsAsync();

    /// <summary>Danh mục cha đang active (kèm Children active, đã sắp theo DisplayOrder) cho menu.</summary>
    Task<IReadOnlyList<Category>> GetMenuCategoriesAsync();

    /// <summary>Xóa cache — gọi khi Admin sửa settings/danh mục (Giai đoạn 6).</summary>
    void Invalidate();
}
