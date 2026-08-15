using RenewZone.Domain.Enums;

namespace RenewZone.Web.Helpers;

/// <summary>
/// Nhãn + class CSS cho khu tài khoản KHÁCH HÀNG (Giai đoạn 9).
/// <para>
/// Tách khỏi <see cref="AdminDisplayExtensions"/> vì hai bên nói hai ngôn ngữ khác nhau: admin
/// cần từ ngữ nghiệp vụ ngắn gọn ("Mới"), khách cần biết ĐIỀU GÌ ĐANG XẢY RA với đơn của họ
/// ("Chờ xác nhận"). Class CSS cũng khác (<c>.acc-status</c> ở account.css vs <c>.adm-badge-*</c>
/// ở admin.css) — dùng chung sẽ kéo cả bảng màu quản trị sang web khách.
/// </para>
/// </summary>
public static class AccountDisplayExtensions
{
    /// <summary>Nhãn trạng thái viết theo góc nhìn của người mua.</summary>
    public static string ToCustomerLabel(this OrderStatus status) => status switch
    {
        OrderStatus.New => "Chờ xác nhận",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.Shipping => "Đang giao hàng",
        OrderStatus.Completed => "Giao thành công",
        OrderStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    /// <summary>Câu giải thích ngắn dưới nhãn — khách biết mình cần chờ hay cần làm gì.</summary>
    public static string ToCustomerHint(this OrderStatus status) => status switch
    {
        OrderStatus.New => "Shop sẽ gọi xác nhận đơn trong thời gian sớm nhất.",
        OrderStatus.Confirmed => "Đơn đã được xác nhận và đang chuẩn bị hàng.",
        OrderStatus.Shipping => "Đơn đang trên đường tới bạn.",
        OrderStatus.Completed => "Cảm ơn bạn! Bạn có thể đánh giá sản phẩm đã mua.",
        OrderStatus.Cancelled => "Đơn hàng này đã được hủy.",
        _ => ""
    };

    public static string ToCustomerStatusClass(this OrderStatus status) => status switch
    {
        OrderStatus.New => "is-new",
        OrderStatus.Confirmed => "is-confirmed",
        OrderStatus.Shipping => "is-shipping",
        OrderStatus.Completed => "is-completed",
        OrderStatus.Cancelled => "is-cancelled",
        _ => "is-new"
    };

    public static string ToCustomerIcon(this OrderStatus status) => status switch
    {
        OrderStatus.New => "bi-hourglass-split",
        OrderStatus.Confirmed => "bi-clipboard-check",
        OrderStatus.Shipping => "bi-truck",
        OrderStatus.Completed => "bi-check-circle-fill",
        OrderStatus.Cancelled => "bi-x-circle",
        _ => "bi-circle"
    };

    public static string ToCustomerLabel(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cod => "Thanh toán khi nhận hàng (COD)",
        PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
        _ => method.ToString()
    };
}
