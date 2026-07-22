using RenewZone.Domain.Enums;

namespace RenewZone.Web.Areas.Admin.Models;

/// <summary>
/// Ô nhập slug dùng chung (tự sinh từ ô Tên + kiểm trùng realtime).
/// Slug phải unique XUYÊN 4 BẢNG nên mọi form có slug đều dùng đúng partial này —
/// tự viết lại ở từng màn là cách chắc chắn để một màn nào đó quên mất phần kiểm.
/// </summary>
public class SlugInputModel
{
    /// <summary>Tên field trong form, vd "Slug" — dùng cho name/id/asp-validation.</summary>
    public required string FieldName { get; init; }

    public required string? Value { get; init; }

    /// <summary>Selector CSS của ô Tên để tự sinh slug, vd "#Name". Null ⇒ không tự sinh.</summary>
    public string? SourceSelector { get; init; }

    /// <summary>Loại nội dung — để API loại chính bản ghi đang sửa ra khỏi phép kiểm trùng.</summary>
    public required SlugTargetType TargetType { get; init; }

    public int? ExcludeId { get; init; }
}
