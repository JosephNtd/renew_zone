namespace RenewZone.Web.Models.Options;

/// <summary>
/// Cấu hình KỸ THUẬT cho upload file — thuộc appsettings.json chứ không phải DB
/// (xem bảng phân loại nội dung ở CLAUDE.md mục 4.7).
/// Mỗi "hồ sơ" (profile) là một chỗ upload có giới hạn riêng: ảnh sản phẩm không cùng
/// ràng buộc với file đính kèm form liên hệ.
/// </summary>
public class UploadOptions
{
    public const string SectionName = "Uploads";

    public UploadProfileOptions Contact { get; set; } = new() { FolderPath = "uploads/contact" };
    public UploadProfileOptions Product { get; set; } = new() { FolderPath = "uploads/products" };
    public UploadProfileOptions News { get; set; } = new() { FolderPath = "uploads/news" };
    public UploadProfileOptions Banner { get; set; } = new() { FolderPath = "uploads/banners" };
    public UploadProfileOptions Editor { get; set; } = new() { FolderPath = "uploads/editor" };
}

/// <summary>Giới hạn của một chỗ upload cụ thể.</summary>
public class UploadProfileOptions
{
    private static readonly string[] DefaultExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public int MaxFileSizeMb { get; set; } = 4;

    /// <summary>
    /// Chiều rộng tối đa (px) khi resize ảnh lúc upload (Giai đoạn 7). Ảnh RỘNG HƠN mức này bị
    /// thu về đúng mức (giữ tỉ lệ); ảnh nhỏ hơn KHÔNG bị phóng to. Mặc định 1600 (số hợp lý —
    /// scalar nên binder THAY THẾ chứ không nối, khác mảng ở AllowedExtensions). Chỉ áp cho ảnh;
    /// PDF bỏ qua.
    /// </summary>
    public int MaxWidth { get; set; } = 1600;

    /// <summary>
    /// ⚠️ Mặc định RỖNG là có chủ đích: binder của IConfiguration **nối thêm** vào collection
    /// sẵn có chứ không thay thế, nên gán sẵn giá trị ở đây sẽ làm danh sách bị lặp đôi khi
    /// appsettings.json cũng khai báo (đã dính một lần ở Giai đoạn 5 — CLAUDE.md Lưu ý 6.14).
    /// Muốn giá trị mặc định thì dùng <see cref="EffectiveExtensions"/>.
    /// </summary>
    public string[] AllowedExtensions { get; set; } = [];

    /// <summary>Thư mục con trong wwwroot. File thật nằm ở &lt;FolderPath&gt;/yyyy-MM/.</summary>
    public string FolderPath { get; set; } = "uploads/misc";

    public long MaxFileSizeBytes => MaxFileSizeMb * 1024L * 1024L;

    /// <summary>Danh sách đuôi file thực dùng — thiếu cấu hình thì rơi về mặc định an toàn.</summary>
    public IReadOnlyList<string> EffectiveExtensions =>
        AllowedExtensions.Length > 0 ? AllowedExtensions : DefaultExtensions;

    /// <summary>Chuỗi cho thuộc tính <c>accept</c> của &lt;input type="file"&gt;.</summary>
    public string AcceptAttribute => string.Join(",", EffectiveExtensions);
}
