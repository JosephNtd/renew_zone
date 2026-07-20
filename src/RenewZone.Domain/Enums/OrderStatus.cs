namespace RenewZone.Domain.Enums;

/// <summary>Vòng đời đơn hàng — admin chuyển trạng thái ở Giai đoạn 6.</summary>
public enum OrderStatus
{
    New = 0,        // Mới đặt, chờ xác nhận
    Confirmed = 1,  // Đã gọi xác nhận với khách
    Shipping = 2,   // Đang giao
    Completed = 3,  // Giao thành công
    Cancelled = 4   // Hủy (khách bom hàng / hết hàng...)
}
