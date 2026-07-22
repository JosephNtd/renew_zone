using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="IDashboardService"/>.</summary>
public class DashboardService(AppDbContext db) : IDashboardService
{
    private const int WindowDays = 30;
    private const int RecentCount = 5;

    public async Task<DashboardSummary> GetSummaryAsync()
    {
        // Mốc theo NGÀY (00:00) chứ không phải "cách đây 30×24 giờ" — nếu không, cột đầu của
        // biểu đồ luôn là một ngày cụt và số liệu nhảy theo giờ admin mở trang
        var today = DateTime.Today;
        var from = today.AddDays(-(WindowDays - 1));
        var previousFrom = from.AddDays(-WindowDays);

        var statusCounts = await db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // Chỉ Completed mới tính là doanh thu — đơn đang giao có thể bị hủy/bom hàng
        var completedInWindow = await db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= from)
            .Select(o => new { o.CreatedAt, o.TotalAmount })
            .ToListAsync();

        var previousRevenue = await db.Orders
            .Where(o => o.Status == OrderStatus.Completed
                        && o.CreatedAt >= previousFrom && o.CreatedAt < from)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        // Gom theo ngày trong bộ nhớ rồi ĐỔ ĐỦ 30 ngày: ngày không có đơn vẫn phải có cột 0,
        // thiếu ngày sẽ làm trục hoành của biểu đồ co giãn sai
        var byDay = completedInWindow
            .GroupBy(o => o.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => (Amount: g.Sum(x => x.TotalAmount), Count: g.Count()));

        var revenueByDay = Enumerable.Range(0, WindowDays)
            .Select(i => from.AddDays(i))
            .Select(date => byDay.TryGetValue(date, out var v)
                ? new RevenuePoint(date, v.Amount, v.Count)
                : new RevenuePoint(date, 0m, 0))
            .ToList();

        var recentOrders = await db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(RecentCount)
            .Select(o => new RecentOrder(
                o.Id, o.Code, o.CustomerName, o.Phone, o.TotalAmount, o.Status, o.CreatedAt))
            .ToListAsync();

        var recentContacts = await db.ContactMessages
            .Where(c => !c.IsRead)
            .OrderByDescending(c => c.CreatedAt)
            .Take(RecentCount)
            .Select(c => new RecentContact(c.Id, c.FullName, c.Subject, c.Content, c.CreatedAt))
            .ToListAsync();

        return new DashboardSummary
        {
            NewOrderCount = statusCounts.GetValueOrDefault(OrderStatus.New),
            Revenue30Days = completedInWindow.Sum(o => o.TotalAmount),
            RevenuePrevious30Days = previousRevenue,
            ActiveProductCount = await db.Products.CountAsync(p => p.IsActive),
            UnreadContactCount = await db.ContactMessages.CountAsync(c => !c.IsRead),
            RevenueByDay = revenueByDay,
            OrderCountByStatus = statusCounts,
            RecentOrders = recentOrders,
            RecentContacts = recentContacts,
            Warnings = await BuildWarningsAsync()
        };
    }

    /// <summary>
    /// Khối "Cần chú ý": tìm dữ liệu thiếu khiến web khách trông dở dang.
    /// Mỗi cảnh báo kèm link tới danh sách ĐÃ LỌC SẴN — nói có vấn đề mà bắt admin tự đi tìm
    /// thì cảnh báo đó gần như vô dụng.
    /// </summary>
    private async Task<List<DataWarning>> BuildWarningsAsync()
    {
        var warnings = new List<DataWarning>();

        var noImage = await db.Products
            .CountAsync(p => p.IsActive && !p.Images.Any());
        if (noImage > 0)
        {
            warnings.Add(new DataWarning(
                $"{noImage} sản phẩm chưa có ảnh nào",
                "Xem danh sách", "/admin/san-pham?coAnh=khong"));
        }

        var noDescription = await db.Products
            .CountAsync(p => p.IsActive && (p.DescriptionHtml == null || p.DescriptionHtml == ""));
        if (noDescription > 0)
        {
            warnings.Add(new DataWarning(
                $"{noDescription} sản phẩm chưa có mô tả",
                "Xem danh sách", "/admin/san-pham?coMoTa=khong"));
        }

        var emptyCategories = await db.Categories
            .CountAsync(c => c.IsActive && !c.Products.Any() && !c.Children.Any());
        if (emptyCategories > 0)
        {
            warnings.Add(new DataWarning(
                $"{emptyCategories} danh mục đang trống (không có sản phẩm lẫn danh mục con)",
                "Xem danh mục", "/admin/danh-muc"));
        }

        return warnings;
    }
}
