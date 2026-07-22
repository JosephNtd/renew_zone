using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Models.Admin;

/// <summary>
/// Kết quả kiểm slug. Không chỉ trả true/false mà nói rõ AI đang giữ slug đó —
/// admin cần biết "phải làm gì" chứ không phải chỉ "không được".
/// </summary>
/// <param name="IsAvailable">true ⇒ dùng được.</param>
/// <param name="TakenBy">Loại nội dung đang giữ slug (null khi còn trống).</param>
/// <param name="TakenByName">Tên bản ghi đang giữ slug — để ghép vào thông báo.</param>
/// <param name="IsInTrash">
/// Bản ghi đang giữ slug nằm trong Thùng rác. Trường hợp này rất dễ gây hoang mang:
/// admin không thấy nó ở đâu cả nhưng unique index của DB vẫn tính ⇒ phải báo riêng.
/// </param>
public record SlugCheckResult(
    bool IsAvailable,
    SlugTargetType? TakenBy,
    string? TakenByName,
    bool IsInTrash)
{
    public static SlugCheckResult Available() => new(true, null, null, false);

    /// <summary>Thông báo tiếng Việt hoàn chỉnh để hiện thẳng lên form (null khi slug hợp lệ).</summary>
    public string? Message => IsAvailable
        ? null
        : IsInTrash
            ? $"Slug này đang thuộc về {TypeLabel} «{TakenByName}» trong Thùng rác. Hãy xóa vĩnh viễn mục đó hoặc chọn slug khác."
            : $"Slug này đã được dùng bởi {TypeLabel} «{TakenByName}».";

    private string TypeLabel => TakenBy switch
    {
        SlugTargetType.Category => "danh mục",
        SlugTargetType.Product => "sản phẩm",
        SlugTargetType.NewsArticle => "bài viết",
        SlugTargetType.StaticPage => "trang tĩnh",
        _ => "một mục khác"
    };
}
