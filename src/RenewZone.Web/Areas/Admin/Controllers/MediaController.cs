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
    public async Task<IActionResult> UploadEditor(IFormFile? file)
    {
        var result = await media.SaveAsync(file, UploadProfile.Editor);

        if (!result.Ok)
        {
            // TinyMCE hiện thông báo này ngay trong editor
            return BadRequest(new { message = result.Error });
        }

        return Json(new { location = result.RelativePath });
    }
}
