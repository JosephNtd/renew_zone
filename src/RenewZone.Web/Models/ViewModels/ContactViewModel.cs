using System.ComponentModel.DataAnnotations;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Models;

namespace RenewZone.Web.Models.ViewModels;

/// <summary>
/// Dữ liệu khách nhập ở /lien-he. Mọi ErrorMessage đều tiếng Việt — để trống thì framework
/// chèn chuỗi "The X field is required." vào HTML (bẫy đã gặp ở Giai đoạn 4).
/// </summary>
public class ContactInput
{
    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    [StringLength(150, ErrorMessage = "Họ tên tối đa {1} ký tự")]
    public string FullName { get; set; } = "";

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (vd 0900000000)")]
    public string Phone { get; set; } = "";

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(150, ErrorMessage = "Email tối đa {1} ký tự")]
    public string? Email { get; set; }

    [Display(Name = "Địa chỉ")]
    [StringLength(300, ErrorMessage = "Địa chỉ tối đa {1} ký tự")]
    public string? Address { get; set; }

    [Display(Name = "Chủ đề")]
    [StringLength(150, ErrorMessage = "Chủ đề tối đa {1} ký tự")]
    public string? Subject { get; set; }

    [Display(Name = "Nội dung")]
    [Required(ErrorMessage = "Vui lòng nhập nội dung cần hỗ trợ")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Nội dung cần từ {2} đến {1} ký tự")]
    public string Content { get; set; } = "";

    /// <summary>
    /// Bẫy bot: input ẩn bằng CSS (KHÔNG dùng type=hidden để bot không nhận ra).
    /// Người thật không bao giờ điền ⇒ có giá trị là spam.
    /// </summary>
    public string? Website { get; set; }

    /// <summary>Đường dẫn file đã lưu — tầng Web tự gán sau khi validate, không nhận từ client.</summary>
    public ContactRequest ToRequest(string? attachmentPath, string? userId) => new()
    {
        UserId = userId,
        FullName = FullName.Trim(),
        Phone = Phone.Trim(),
        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
        Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
        Subject = string.IsNullOrWhiteSpace(Subject) ? null : Subject.Trim(),
        Content = Content.Trim(),
        AttachmentPath = attachmentPath
    };
}

/// <summary>Dữ liệu trang /lien-he: form + kênh liên hệ + trạng thái đã gửi.</summary>
public class ContactViewModel
{
    public required ContactInput Input { get; init; }

    /// <summary>Danh sách chủ đề tách từ setting ContactSubjects (ngăn bằng "|").</summary>
    public required IReadOnlyList<string> Subjects { get; init; }

    /// <summary>Kênh liên hệ nhanh (ContentBlock group "contact.channels"). Rỗng ⇒ ẩn cả hàng.</summary>
    public required IReadOnlyList<ContentBlock> Channels { get; init; }

    /// <summary>Vừa gửi thành công (đến từ PRG ?sent=1) ⇒ hiện khối cảm ơn thay form.</summary>
    public bool JustSent { get; init; }

    public required int MaxFileSizeMb { get; init; }
    public required string AcceptAttribute { get; init; }
}
