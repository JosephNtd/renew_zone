using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Models.Admin;

public class ReviewListRequest : AdminListRequest
{
    public ReviewStatus? Status { get; init; }
}

public record ReviewListItem(
    int Id, int ProductId, string ProductName, string ProductSlug,
    string AuthorName, string? Phone, int Rating, string Content, ReviewStatus Status,
    string? AdminReply, DateTime? RepliedAt, DateTime CreatedAt);

/// <summary>Số đánh giá theo từng trạng thái — dựng tab lọc + badge, đọc từ dữ liệu THẬT.</summary>
public record ReviewStatusCounts(int Pending, int Approved, int Rejected)
{
    public int Total => Pending + Approved + Rejected;
}
