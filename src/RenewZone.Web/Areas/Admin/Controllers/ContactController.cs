using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Web.Areas.Admin.Models;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// Hộp thư liên hệ. Chi tiết mở bằng drawer trượt từ phải (nhanh hơn chuyển trang) và
/// tự đánh dấu đã đọc khi mở.
/// </summary>
public class ContactController(IContactAdminService contacts) : BaseAdminController
{
    [HttpGet("/admin/lien-he")]
    public async Task<IActionResult> Index(
        string? trangThai = null, string? chuDe = null, string? q = null,
        int? id = null, int page = 1)
    {
        bool? isRead = trangThai switch { "daDoc" => true, "chuaDoc" => false, _ => null };

        var result = await contacts.GetListAsync(new ContactListRequest
        {
            IsRead = isRead,
            Subject = chuDe,
            Search = q,
            Page = page,
            PageSize = 20
        });

        return View(new ContactListViewModel
        {
            Result = result,
            Subjects = await contacts.GetSubjectsAsync(),
            IsRead = isRead,
            Subject = chuDe,
            Search = q,
            OpenId = id
        });
    }

    /// <summary>Nội dung drawer + tự đánh dấu đã đọc.</summary>
    [HttpGet("/admin/lien-he/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var message = await contacts.GetAsync(id);

        if (message is null)
        {
            return NotFound();
        }

        // Mở ra là coi như đã đọc — không bắt admin bấm thêm một nút nữa
        await contacts.MarkReadAsync(id);

        return PartialView("_ContactDetail", message);
    }

    [HttpPost("/admin/lien-he/{id:int}/da-doc")]
    public async Task<IActionResult> MarkRead(int id, bool daDoc = true)
    {
        var result = await contacts.MarkReadAsync(id, daDoc);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    [HttpPost("/admin/lien-he/{id:int}/xoa")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await contacts.DeleteAsync(id);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Cấu hình site — nhóm theo chủ đề với nhãn tiếng Việt, không phải bảng key-value trần.</summary>
public class SettingController(ISettingAdminService settings) : BaseAdminController
{
    [HttpGet("/admin/cau-hinh")]
    public async Task<IActionResult> Index()
    {
        return View(new SettingViewModel { Values = await settings.GetAllAsync() });
    }

    [HttpPost("/admin/cau-hinh")]
    public async Task<IActionResult> Index(Dictionary<string, string?> values)
    {
        // Service tự lọc theo whitelist SettingKeys nên key rác trong form bị bỏ qua
        var result = await settings.SaveAsync(values);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Thùng rác — khôi phục hoặc xóa vĩnh viễn bản ghi đã xóa mềm.</summary>
public class TrashController(ITrashService trash) : BaseAdminController
{
    [HttpGet("/admin/thung-rac")]
    public async Task<IActionResult> Index(TrashType loai = TrashType.Product)
    {
        return View(new TrashViewModel
        {
            Type = loai,
            Items = await trash.GetItemsAsync(loai),
            Counts = await trash.GetCountsAsync()
        });
    }

    [HttpPost("/admin/thung-rac/khoi-phuc")]
    public async Task<IActionResult> Restore(TrashType loai, int id)
    {
        var result = await trash.RestoreAsync(loai, id);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index), new { loai });
    }

    [HttpPost("/admin/thung-rac/xoa-vinh-vien")]
    public async Task<IActionResult> Purge(TrashType loai, int id)
    {
        var result = await trash.PurgeAsync(loai, id);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index), new { loai });
    }
}
