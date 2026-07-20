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
}
