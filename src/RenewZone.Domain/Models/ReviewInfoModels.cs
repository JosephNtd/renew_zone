using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Models;

/// <summary>Đánh giá giao hàng của một đơn (Giai đoạn 10) — dùng chung cho trang khách và trang quản trị.</summary>
public record DeliveryReviewInfo(int Rating, string? Comment, DateTime CreatedAt);

/// <summary>
/// Đánh giá khách đã viết cho một dòng đơn hàng — trang chi tiết đơn dùng để thay nút "Đánh giá"
/// bằng số sao + trạng thái. <paramref name="IsRemoved"/> = shop đã gỡ (xóa) đánh giá đó.
/// </summary>
public record ItemReviewInfo(int Rating, ReviewStatus Status, bool IsRemoved);

/// <summary>Dữ liệu khách gửi khi đánh giá giao hàng (kiểm lại ở service, không tin client).</summary>
public record DeliveryReviewRequest(int Rating, string? Comment);
