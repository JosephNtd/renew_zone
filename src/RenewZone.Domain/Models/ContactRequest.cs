namespace RenewZone.Domain.Models;

/// <summary>
/// Dữ liệu form liên hệ đã được tầng Web validate + xử lý file xong.
/// Giống pattern <see cref="OrderRequest"/>: Domain/Infrastructure không biết gì về MVC hay IFormFile —
/// tầng Web lưu file rồi chỉ truyền xuống đường dẫn tương đối.
/// </summary>
public class ContactRequest
{
    /// <summary>Tài khoản đang đăng nhập khi gửi (controller lấy từ claim). Null = khách vãng lai.</summary>
    public string? UserId { get; init; }

    public required string FullName { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Subject { get; init; }
    public required string Content { get; init; }

    /// <summary>Đường dẫn tương đối tới file đã lưu trong wwwroot (vd "/uploads/contact/2026-07/ab12.jpg").</summary>
    public string? AttachmentPath { get; init; }
}
