using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Web.Helpers;
using RenewZone.Web.Models.Options;
using RenewZone.Web.Models.ViewModels;
using RenewZone.Web.Services;

namespace RenewZone.Web.Controllers;

/// <summary>
/// Trang liên hệ. Route literal 1 segment ⇒ attribute routing để thắng fallback "{slug}".
/// POST theo mô hình PRG: lưu xong redirect sang GET ?sent=1 nên F5 không tạo bản ghi trùng.
/// </summary>
public class ContactController : Controller
{
    /// <summary>Khóa session lưu thời điểm gửi gần nhất — chặn spam bấm liên tục.</summary>
    private const string LastSubmitSessionKey = "rz_contact_last";

    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(30);

    private readonly IContactService _contact;
    private readonly IContentBlockService _blocks;
    private readonly ISiteContentService _siteContent;
    private readonly IMediaService _media;
    private readonly UploadProfileOptions _upload;

    public ContactController(
        IContactService contact,
        IContentBlockService blocks,
        ISiteContentService siteContent,
        IMediaService media)
    {
        _contact = contact;
        _blocks = blocks;
        _siteContent = siteContent;
        _media = media;
        _upload = media.GetOptions(UploadProfile.Contact);
    }

    [HttpGet("/lien-he")]
    public async Task<IActionResult> Index(int sent = 0)
    {
        return View(await BuildViewModelAsync(new ContactInput(), justSent: sent == 1));
    }

    [HttpPost("/lien-he")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)] // chặn sớm ở tầng request, giới hạn từng file kiểm riêng bên dưới
    public async Task<IActionResult> Index([Bind(Prefix = "Input")] ContactInput input, IFormFile? attachment)
    {
        // Honeypot: bot điền vào input ẩn ⇒ giả vờ thành công nhưng KHÔNG ghi DB
        if (!string.IsNullOrWhiteSpace(input.Website))
        {
            return RedirectToAction(nameof(Index), new { sent = 1 });
        }

        // Chặn tần suất: 2 lần gửi cách nhau dưới 30 giây
        var lastTicks = HttpContext.Session.GetString(LastSubmitSessionKey);
        if (lastTicks is not null
            && long.TryParse(lastTicks, out var ticks)
            && DateTime.Now - new DateTime(ticks) < MinInterval)
        {
            ModelState.AddModelError(string.Empty,
                "Bạn vừa gửi liên hệ xong. Vui lòng đợi khoảng 30 giây rồi gửi tiếp nhé.");
            return View(await BuildViewModelAsync(input, justSent: false));
        }

        // File đính kèm: validate server, KHÔNG tin phần kiểm ở client
        var attachmentPath = await TryStoreAttachmentAsync(attachment);

        if (!ModelState.IsValid)
        {
            // Trả lại view kèm dữ liệu đã nhập — khách không phải gõ lại
            return View(await BuildViewModelAsync(input, justSent: false));
        }

        await _contact.SubmitAsync(input.ToRequest(attachmentPath));

        HttpContext.Session.SetString(LastSubmitSessionKey, DateTime.Now.Ticks.ToString());

        return RedirectToAction(nameof(Index), new { sent = 1 });
    }

    /// <summary>
    /// Lưu file đính kèm qua <see cref="IMediaService"/> (nơi duy nhất ghi file trong hệ thống —
    /// đổi tên GUID, whitelist đuôi + ContentType, giới hạn dung lượng đều nằm trong đó).
    /// Ở đây chỉ dịch kết quả sang ModelState để lỗi hiện đúng ô trên form.
    /// </summary>
    private async Task<string?> TryStoreAttachmentAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null; // không đính kèm là hợp lệ — đây là trường tùy chọn
        }

        var result = await _media.SaveAsync(file, UploadProfile.Contact);

        if (!result.Ok)
        {
            ModelState.AddModelError("attachment", result.Error!);
            return null;
        }

        return result.RelativePath;
    }

    private async Task<ContactViewModel> BuildViewModelAsync(ContactInput input, bool justSent)
    {
        var settings = await _siteContent.GetSettingsAsync();

        var subjectsRaw = settings.Get(SettingKeys.ContactSubjects);
        var subjects = subjectsRaw
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        return new ContactViewModel
        {
            Input = input,
            Subjects = subjects,
            Channels = await _blocks.GetAsync(ContentBlockGroups.ContactChannels),
            JustSent = justSent,
            MaxFileSizeMb = _upload.MaxFileSizeMb,
            AcceptAttribute = _upload.AcceptAttribute
        };
    }
}
