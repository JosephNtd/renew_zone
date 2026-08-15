using System.ComponentModel.DataAnnotations;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Account;

namespace RenewZone.Web.Models.ViewModels;

/// <summary>
/// Biểu thức chính quy + thông báo dùng chung cho MỌI ô số điện thoại của web khách.
/// Gom về một chỗ vì SĐT xuất hiện ở 5 form (đăng ký, đăng nhập, hồ sơ, địa chỉ, đặt hàng) —
/// mỗi form tự chép một bản là kiểu gì cũng có bản lệch.
/// </summary>
public static class PhoneRules
{
    public const string Pattern = @"^(0|\+84)\d{9,10}$";
    public const string Message = "Số điện thoại không hợp lệ (VD: 0912345678).";
}

public class LoginInput
{
    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(PhoneRules.Pattern, ErrorMessage = PhoneRules.Message)]
    public string Phone { get; set; } = null!;

    [Display(Name = "Mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public class RegisterInput
{
    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Họ tên cần từ 2 đến 150 ký tự.")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(PhoneRules.Pattern, ErrorMessage = PhoneRules.Message)]
    public string Phone { get; set; } = null!;

    /// <summary>TÙY CHỌN — khách không có email vẫn phải mua hàng được (quyết định 2026-07-28).</summary>
    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Display(Name = "Mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu cần ít nhất 6 ký tự.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Display(Name = "Nhập lại mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Hai mật khẩu không khớp nhau.")]
    public string ConfirmPassword { get; set; } = null!;

    /// <summary>Honeypot chống bot — giống form liên hệ, ẩn bằng CSS chứ không phải type=hidden.</summary>
    public string? Website { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ProfileInput
{
    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Họ tên cần từ 2 đến 150 ký tự.")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(PhoneRules.Pattern, ErrorMessage = PhoneRules.Message)]
    public string Phone { get; set; } = null!;

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Display(Name = "Ngày sinh")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Giới tính")]
    public string? Gender { get; set; }
}

public class ChangePasswordInput
{
    [Display(Name = "Mật khẩu hiện tại")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = null!;

    [Display(Name = "Mật khẩu mới")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu cần ít nhất 6 ký tự.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = null!;

    [Display(Name = "Nhập lại mật khẩu mới")]
    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Hai mật khẩu không khớp nhau.")]
    public string ConfirmPassword { get; set; } = null!;
}

/// <summary>Form thêm/sửa địa chỉ. Ánh xạ sang <see cref="AddressInput"/> của Domain qua <see cref="ToInput"/>.</summary>
public class AddressFormInput
{
    public int Id { get; set; }

    [Display(Name = "Nhãn địa chỉ")]
    [StringLength(50)]
    public string? Label { get; set; }

    [Display(Name = "Tên người nhận")]
    [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Tên người nhận cần từ 2 đến 150 ký tự.")]
    public string ReceiverName { get; set; } = null!;

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(PhoneRules.Pattern, ErrorMessage = PhoneRules.Message)]
    public string Phone { get; set; } = null!;

    [Display(Name = "Địa chỉ")]
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Địa chỉ cần từ 5 đến 500 ký tự.")]
    public string AddressLine { get; set; } = null!;

    [Display(Name = "Đặt làm địa chỉ mặc định")]
    public bool IsDefault { get; set; }

    /// <summary>Đổi sang model của Domain — tầng dưới không biết gì về DataAnnotations của MVC.</summary>
    public AddressInput ToInput() => new()
    {
        Id = Id,
        Label = Label,
        ReceiverName = ReceiverName,
        Phone = Phone,
        AddressLine = AddressLine,
        IsDefault = IsDefault
    };
}

/// <summary>Model của trang tổng quan tài khoản.</summary>
public class AccountDashboardViewModel
{
    public string DisplayName { get; set; } = "";
    public AccountSummary Summary { get; set; } = null!;
}

/// <summary>Model trang "Đơn hàng của tôi" — giữ bộ lọc khi đổi trang.</summary>
public class MyOrdersViewModel
{
    public PagedResult<MyOrderListItem> Orders { get; set; } = null!;

    public string PageUrl(int page) => $"/tai-khoan/don-hang?trang={page}";
}
