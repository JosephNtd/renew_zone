using Microsoft.Extensions.Options;
using RenewZone.Web.Models.Options;

namespace RenewZone.Web.Services;

/// <summary>
/// Xem <see cref="IMediaService"/>. Toàn bộ quy tắc bảo mật upload của dự án tập trung ở đây.
/// </summary>
public class MediaService(IWebHostEnvironment env, IOptions<UploadOptions> options, ILogger<MediaService> logger)
    : IMediaService
{
    private readonly UploadOptions _options = options.Value;

    /// <summary>Thư mục gốc chứa mọi file người dùng tải lên — <see cref="Delete"/> không đụng ra ngoài đây.</summary>
    private const string UploadRoot = "uploads";

    public UploadProfileOptions GetOptions(UploadProfile profile) => profile switch
    {
        UploadProfile.Contact => _options.Contact,
        UploadProfile.Product => _options.Product,
        UploadProfile.News => _options.News,
        UploadProfile.Banner => _options.Banner,
        UploadProfile.Editor => _options.Editor,
        _ => _options.Product
    };

    public async Task<MediaResult> SaveAsync(IFormFile? file, UploadProfile profile)
    {
        if (file is null || file.Length == 0)
        {
            return new MediaResult(false, null, "Chưa chọn file.");
        }

        var config = GetOptions(profile);
        var originalName = Path.GetFileName(file.FileName);

        if (file.Length > config.MaxFileSizeBytes)
        {
            return new MediaResult(false, null,
                $"File vượt quá {config.MaxFileSizeMb}MB.", originalName);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        var allowed = config.EffectiveExtensions
            .Select(e => e.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        if (!allowed.Contains(extension))
        {
            return new MediaResult(false, null,
                $"Chỉ nhận file {string.Join(", ", config.EffectiveExtensions)}.", originalName);
        }

        // Đuôi đúng nhưng khai báo kiểu lạ vẫn bị chặn — đổi tên file .exe thành .jpg không lọt được
        if (!IsAllowedContentType(file.ContentType, extension))
        {
            return new MediaResult(false, null, "Định dạng file không hợp lệ.", originalName);
        }

        // Chia thư mục theo tháng: một thư mục vài chục nghìn file sẽ chậm khi liệt kê
        var monthFolder = DateTime.Now.ToString("yyyy-MM");
        var relativeFolder = Path.Combine(config.FolderPath, monthFolder).Replace('\\', '/');
        var absoluteFolder = Path.Combine(env.WebRootPath, relativeFolder.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(absoluteFolder);

        // Tên file LUÔN là GUID — không bao giờ dùng tên gốc của người dùng
        // (chống path traversal "../../web.config" và chống ghi đè file người khác)
        var storedName = Guid.NewGuid().ToString("N") + extension;
        var absolutePath = Path.Combine(absoluteFolder, storedName);

        try
        {
            await using var stream = File.Create(absolutePath);
            await file.CopyToAsync(stream);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Không ghi được file upload vào {Path}", absolutePath);
            return new MediaResult(false, null, "Không lưu được file, vui lòng thử lại.", originalName);
        }

        // TODO (Giai đoạn 7): resize/nén ảnh tại đây (ImageSharp). Mọi chỗ gọi đều đi qua hàm này
        // nên thêm resize sau này KHÔNG phải sửa controller nào.

        return new MediaResult(true, "/" + relativeFolder + "/" + storedName, null, originalName);
    }

    public async Task<IReadOnlyList<MediaResult>> SaveManyAsync(IEnumerable<IFormFile> files, UploadProfile profile)
    {
        var results = new List<MediaResult>();

        foreach (var file in files)
        {
            results.Add(await SaveAsync(file, profile));
        }

        return results;
    }

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var trimmed = relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var uploadRootAbsolute = Path.GetFullPath(Path.Combine(env.WebRootPath, UploadRoot));
        var target = Path.GetFullPath(Path.Combine(env.WebRootPath, trimmed));

        // Chốt chặn cuối: đường dẫn đến từ DB, nhưng nếu DB bị ghi bậy thì cũng chỉ xóa được
        // trong wwwroot/uploads — không bao giờ chạm tới file mã nguồn hay ảnh placeholder
        if (!target.StartsWith(uploadRootAbsolute + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Bỏ qua yêu cầu xóa file ngoài thư mục uploads: {Path}", relativePath);
            return;
        }

        try
        {
            if (File.Exists(target))
            {
                File.Delete(target);
            }
        }
        catch (IOException ex)
        {
            // File rác không xóa được không phải lỗi nghiệp vụ — bản ghi DB đã gỡ rồi
            logger.LogWarning(ex, "Không xóa được file {Path}", target);
        }
    }

    private static bool IsAllowedContentType(string? contentType, string extension)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        contentType = contentType.ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => contentType is "image/jpeg" or "image/jpg" or "image/pjpeg",
            ".png" => contentType == "image/png",
            ".webp" => contentType == "image/webp",
            ".gif" => contentType == "image/gif",
            ".pdf" => contentType == "application/pdf",
            _ => false
        };
    }
}
