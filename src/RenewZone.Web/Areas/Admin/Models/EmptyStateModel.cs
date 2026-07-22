namespace RenewZone.Web.Areas.Admin.Models;

/// <summary>
/// Trạng thái rỗng dùng chung. Không bao giờ để bảng trống trơ trọi — phải nói rõ
/// "chưa có gì" và "làm gì tiếp theo".
/// </summary>
public class EmptyStateModel
{
    /// <summary>Class Bootstrap Icons, vd "bi-inbox".</summary>
    public string Icon { get; init; } = "bi-inbox";

    public required string Title { get; init; }

    public string? Message { get; init; }

    /// <summary>Nút hành động chính. Null ⇒ chỉ hiện thông điệp.</summary>
    public string? ActionText { get; init; }

    public string? ActionUrl { get; init; }
}
