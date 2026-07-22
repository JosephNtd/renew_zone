using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// Kiểm slug realtime cho ô Slug trong các form admin (chấm ✓/✗ hiện ngay khi gõ).
/// <para>
/// ⚠️ Đây CHỈ là tiện ích UX. Việc kiểm slug thật sự nằm ở service lúc lưu — client có thể
/// tắt JS hoặc gọi thẳng POST, nên không bao giờ được coi endpoint này là chốt chặn.
/// </para>
/// </summary>
public class SlugApiController(ISlugService slugs) : BaseAdminController
{
    [HttpGet("/admin/api/slug/kiem-tra")]
    public async Task<IActionResult> Check(string? slug, SlugTargetType? type = null, int? id = null)
    {
        var normalized = slugs.Slugify(slug ?? string.Empty);
        var result = await slugs.CheckAsync(normalized, type, id);

        return Json(new
        {
            slug = normalized,
            isAvailable = result.IsAvailable,
            isInTrash = result.IsInTrash,
            takenBy = result.TakenBy?.ToString(),
            takenByName = result.TakenByName,
            message = result.Message
        });
    }
}
