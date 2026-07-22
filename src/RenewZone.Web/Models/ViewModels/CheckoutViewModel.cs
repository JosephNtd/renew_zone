using System.ComponentModel.DataAnnotations;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Models;

namespace RenewZone.Web.Models.ViewModels;

/// <summary>
/// Thông tin khách nhập ở trang /dat-hang. Validate bằng DataAnnotations (server) +
/// jquery-validation-unobtrusive (client). Map sang <see cref="OrderRequest"/> trước khi
/// đưa xuống tầng dưới — Domain không biết gì về MVC.
/// </summary>
public class CheckoutInput
{
    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
    [StringLength(150, ErrorMessage = "Họ tên tối đa {1} ký tự")]
    public string CustomerName { get; set; } = "";

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (vd 0900000000)")]
    public string Phone { get; set; } = "";

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(150, ErrorMessage = "Email tối đa {1} ký tự")]
    public string? Email { get; set; }

    [Display(Name = "Địa chỉ nhận hàng")]
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
    [StringLength(500, ErrorMessage = "Địa chỉ tối đa {1} ký tự")]
    public string Address { get; set; } = "";

    [Display(Name = "Ghi chú")]
    [StringLength(1000, ErrorMessage = "Ghi chú tối đa {1} ký tự")]
    public string? Note { get; set; }

    [Display(Name = "Phương thức thanh toán")]
    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cod;

    public OrderRequest ToRequest() => new()
    {
        CustomerName = CustomerName,
        Phone = Phone,
        Email = Email,
        Address = Address,
        Note = Note,
        PaymentMethod = PaymentMethod
    };
}

/// <summary>Dữ liệu trang /dat-hang: form + tóm tắt giỏ (chỉ đọc).</summary>
public class CheckoutViewModel
{
    public required CheckoutInput Input { get; init; }
    public required Cart Cart { get; init; }
}

// Ghi chú Giai đoạn 5: class DemoBankInfo (hardcode tên ngân hàng/STK trong C#) đã bị XÓA.
// Thông tin chuyển khoản giờ đọc từ SiteSettings qua partial Views/Shared/_BankInfo.cshtml
// để admin sửa được ở Giai đoạn 6 — xem CLAUDE.md mục 4.7.
