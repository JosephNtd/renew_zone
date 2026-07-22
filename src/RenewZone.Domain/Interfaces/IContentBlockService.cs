using RenewZone.Domain.Entities;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Cấp các khối nội dung theo GroupKey (cam kết PDP, trấn an giỏ hàng, kênh liên hệ...).
/// Cache in-memory TTL 5 phút; Admin sửa xong gọi <see cref="Invalidate"/>.
/// </summary>
public interface IContentBlockService
{
    /// <summary>Danh sách khối của một nhóm — đã lọc IsActive, đã sắp xếp. Rỗng ⇒ view ẩn cả khối.</summary>
    Task<IReadOnlyList<ContentBlock>> GetAsync(string groupKey);

    void Invalidate();
}
