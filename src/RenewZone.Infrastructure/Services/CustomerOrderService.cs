using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Account;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <inheritdoc cref="ICustomerOrderService" />
/// <remarks>
/// KHÔNG cache: đây là dữ liệu riêng từng người và đổi ngay khi admin xử lý đơn — hiện dữ liệu
/// cũ 5 phút ở màn "Đơn hàng của tôi" là khách gọi điện hỏi tại sao đơn chưa cập nhật.
/// </remarks>
public class CustomerOrderService(AppDbContext db, ILogger<CustomerOrderService> logger)
    : ICustomerOrderService
{
    /// <summary>Nhãn người thao tác hiện cho khách — không bao giờ lộ tên đăng nhập của admin.</summary>
    private const string ShopActorLabel = "Cửa hàng";
    private const string CustomerActorLabel = "Bạn";

    /// <summary>Ghi vào <c>OrderStatusHistory.ChangedBy</c> khi CHÍNH khách bấm hủy.</summary>
    private const string CustomerActor = "Khách hàng";

    public async Task<PagedResult<MyOrderListItem>> GetMyOrdersAsync(string userId, int page, int pageSize)
    {
        var query = db.Orders.AsNoTracking().Where(o => o.UserId == userId);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new MyOrderListItem(
                o.Id,
                o.Code,
                o.Status,
                o.PaymentMethod,
                o.TotalAmount,
                o.Items.Count,
                o.Items.Select(i => i.ProductName).FirstOrDefault(),

                // Ảnh chính của sản phẩm đầu tiên. Sản phẩm có thể đã bị xóa (ProductId null)
                // ⇒ để null và view rơi về placeholder, KHÔNG được làm vỡ cả trang lịch sử đơn.
                o.Items
                    .Where(i => i.ProductId != null)
                    .SelectMany(i => i.Product!.Images.Where(img => img.IsMain).Select(img => img.Url))
                    .FirstOrDefault(),
                o.CreatedAt))
            .ToListAsync();

        return new PagedResult<MyOrderListItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<MyOrderDetail?> GetMyOrderAsync(string userId, string code)
    {
        // Điều kiện UserId nằm NGAY trong câu truy vấn (không phải kiểm sau khi lấy về):
        // đơn của người khác không bao giờ rời khỏi database.
        var order = await db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId && o.Code == code)
            .Select(o => new
            {
                o.Id, o.Code, o.CustomerName, o.Phone, o.Email, o.Address, o.Note,
                o.PaymentMethod, o.Status, o.TotalAmount, o.CreatedAt,
                Items = o.Items.Select(i => new OrderDetailItem(
                    i.Id,
                    i.ProductId,
                    i.ProductName,
                    i.Product != null ? i.Product.Slug : null,
                    i.Product != null
                        ? i.Product.Images.Where(img => img.IsMain).Select(img => img.Url).FirstOrDefault()
                        : null,
                    i.VariantDescription,
                    i.UnitPrice,
                    i.Quantity)).ToList(),
                History = o.StatusHistory
                    .OrderBy(h => h.ChangedAt)
                    .Select(h => new { h.FromStatus, h.ToStatus, h.ChangedBy, h.Note, h.ChangedAt })
                    .ToList()
            })
            // Hai collection lồng trong cùng một projection (dòng hàng + nhật ký trạng thái) ⇒
            // một câu SQL duy nhất sẽ nhân chéo hai bảng với nhau. Tách câu như PDP đã làm từ
            // Giai đoạn 3 (CLAUDE.md mục 4.3), đồng thời hết cảnh báo EF 20504.
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (order is null)
        {
            return null;
        }

        return new MyOrderDetail(
            order.Id, order.Code, order.CustomerName, order.Phone, order.Email, order.Address,
            order.Note, order.PaymentMethod, order.Status, order.TotalAmount, order.CreatedAt,
            order.Items,
            order.History
                .Select(h => new MyOrderHistoryEntry(
                    h.FromStatus, h.ToStatus, MaskActor(h.ChangedBy), h.Note, h.ChangedAt))
                .ToList());
    }

    /// <summary>
    /// Quy tên người thao tác về nhãn an toàn. Sổ nhật ký lưu tên đăng nhập quản trị viên
    /// (vd "admin") — hiển thị nguyên si cho khách là rò rỉ tài khoản nội bộ mà chẳng để làm gì.
    /// </summary>
    private static string MaskActor(string changedBy) =>
        changedBy is CustomerActor or "Khách đặt hàng" ? CustomerActorLabel : ShopActorLabel;

    public async Task<AccountOpResult> CancelMyOrderAsync(string userId, string code, string? reason)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.UserId == userId && o.Code == code);

        if (order is null)
        {
            // Cùng một câu cho "không tồn tại" và "không phải đơn của bạn" — nói khác nhau là
            // tự xác nhận cho người đang dò rằng mã đơn đó có thật.
            return AccountOpResult.Fail("Không tìm thấy đơn hàng này trong tài khoản của bạn.");
        }

        if (!OrderStatusRules.CustomerCanCancel(order.Status))
        {
            return AccountOpResult.Fail(order.Status == OrderStatus.Cancelled
                ? "Đơn hàng này đã được hủy trước đó."
                : "Đơn hàng đã được xử lý nên không thể tự hủy. Vui lòng gọi hotline để được hỗ trợ.");
        }

        var previous = order.Status;
        order.Status = OrderStatus.Cancelled;

        // Cùng SaveChanges với việc đổi Status ⇒ nguyên tử, không thể hủy mà quên ghi sổ (Giai đoạn 7).
        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = previous,
            ToStatus = OrderStatus.Cancelled,
            ChangedBy = CustomerActor,
            Note = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            ChangedAt = DateTime.Now
        });

        await db.SaveChangesAsync();

        logger.LogInformation("Khách {UserId} tự hủy đơn {Code} (đang ở {From})", userId, code, previous);
        return AccountOpResult.Success($"Đã hủy đơn hàng {order.Code}.");
    }

    public async Task<AccountSummary> GetSummaryAsync(string userId)
    {
        var orders = db.Orders.AsNoTracking().Where(o => o.UserId == userId);

        // Gộp 4 con số vào MỘT truy vấn nhóm thay vì 4 lần round-trip
        var stats = await orders
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled),
                Completed = g.Count(o => o.Status == OrderStatus.Completed),

                // Chỉ tính đơn ĐÃ hoàn thành: đơn đang xử lý hay đã hủy chưa phải tiền đã tiêu.
                Spent = g.Where(o => o.Status == OrderStatus.Completed).Sum(o => (decimal?)o.TotalAmount) ?? 0m
            })
            .FirstOrDefaultAsync();

        var recent = await GetMyOrdersAsync(userId, 1, 5);

        return new AccountSummary(
            stats?.Total ?? 0,
            stats?.Active ?? 0,
            stats?.Completed ?? 0,
            stats?.Spent ?? 0m,
            await db.CustomerAddresses.CountAsync(a => a.UserId == userId),
            await db.WishlistItems.CountAsync(w => w.UserId == userId),
            recent.Items);
    }

    public Task<bool> HasPurchasedAsync(string userId, int productId) =>
        db.OrderItems
            .AsNoTracking()
            .AnyAsync(i => i.ProductId == productId
                           && i.Order.UserId == userId
                           && i.Order.Status == OrderStatus.Completed);

    public async Task<IReadOnlyList<ReorderLine>> GetReorderLinesAsync(string userId, string code)
    {
        var items = await db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId && o.Code == code)
            .SelectMany(o => o.Items)
            .Where(i => i.ProductId != null) // sản phẩm đã bị gỡ khỏi shop ⇒ không mua lại được
            .Select(i => new { ProductId = i.ProductId!.Value, i.VariantDescription, i.Quantity })
            .ToListAsync();

        if (items.Count == 0)
        {
            return [];
        }

        // OrderItem chỉ snapshot MÔ TẢ biến thể dạng chữ ("512GB · Titan Đen"), không lưu id option
        // (cố ý từ Giai đoạn 1: sổ sách phải đọc được kể cả khi biến thể bị xóa). Để dựng lại giỏ
        // phải dò ngược từ chữ về id đang có; giá trị nào không còn thì bỏ qua và CartService sẽ
        // tự lấy giá trị mặc định của nhóm — thà mua lại ra cấu hình gần đúng còn hơn không mua được.
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var optionValues = await db.ProductOptionValues.AsNoTracking()
            .Where(v => productIds.Contains(v.Group.ProductId))
            .Select(v => new { v.Id, v.Value, ProductId = v.Group.ProductId })
            .ToListAsync();

        var lines = new List<ReorderLine>();
        foreach (var item in items)
        {
            var chosen = new List<int>();

            if (!string.IsNullOrWhiteSpace(item.VariantDescription))
            {
                // CartService nối bằng " · ", nhưng chấp nhận cả dấu phẩy cho dữ liệu đơn cũ
                var parts = item.VariantDescription.Split(
                    ['·', ','],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var part in parts)
                {
                    var match = optionValues.FirstOrDefault(v =>
                        v.ProductId == item.ProductId
                        && string.Equals(v.Value, part, StringComparison.OrdinalIgnoreCase));

                    if (match is not null)
                    {
                        chosen.Add(match.Id);
                    }
                }
            }

            lines.Add(new ReorderLine(item.ProductId, chosen, item.Quantity));
        }

        return lines;
    }
}
