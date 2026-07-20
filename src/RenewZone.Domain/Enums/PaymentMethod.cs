namespace RenewZone.Domain.Enums;

/// <summary>Phương thức thanh toán theo Chính Sách Thanh Toán của site gốc.</summary>
public enum PaymentMethod
{
    Cod = 0,          // Thanh toán khi nhận hàng
    BankTransfer = 1  // Chuyển khoản
}
