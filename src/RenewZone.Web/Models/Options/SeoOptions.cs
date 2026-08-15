namespace RenewZone.Web.Models.Options;

/// <summary>
/// Cấu hình KỸ THUẬT về khả năng hiển thị của site với công cụ tìm kiếm — thuộc appsettings.json
/// chứ không phải DB (bảng phân loại nội dung ở CLAUDE.md mục 4.7: đây không phải thứ chủ shop
/// chỉnh hằng ngày mà là thuộc tính của MÔI TRƯỜNG đang chạy).
/// </summary>
public class SeoOptions
{
    public const string SectionName = "Seo";

    /// <summary>
    /// Cho phép Google/Bing index site hay không. Mặc định <c>true</c> (bản thật).
    /// Đặt <c>false</c> ở môi trường demo/staging ⇒ <c>robots.txt</c> trả <c>Disallow: /</c>
    /// VÀ mọi trang thêm <c>noindex, nofollow</c> — cần cả hai vì robots.txt chỉ ngăn thu thập,
    /// trang được link từ nơi khác vẫn có thể vào index nếu thiếu thẻ meta.
    /// ⚠️ Dự án này là bản clone học tập (CLAUDE.md Lưu ý 6.2): bản deploy công khai PHẢI để
    /// <c>false</c> để không cạnh tranh kết quả tìm kiếm với website gốc.
    /// </summary>
    public bool AllowIndexing { get; set; } = true;
}
