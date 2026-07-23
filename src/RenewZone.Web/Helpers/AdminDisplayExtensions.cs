using RenewZone.Domain.Enums;

namespace RenewZone.Web.Helpers;

/// <summary>
/// Nhãn + class CSS cho các enum khi hiển thị trong admin. Là TRÌNH BÀY nên đặt ở tầng Web —
/// Domain không được biết gì về tên class CSS.
/// </summary>
public static class AdminDisplayExtensions
{
    public static string ToVietnamese(this OrderStatus status) => status switch
    {
        OrderStatus.New => "Mới",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.Shipping => "Đang giao",
        OrderStatus.Completed => "Hoàn thành",
        OrderStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    /// <summary>Class badge khớp cặp biến --adm-{status} / --adm-{status}-bg trong admin.css.</summary>
    public static string ToBadgeClass(this OrderStatus status) => status switch
    {
        OrderStatus.New => "adm-badge-new",
        OrderStatus.Confirmed => "adm-badge-confirm",
        OrderStatus.Shipping => "adm-badge-shipping",
        OrderStatus.Completed => "adm-badge-done",
        OrderStatus.Cancelled => "adm-badge-cancel",
        _ => "adm-badge-muted"
    };

    public static string ToIcon(this OrderStatus status) => status switch
    {
        OrderStatus.New => "bi-star",
        OrderStatus.Confirmed => "bi-check-lg",
        OrderStatus.Shipping => "bi-truck",
        OrderStatus.Completed => "bi-check-all",
        OrderStatus.Cancelled => "bi-x-lg",
        _ => "bi-dash"
    };

    /// <summary>Modifier tô ĐẶC chấm timeline dọc bằng token --adm-{status} (nhật ký xử lý đơn).</summary>
    public static string ToTimelineDotClass(this OrderStatus status) => status switch
    {
        OrderStatus.New => "is-new",
        OrderStatus.Confirmed => "is-confirm",
        OrderStatus.Shipping => "is-shipping",
        OrderStatus.Completed => "is-done",
        OrderStatus.Cancelled => "is-cancel",
        _ => "is-muted"
    };

    public static string ToVietnamese(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cod => "Thanh toán khi nhận hàng",
        PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
        _ => method.ToString()
    };

    public static string ToShortLabel(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cod => "COD",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        _ => method.ToString()
    };

    public static string ToIcon(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cod => "bi-cash-coin",
        PaymentMethod.BankTransfer => "bi-bank",
        _ => "bi-credit-card"
    };

    /// <summary>
    /// Thời gian tương đối kiểu "2 giờ trước" — trong bảng đơn hàng, khoảng cách thời gian
    /// dễ nắm bắt hơn nhiều so với một mốc ngày giờ đầy đủ.
    /// </summary>
    public static string ToRelativeTime(this DateTime value)
    {
        var span = DateTime.Now - value;

        return span switch
        {
            { TotalSeconds: < 60 } => "vừa xong",
            { TotalMinutes: < 60 } => $"{(int)span.TotalMinutes} phút trước",
            { TotalHours: < 24 } => $"{(int)span.TotalHours} giờ trước",
            { TotalDays: < 7 } => $"{(int)span.TotalDays} ngày trước",
            _ => value.ToString("dd/MM/yyyy")
        };
    }
}
