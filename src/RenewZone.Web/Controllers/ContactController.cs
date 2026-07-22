using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Web.Helpers;
using RenewZone.Web.Models.Options;
using RenewZone.Web.Models.ViewModels;

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
    private readonly IWebHostEnvironment _env;
    private readonly ContactUploadOptions _upload;

    public ContactController(
        IContactService contact,
        IContentBlockService blocks,
        ISiteContentService siteContent,
        IWebHostEnvironment env,
        IOptions<ContactUploadOptions> upload)
    {
        _contact = contact;
        _blocks = blocks;
        _siteContent = siteContent;
        _env = env;
        _upload = upload.Value;
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
        // ----- Honeypot: bot điền vào input ẩn ⇒ giả vờ thành công nhưng KHÔNG ghi DB -----
        if (!string.IsNullOrWhiteSpace(input.Website))
        {
            return RedirectToAction(nameof(Index), new { sent = 1 });
        }

        // ----- Chặn tần suất: 2 lần gửi cách nhau dưới 30 giây -----
        var lastTicks = HttpContext.Session.GetString(LastSubmitSessionKey);
        if (lastTicks is not null
            && long.TryParse(lastTicks, out var ticks)
            && DateTime.Now - new DateTime(ticks) < MinInterval)
        {
            ModelState.AddModelError(string.Empty,
                "Bạn vừa gửi liên hệ xong. Vui lòng đợi khoảng 30 giây rồi gửi tiếp nhé.");
            return View(await BuildViewModelAsync(input, justSent: false));
        }

        // ----- File đính kèm: validate server, KHÔNG tin phần kiểm ở client -----
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
    /// Lưu file đính kèm nếu hợp lệ. Sai định dạng/quá nặng ⇒ ghi lỗi vào ModelState và trả null
    /// (không throw). Tên file LUÔN đổi thành GUID — không bao giờ dùng tên gốc của khách
    /// (chống path traversal và ghi đè file người khác).
    /// </summary>
    private async Task<string?> TryStoreAttachmentAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > _upload.MaxFileSizeBytes)
        {
            ModelState.AddModelError("attachment", $"File đính kèm tối đa {_upload.MaxFileSizeMb}MB.");
            return null;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = _upload.EffectiveExtensions
            .Select(e => e.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        if (!allowed.Contains(extension))
        {
            ModelState.AddModelError("attachment",
                $"Chỉ nhận file {string.Join(", ", _upload.EffectiveExtensions)}.");
            return null;
        }

        // Kiểm thêm ContentType: đuôi đúng nhưng khai báo kiểu lạ vẫn bị chặn
        if (!IsAllowedContentType(file.ContentType, extension))
        {
            ModelState.AddModelError("attachment", "Định dạng file không hợp lệ.");
            return null;
        }

        var monthFolder = DateTime.Now.ToString("yyyy-MM");
        var relativeFolder = Path.Combine(_upload.FolderPath, monthFolder).Replace('\\', '/');
        var absoluteFolder = Path.Combine(_env.WebRootPath, relativeFolder.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(absoluteFolder);

        var storedName = Guid.NewGuid().ToString("N") + extension;
        var absolutePath = Path.Combine(absoluteFolder, storedName);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream);
        }

        return "/" + relativeFolder + "/" + storedName;
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
            ".pdf" => contentType == "application/pdf",
            _ => false
        };
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
            AcceptAttribute = string.Join(",", _upload.EffectiveExtensions)
        };
    }
}
