using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Models;

/// <summary>
/// Thông tin người nhận khi đặt hàng, đã qua validate ở tầng Web.
/// Tách khỏi ViewModel (CheckoutInput) để Domain/Infrastructure không phụ thuộc ASP.NET MVC.
/// </summary>
public class OrderRequest
{
    /// <summary>
    /// Tài khoản đặt đơn. Từ Giai đoạn 9 luôn có giá trị (checkout yêu cầu đăng nhập); để
    /// nullable vì <c>Order.UserId</c> nullable — đơn cũ của khách vãng lai vẫn hợp lệ.
    /// Controller lấy từ claim, KHÔNG nhận từ form: cho client gửi lên là mở cửa cho việc
    /// gán đơn sang tài khoản người khác.
    /// </summary>
    public string? UserId { get; init; }

    public required string CustomerName { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public required string Address { get; init; }
    public string? Note { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
}
