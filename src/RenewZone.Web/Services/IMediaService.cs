using RenewZone.Web.Models.Options;

namespace RenewZone.Web.Services;

/// <summary>
/// Nơi DUY NHẤT trong hệ thống ghi file người dùng tải lên.
/// <para>
/// Trích ra từ <c>ContactController.TryStoreAttachmentAsync</c> ở Giai đoạn 6: admin có 4 chỗ upload
/// nữa (ảnh sản phẩm, thumbnail bài viết, banner, ảnh trong editor) — copy-paste logic bảo mật
/// sang từng chỗ là cách chắc chắn nhất để một chỗ nào đó quên mất một lớp kiểm.
/// </para>
/// Đặt ở tầng Web (không phải Infrastructure) vì phụ thuộc <c>IFormFile</c> +
/// <c>IWebHostEnvironment</c> — giống <c>ICartService</c> phụ thuộc Session.
/// </summary>
public interface IMediaService
{
    /// <summary>Lưu 1 file. KHÔNG ném lỗi khi file sai — trả <see cref="MediaResult"/> có <c>Error</c>.</summary>
    Task<MediaResult> SaveAsync(IFormFile? file, UploadProfile profile);

    /// <summary>
    /// Lưu nhiều file. File nào lỗi thì chỉ mình nó lỗi, các file còn lại vẫn được lưu —
    /// upload 5 ảnh mà hỏng 1 thì không có lý do gì bắt admin chọn lại từ đầu.
    /// </summary>
    Task<IReadOnlyList<MediaResult>> SaveManyAsync(IEnumerable<IFormFile> files, UploadProfile profile);

    /// <summary>Xóa file rác (gỡ ảnh sản phẩm). Đường dẫn ngoài wwwroot/uploads bị bỏ qua.</summary>
    void Delete(string? relativePath);

    /// <summary>Giới hạn của một hồ sơ — view cần để hiện <c>accept</c> và dòng "tối đa N MB".</summary>
    UploadProfileOptions GetOptions(UploadProfile profile);
}

/// <param name="Ok">false ⇒ file bị từ chối, xem <paramref name="Error"/>.</param>
/// <param name="RelativePath">Đường dẫn web dùng được ngay trong <c>src</c>, vd "/uploads/products/2026-07/ab12.jpg".</param>
/// <param name="Error">Thông báo tiếng Việt hiện thẳng cho người dùng.</param>
/// <param name="OriginalFileName">Tên gốc — chỉ để hiển thị trong thông báo lỗi, KHÔNG dùng đặt tên file.</param>
public record MediaResult(bool Ok, string? RelativePath, string? Error, string? OriginalFileName = null);

/// <summary>Các chỗ upload trong hệ thống — mỗi giá trị khớp một mục trong section "Uploads".</summary>
public enum UploadProfile
{
    Contact,
    Product,
    News,
    Banner,
    Editor
}
