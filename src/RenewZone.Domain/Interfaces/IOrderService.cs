using RenewZone.Domain.Entities;
using RenewZone.Domain.Models;

namespace RenewZone.Domain.Interfaces;

/// <summary>Ghi đơn hàng xuống DB (Giai đoạn 4).</summary>
public interface IOrderService
{
    /// <summary>
    /// Tạo <see cref="Order"/> + các <see cref="OrderItem"/> snapshot từ giỏ hàng.
    /// Tự sinh mã đơn unique. Ném <see cref="InvalidOperationException"/> nếu giỏ rỗng.
    /// </summary>
    Task<Order> CreateOrderAsync(OrderRequest request, Cart cart);
}
