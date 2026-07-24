using RenewZone.Domain.Common;
using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Đánh giá + bình luận của khách trên trang chi tiết sản phẩm (Giai đoạn 8).
/// Khách vãng lai (không tài khoản) chỉ nhập tên hiển thị + số sao + nội dung; mọi bản ghi
/// vào trạng thái <see cref="ReviewStatus.Pending"/> chờ admin duyệt trước khi hiện ra web khách.
/// Admin có thể trả lời công khai (<see cref="AdminReply"/> — hiện dạng "Phản hồi từ shop").
/// </summary>
public class ProductReview : AuditableEntity, ISoftDeletable
{
    public int ProductId { get; set; }

    /// <summary>Tên khách tự nhập (không xác thực) — chỉ để hiển thị.</summary>
    public string AuthorName { get; set; } = null!;

    /// <summary>Số sao 1–5. Validate cứng ở server, không tin giá trị client gửi.</summary>
    public int Rating { get; set; }

    public string Content { get; set; } = null!;

    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    /// <summary>Câu trả lời công khai của shop — null ⇒ chưa phản hồi.</summary>
    public string? AdminReply { get; set; }

    public DateTime? RepliedAt { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    public Product Product { get; set; } = null!;
}
