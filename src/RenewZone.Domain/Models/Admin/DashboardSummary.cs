using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Models.Admin;

/// <summary>Toàn bộ số liệu trang tổng quan — gom vào 1 DTO để controller chỉ gọi service 1 lần.</summary>
public class DashboardSummary
{
    public required int NewOrderCount { get; init; }

    /// <summary>Doanh thu 30 ngày gần nhất — CHỈ tính đơn Completed (đơn chưa giao xong chưa phải tiền thật).</summary>
    public required decimal Revenue30Days { get; init; }

    /// <summary>Doanh thu 30 ngày liền trước, để tính % tăng/giảm.</summary>
    public required decimal RevenuePrevious30Days { get; init; }

    public required int ActiveProductCount { get; init; }
    public required int UnreadContactCount { get; init; }

    /// <summary>Doanh thu theo từng ngày trong 30 ngày qua — ĐỦ 30 phần tử kể cả ngày không có đơn.</summary>
    public required IReadOnlyList<RevenuePoint> RevenueByDay { get; init; }

    public required IReadOnlyDictionary<OrderStatus, int> OrderCountByStatus { get; init; }

    public required IReadOnlyList<RecentOrder> RecentOrders { get; init; }
    public required IReadOnlyList<RecentContact> RecentContacts { get; init; }

    /// <summary>Cảnh báo dữ liệu thiếu — rỗng nghĩa là mọi thứ đều ổn.</summary>
    public required IReadOnlyList<DataWarning> Warnings { get; init; }

    /// <summary>% thay đổi doanh thu so với 30 ngày trước. Null khi kỳ trước = 0 (không chia được).</summary>
    public double? RevenueChangePercent => RevenuePrevious30Days == 0
        ? null
        : (double)((Revenue30Days - RevenuePrevious30Days) / RevenuePrevious30Days) * 100;

    public decimal MaxDailyRevenue => RevenueByDay.Count == 0 ? 0 : RevenueByDay.Max(p => p.Amount);

    public int TotalOrderCount => OrderCountByStatus.Values.Sum();
}

public record RevenuePoint(DateTime Date, decimal Amount, int OrderCount);

public record RecentOrder(
    int Id, string Code, string CustomerName, string Phone,
    decimal TotalAmount, OrderStatus Status, DateTime CreatedAt);

public record RecentContact(int Id, string FullName, string? Subject, string Content, DateTime CreatedAt);

/// <param name="Message">Câu mô tả vấn đề, vd "3 sản phẩm chưa có ảnh".</param>
/// <param name="ActionUrl">Link tới danh sách ĐÃ LỌC SẴN đúng nhóm dữ liệu có vấn đề.</param>
public record DataWarning(string Message, string ActionText, string ActionUrl);
