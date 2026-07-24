namespace RenewZone.Domain.Models;

/// <summary>
/// Dữ liệu form đánh giá của khách đã được tầng Web validate xong (giống <see cref="ContactRequest"/>).
/// Domain không biết gì về MVC. Khách vãng lai nên chỉ có tên hiển thị + sao + nội dung.
/// </summary>
public class ReviewRequest
{
    public required int ProductId { get; init; }
    public required string AuthorName { get; init; }
    public required int Rating { get; init; }
    public required string Content { get; init; }
}
