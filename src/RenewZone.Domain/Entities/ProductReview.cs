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

    /// <summary>
    /// Tài khoản đã viết đánh giá (khóa ngoại <c>AspNetUsers</c>, không navigation — xem
    /// <c>Order.UserId</c>). NULL = đánh giá của khách vãng lai từ Giai đoạn 8, giữ nguyên
    /// để không mất dữ liệu cũ.
    /// <para>
    /// Từ Giai đoạn 9 chỉ tài khoản ĐÃ MUA sản phẩm này (đơn Hoàn thành) mới gửi được đánh giá
    /// ⇒ mọi bản ghi mới đều có UserId, và <see cref="IsVerifiedPurchase"/> luôn true.
    /// </para>
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Đã đối chiếu được với đơn hàng hoàn thành của chính người viết tại thời điểm gửi.
    /// Chốt cứng thành cột thay vì kiểm lại lúc hiển thị: đơn có thể bị admin đổi trạng thái
    /// hoặc sản phẩm bị gỡ về sau, nhưng huy hiệu "Đã mua hàng" phản ánh sự thật LÚC ĐÁNH GIÁ.
    /// </summary>
    public bool IsVerifiedPurchase { get; set; }

    /// <summary>Tên khách tự nhập (không xác thực) — chỉ để hiển thị.</summary>
    public string AuthorName { get; set; } = null!;

    /// <summary>
    /// Số điện thoại khách tự nhập — TÙY CHỌN, để admin liên hệ lại nếu cần (khiếu nại, tư vấn).
    /// Là THÔNG TIN CÁ NHÂN: chỉ hiện ở màn admin, TUYỆT ĐỐI không render ra web khách cạnh bình luận.
    /// </summary>
    public string? Phone { get; set; }

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
