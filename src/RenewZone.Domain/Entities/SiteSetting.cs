namespace RenewZone.Domain.Entities;

/// <summary>
/// Cấu hình toàn site dạng key-value: hotline, địa chỉ, giờ mở cửa, link social, MST...
/// Layout đọc các giá trị này thay vì hardcode — admin sửa được không cần deploy.
/// Key là khóa chính (không dùng BaseEntity vì không cần Id int).
/// </summary>
public class SiteSetting
{
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
}

/// <summary>Danh sách key chuẩn — dùng hằng số để tránh gõ sai chuỗi.</summary>
public static class SettingKeys
{
    public const string StoreName = "StoreName";
    public const string Tagline = "Tagline";
    public const string Hotline = "Hotline";
    public const string Address = "Address";
    public const string OpenHours = "OpenHours";
    public const string Email = "Email";
    public const string TaxCode = "TaxCode";
    public const string FacebookUrl = "FacebookUrl";
    public const string MessengerUrl = "MessengerUrl";
    public const string ZaloUrl = "ZaloUrl";
    public const string MapUrl = "MapUrl";
    public const string MapEmbedUrl = "MapEmbedUrl";

    // ----- Thanh toán chuyển khoản (GIẢ LẬP — Lưu ý 6.2) -----
    public const string BankName = "BankName";
    public const string BankAccountNumber = "BankAccountNumber";
    public const string BankAccountName = "BankAccountName";

    /// <summary>Mẫu nội dung chuyển khoản, <c>{0}</c> = mã đơn hàng.</summary>
    public const string BankTransferNoteTemplate = "BankTransferNoteTemplate";

    // ----- Nội dung rời rạc trên các trang -----
    public const string ShippingNote = "ShippingNote";

    /// <summary>Danh sách chủ đề form liên hệ, ngăn cách bằng ký tự <c>|</c>.</summary>
    public const string ContactSubjects = "ContactSubjects";

    public const string ContactIntro = "ContactIntro";
    public const string NewsPageTitle = "NewsPageTitle";
    public const string NewsPageIntro = "NewsPageIntro";
    public const string FooterAbout = "FooterAbout";
    public const string FooterDisclaimer = "FooterDisclaimer";
}
