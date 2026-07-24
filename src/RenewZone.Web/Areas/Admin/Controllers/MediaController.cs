using Microsoft.AspNetCore.Mvc;
using RenewZone.Web.Services;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// Nhận ảnh chèn trong editor TinyMCE. Trả về đúng hình dạng <c>{ location }</c> mà
/// <c>images_upload_handler</c> của TinyMCE mong đợi.
/// </summary>
public class MediaController(IMediaService media) : BaseAdminController
{
    [HttpPost("/admin/media/upload-editor")]
    public async Task<IActionResult> UploadEditor(IFormFile? file, string? profile)
    {
        // Nút "Tải ảnh lên" ở banner/tin gửi kèm `profile` để áp min-size riêng của loại đó.
        // TinyMCE inline (và các form khác) KHÔNG gửi → mặc định Editor (sàn nhẹ, cho phép ảnh nhỏ
        // chèn trong bài). Whitelist qua Enum.IsDefined để chuỗi/số rác không lọt (Lưu ý 6.16).
        var target = Enum.TryParse<UploadProfile>(profile, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed)
                ? parsed
                : UploadProfile.Editor;

        var result = await media.SaveAsync(file, target);

        if (!result.Ok)
        {
            // TinyMCE hiện thông báo này ngay trong editor
            return BadRequest(new { message = result.Error });
        }

        return Json(new { location = result.RelativePath });
    }
}
