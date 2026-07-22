using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Cấp menu cho layout. Có cache in-memory (TTL 5 phút) vì layout đọc mỗi request —
/// Admin sửa menu ở Giai đoạn 6 phải gọi <see cref="Invalidate"/>.
/// </summary>
public interface INavigationService
{
    /// <summary>Trả về các mục gốc của một nhóm — đã lọc IsActive, đã Include Children, đã sắp xếp.</summary>
    Task<IReadOnlyList<MenuItem>> GetMenuAsync(MenuGroup group);

    void Invalidate();
}
