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
/// <param name="IsReserved">
/// Slug trùng một ĐƯỜNG DẪN CỐ ĐỊNH của website (<c>/gio-hang</c>, <c>/dang-nhap</c>...).
/// Không bảng nào giữ nó nên 3 kiểm tra ở trên đều báo "trống", nhưng route tĩnh luôn thắng
/// fallback <c>{slug}</c> ⇒ lưu được mà trang thì VĨNH VIỄN không mở ra được. Lỗi thầm lặng
/// nhất trong nhóm, phải chặn ngay tại form.
/// </param>
public record SlugCheckResult(
    bool IsAvailable,
    SlugTargetType? TakenBy,
    string? TakenByName,
    bool IsInTrash,
    bool IsReserved = false)
{
    public static SlugCheckResult Available() => new(true, null, null, false);

    public static SlugCheckResult Reserved() => new(false, null, null, false, true);

    /// <summary>Thông báo tiếng Việt hoàn chỉnh để hiện thẳng lên form (null khi slug hợp lệ).</summary>
    public string? Message => IsAvailable
        ? null
        : IsReserved
            ? "Slug này trùng với một đường dẫn có sẵn của website (giỏ hàng, đăng nhập, tìm kiếm...). Trang sẽ không bao giờ mở được — hãy chọn slug khác."
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
