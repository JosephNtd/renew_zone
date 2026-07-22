using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <inheritdoc cref="IOrderService" />
public class OrderService(AppDbContext db) : IOrderService
{
    /// <summary>Số lần thử sinh lại mã đơn khi đụng unique index Orders.Code.</summary>
    private const int MaxCodeAttempts = 5;

    public async Task<Order> CreateOrderAsync(OrderRequest request, Cart cart)
    {
        if (cart.IsEmpty)
        {
            throw new InvalidOperationException("Không thể tạo đơn từ giỏ hàng rỗng.");
        }

        // Sản phẩm có thể đã bị admin xóa sau khi khách bỏ vào giỏ ⇒ để ProductId null
        // (OrderItem.ProductId là SetNull) thay vì để SaveChanges vỡ vì lỗi khóa ngoại.
        var cartProductIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var aliveProductIds = await db.Products
            .Where(p => cartProductIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        var order = new Order
        {
            CustomerName = request.CustomerName.Trim(),
            Phone = request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Address = request.Address.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            PaymentMethod = request.PaymentMethod,
            Status = OrderStatus.New,
            TotalAmount = cart.Subtotal
        };

        foreach (var item in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = aliveProductIds.Contains(item.ProductId) ? item.ProductId : null,
                ProductName = item.ProductName,
                VariantDescription = item.VariantDescription,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            });
        }

        db.Orders.Add(order);

        // CreatedAt do AppDbContext.SaveChanges tự gán (AuditableEntity) — không set tay ở đây.
        for (var attempt = 1; ; attempt++)
        {
            order.Code = GenerateCode();
            try
            {
                await db.SaveChangesAsync();
                return order;
            }
            catch (DbUpdateException) when (attempt < MaxCodeAttempts)
            {
                // Nhiều khả năng là trùng Orders.Code (unique index) — sinh mã khác rồi thử lại.
                // Các entity vẫn ở trạng thái Added trong change tracker nên lần lưu sau vẫn đủ dữ liệu.
            }
        }
    }

    /// <summary>Mã đơn dạng RZ + yyMMdd + 4 số ngẫu nhiên, vd "RZ2607224821" (12 ký tự).</summary>
    private static string GenerateCode() =>
        $"RZ{DateTime.Now:yyMMdd}{Random.Shared.Next(1000, 10000)}";
}
