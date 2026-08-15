using System.ComponentModel.DataAnnotations;
using RenewZone.Domain.Entities;
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

    /// <summary>
    /// Id địa chỉ trong sổ mà khách vừa bấm chọn. Chỉ dùng để tô sáng thẻ địa chỉ khi form được
    /// render lại — 3 ô họ tên/SĐT/địa chỉ vẫn là nguồn sự thật của đơn, nên khách sửa tay sau
    /// khi chọn thì đơn đi theo cái họ sửa.
    /// </summary>
    public int? SelectedAddressId { get; set; }

    /// <param name="userId">
    /// Lấy từ claim của người đang đăng nhập, KHÔNG phải từ form — để form gửi lên thì ai cũng
    /// gán được đơn của mình sang tài khoản người khác.
    /// </param>
    public OrderRequest ToRequest(string? userId) => new()
    {
        UserId = userId,
        CustomerName = CustomerName,
        Phone = Phone,
        Email = Email,
        Address = Address,
        Note = Note,
        PaymentMethod = PaymentMethod
    };
}

/// <summary>Dữ liệu trang /dat-hang: form + tóm tắt giỏ (chỉ đọc) + sổ địa chỉ để chọn nhanh.</summary>
public class CheckoutViewModel
{
    public required CheckoutInput Input { get; init; }
    public required Cart Cart { get; init; }

    /// <summary>Sổ địa chỉ của khách — rỗng thì view ẩn hẳn khối chọn nhanh (luật fallback mục 4.7).</summary>
    public IReadOnlyList<CustomerAddress> Addresses { get; init; } = [];
}

// Ghi chú Giai đoạn 5: class DemoBankInfo (hardcode tên ngân hàng/STK trong C#) đã bị XÓA.
// Thông tin chuyển khoản giờ đọc từ SiteSettings qua partial Views/Shared/_BankInfo.cshtml
// để admin sửa được ở Giai đoạn 6 — xem CLAUDE.md mục 4.7.
