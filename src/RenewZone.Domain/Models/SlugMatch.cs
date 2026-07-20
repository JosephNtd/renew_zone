using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Models;

/// <summary>
/// Kết quả tra một slug: biết nó thuộc loại nào và Id bao nhiêu, đủ để controller
/// nạp dữ liệu chi tiết bằng đúng 1 query tiếp theo.
/// </summary>
/// <param name="Type">Loại nội dung.</param>
/// <param name="Id">Khóa chính của bản ghi trong bảng tương ứng.</param>
public sealed record SlugMatch(SlugTargetType Type, int Id);
