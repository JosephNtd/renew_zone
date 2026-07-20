using RenewZone.Domain.Models;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Tra slug phẳng ở root về đúng loại nội dung — trái tim của fallback route "{slug}"
/// (CLAUDE.md mục 4.3). Chạy trên MỌI request không khớp route tĩnh nên implementation
/// bắt buộc phải cache, không được query 4 bảng mỗi lần.
/// </summary>
public interface ISlugResolver
{
    /// <summary>Trả null nếu không bảng nào có slug này (⇒ controller trả 404).</summary>
    Task<SlugMatch?> ResolveAsync(string slug);

    /// <summary>Xóa cache — gọi khi Admin thêm/sửa/xóa slug bất kỳ (Giai đoạn 6).</summary>
    void Invalidate();
}
