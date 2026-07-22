using RenewZone.Domain.Enums;

namespace RenewZone.Web.Models.ViewModels;

/// <summary>
/// Dữ liệu trang cảm ơn. Truyền qua TempData (JSON) sau redirect PRG — không đọc lại DB,
/// cũng không để mã đơn trên URL.
/// </summary>
public class OrderCompleteViewModel
{
    public string Code { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string TotalText { get; set; } = "";
    public PaymentMethod PaymentMethod { get; set; }

    public string PaymentMethodLabel => PaymentMethod == PaymentMethod.BankTransfer
        ? "Chuyển khoản ngân hàng"
        : "Thanh toán khi nhận hàng (COD)";
}
