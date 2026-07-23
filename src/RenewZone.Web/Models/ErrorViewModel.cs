namespace RenewZone.Web.Models;

/// <summary>Dữ liệu cho trang lỗi tùy chỉnh (404/500). Mặc định tự chủ, không phụ thuộc DB.</summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>Mã HTTP gốc (404/500...) — hiển thị + để trang set đúng status code.</summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Hotline hiển thị ở trang 500. Đọc thử từ SiteSettings trong try/catch — DB chết thì
    /// dùng hằng fallback. Trang 500 KHÔNG được tự đọc DB (có thể chính DB đang lỗi).
    /// </summary>
    public string Hotline { get; set; } = "0900.000.000";
}
