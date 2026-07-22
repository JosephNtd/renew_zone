namespace RenewZone.Web.Helpers;

/// <summary>
/// Đọc SiteSetting trong view kèm fallback — luật bắt buộc ở CLAUDE.md mục 4.7:
/// thiếu dữ liệu trong DB KHÔNG BAO GIỜ được làm vỡ layout hay lọt chuỗi rỗng ra HTML.
/// </summary>
public static class SettingExtensions
{
    public static string Get(this IReadOnlyDictionary<string, string?> settings, string key, string fallback = "")
    {
        var value = settings.GetValueOrDefault(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
