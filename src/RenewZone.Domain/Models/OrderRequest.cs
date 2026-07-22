using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Models;

/// <summary>
/// Thông tin người nhận khi đặt hàng, đã qua validate ở tầng Web.
/// Tách khỏi ViewModel (CheckoutInput) để Domain/Infrastructure không phụ thuộc ASP.NET MVC.
/// </summary>
public class OrderRequest
{
    public required string CustomerName { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public required string Address { get; init; }
    public string? Note { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
}
