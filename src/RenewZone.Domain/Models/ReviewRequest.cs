namespace RenewZone.Domain.Models;

/// <summary>
/// Dữ liệu form đánh giá của khách đã được tầng Web validate xong (giống <see cref="ContactRequest"/>).
/// Domain không biết gì về MVC.
/// <para>
/// Từ Giai đoạn 9, người gửi bắt buộc đăng nhập VÀ đã mua sản phẩm — nên có thêm
/// <see cref="UserId"/>/<see cref="IsVerifiedPurchase"/>, còn <see cref="AuthorName"/> do
/// controller lấy từ hồ sơ tài khoản chứ không phải khách tự khai.
/// </para>
/// </summary>
public class ReviewRequest
{
    public required int ProductId { get; init; }

    /// <summary>Tài khoản viết đánh giá. Controller lấy từ claim — không bao giờ nhận từ body.</summary>
    public string? UserId { get; init; }

    /// <summary>Đã đối chiếu được đơn hoàn thành chứa sản phẩm này tại thời điểm gửi.</summary>
    public bool IsVerifiedPurchase { get; init; }

    public required string AuthorName { get; init; }
    public required int Rating { get; init; }
    public required string Content { get; init; }

    /// <summary>SĐT tùy chọn để admin liên hệ lại — null/rỗng khi khách không điền.</summary>
    public string? Phone { get; init; }
}
