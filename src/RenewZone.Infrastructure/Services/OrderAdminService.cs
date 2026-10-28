using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="IOrderAdminService"/>.</summary>
public class OrderAdminService(AppDbContext db, ILogger<OrderAdminService> logger) : IOrderAdminService
{
    public async Task<PagedResult<OrderListItem>> GetListAsync(OrderListRequest request)
    {
        var query = db.Orders.AsNoTracking().AsQueryable();

        if (request.Status is { } status)
        {
            query = query.Where(o => o.Status == status);
        }

        if (request.PaymentMethod is { } payment)
        {
            query = query.Where(o => o.PaymentMethod == payment);
        }

        if (request.FromDate is { } from)
        {
            query = query.Where(o => o.CreatedAt >= from.Date);
        }

        if (request.ToDate is { } to)
        {
            // Người dùng chọn "đến 22/07" nghĩa là GỒM CẢ ngày 22 — so sánh với 00:00 hôm sau,
            // nếu không thì đơn đặt lúc 14h ngày 22 sẽ bị lọt ra ngoài một cách khó hiểu
            query = query.Where(o => o.CreatedAt < to.Date.AddDays(1));
        }

        if (request.NormalizedSearch is { } search)
        {
            query = query.Where(o =>
                o.Code.Contains(search) ||
                o.CustomerName.Contains(search) ||
                o.Phone.Contains(search));
        }

        var totalItems = await query.CountAsync();

        query = (request.SortBy, request.SortDesc) switch
        {
            ("tong", false) => query.OrderBy(o => o.TotalAmount),
            ("tong", true) => query.OrderByDescending(o => o.TotalAmount),
            ("khach", false) => query.OrderBy(o => o.CustomerName),
            ("khach", true) => query.OrderByDescending(o => o.CustomerName),
            ("ngay", false) => query.OrderBy(o => o.CreatedAt),
            _ => query.OrderByDescending(o => o.CreatedAt) // mặc định: mới nhất lên đầu
        };

        var items = await query
            .Skip(request.Skip)
            .Take(request.SafePageSize)
            .Select(o => new OrderListItem(
                o.Id, o.Code, o.CustomerName, o.Phone,
                o.Items.Count, o.TotalAmount, o.PaymentMethod, o.Status, o.CreatedAt))
            .ToListAsync();

        return new PagedResult<OrderListItem>
        {
            Items = items,
            Page = request.SafePage,
            PageSize = request.SafePageSize,
            TotalItems = totalItems
        };
    }

    public async Task<OrderDetail?> GetDetailAsync(int id)
    {
        var order = await db.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new
            {
                o.Id, o.Code, o.CustomerName, o.Phone, o.Email, o.Address, o.Note,
                o.PaymentMethod, o.Status, o.TotalAmount, o.CreatedAt, o.UpdatedAt,
                Items = o.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.ProductName,
                    // Sản phẩm đã xóa (kể cả xóa mềm) ⇒ Product null, dòng hàng vẫn sống bằng snapshot
                    ProductSlug = i.Product != null ? i.Product.Slug : null,
                    ImageUrl = i.Product != null
                        ? i.Product.Images.Where(img => img.IsMain).Select(img => img.Url).FirstOrDefault()
                        : null,
                    i.VariantDescription,
                    i.UnitPrice,
                    i.Quantity
                }).ToList(),
                Delivery = o.DeliveryReview == null
                    ? null
                    : new { o.DeliveryReview.Rating, o.DeliveryReview.Comment, o.DeliveryReview.CreatedAt },
                // Nhật ký trạng thái, cũ → mới để view vẽ timeline theo dòng thời gian tự nhiên
                History = o.StatusHistory
                    .OrderBy(h => h.ChangedAt)
                    .Select(h => new
                    {
                        h.FromStatus, h.ToStatus, h.ChangedBy, h.Note, h.ChangedAt
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order is null)
        {
            return null;
        }

        return new OrderDetail(
            order.Id, order.Code, order.CustomerName, order.Phone, order.Email,
            order.Address, order.Note, order.PaymentMethod, order.Status,
            order.TotalAmount, order.CreatedAt, order.UpdatedAt,
            [.. order.Items.Select(i => new OrderDetailItem(
                i.Id, i.ProductId, i.ProductName, i.ProductSlug, i.ImageUrl,
                i.VariantDescription, i.UnitPrice, i.Quantity))],
            [.. order.History.Select(h => new OrderStatusHistoryEntry(
                h.FromStatus, h.ToStatus, h.ChangedBy, h.Note, h.ChangedAt))],
            order.Delivery is null
                ? null
                : new DeliveryReviewInfo(order.Delivery.Rating, order.Delivery.Comment, order.Delivery.CreatedAt));
    }

    public async Task<IReadOnlyDictionary<OrderStatus, int>> GetStatusCountsAsync() =>
        await db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

    public async Task<ChangeStatusResult> ChangeStatusAsync(int id, OrderStatus newStatus, string actor, string? note = null)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return new ChangeStatusResult(false, "Không tìm thấy đơn hàng.", OrderStatus.New, []);
        }

        if (order.Status == newStatus)
        {
            return new ChangeStatusResult(true, "Đơn hàng đã ở trạng thái này rồi.",
                order.Status, OrderStatusRules.AllowedNext(order.Status));
        }

        // Chốt chặn NGHIỆP VỤ: từ chối bước nhảy sai kể cả khi bị gọi thẳng bằng curl
        if (!OrderStatusRules.CanTransition(order.Status, newStatus))
        {
            logger.LogWarning("Từ chối đổi trạng thái đơn {Code}: {From} → {To} không hợp lệ",
                order.Code, order.Status, newStatus);

            return new ChangeStatusResult(false,
                $"Không thể chuyển đơn từ “{Label(order.Status)}” sang “{Label(newStatus)}”.",
                order.Status, OrderStatusRules.AllowedNext(order.Status));
        }

        var previous = order.Status;
        order.Status = newStatus;

        // Ghi sổ nhật ký trong CÙNG SaveChanges với việc đổi Order.Status ⇒ nguyên tử: không thể
        // có chuyện đổi trạng thái mà quên ghi sổ (nếu tách 2 lần save, lỗi giữa chừng sẽ lệch sổ).
        // ChangedAt set tay vì OrderStatusHistory không phải AuditableEntity.
        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = previous,
            ToStatus = newStatus,
            ChangedBy = actor,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            ChangedAt = DateTime.Now
        });

        // UpdatedAt do AppDbContext tự gán.
        await db.SaveChangesAsync();

        logger.LogInformation("Đơn {Code} đổi trạng thái {From} → {To} bởi {Actor}",
            order.Code, previous, newStatus, actor);

        return new ChangeStatusResult(true,
            $"Đã chuyển đơn {order.Code} sang “{Label(newStatus)}”.",
            newStatus, OrderStatusRules.AllowedNext(newStatus), order.UserId);
    }

    /// <summary>Nhãn tiếng Việt cho THÔNG BÁO NGHIỆP VỤ (khác với nhãn hiển thị ở tầng Web).</summary>
    private static string Label(OrderStatus status) => status switch
    {
        OrderStatus.New => "Mới",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.Shipping => "Đang giao",
        OrderStatus.Completed => "Hoàn thành",
        OrderStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };
}
