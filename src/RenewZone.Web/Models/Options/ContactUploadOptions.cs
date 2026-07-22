namespace RenewZone.Web.Models.Options;

/// <summary>
/// Cấu hình KỸ THUẬT cho file đính kèm form liên hệ — thuộc appsettings.json chứ không phải DB
/// (xem bảng phân loại nội dung ở CLAUDE.md mục 4.7).
/// </summary>
public class ContactUploadOptions
{
    public const string SectionName = "ContactUpload";

    private static readonly string[] DefaultExtensions = [".jpg", ".jpeg", ".png", ".webp", ".pdf"];

    public int MaxFileSizeMb { get; set; } = 5;

    /// <summary>
    /// ⚠️ Để RỖNG làm mặc định là có chủ đích: binder của IConfiguration **nối thêm** vào
    /// collection sẵn có chứ không thay thế, nên gán sẵn giá trị ở đây sẽ làm danh sách bị lặp
    /// khi appsettings.json cũng khai báo. Muốn giá trị mặc định thì dùng
    /// <see cref="EffectiveExtensions"/>.
    /// </summary>
    public string[] AllowedExtensions { get; set; } = [];

    /// <summary>Thư mục con trong wwwroot. File thật nằm ở &lt;FolderPath&gt;/yyyy-MM/.</summary>
    public string FolderPath { get; set; } = "uploads/contact";

    public long MaxFileSizeBytes => MaxFileSizeMb * 1024L * 1024L;

    /// <summary>Danh sách đuôi file thực dùng — thiếu cấu hình thì rơi về mặc định an toàn.</summary>
    public IReadOnlyList<string> EffectiveExtensions =>
        AllowedExtensions.Length > 0 ? AllowedExtensions : DefaultExtensions;
}
