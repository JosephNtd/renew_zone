using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Web.Helpers;
using RenewZone.Web.Models.ViewModels;

namespace RenewZone.Web.Controllers;

/// <summary>
/// Trang listing tin công nghệ. Route literal 1 segment nên PHẢI dùng attribute routing
/// để thắng fallback "{slug}" đăng ký cuối Program.cs (CLAUDE.md mục 4.3).
/// </summary>
public class NewsController : Controller
{
    private const int PageSize = 9;

    private readonly INewsQueryService _news;
    private readonly ISiteContentService _siteContent;

    public NewsController(INewsQueryService news, ISiteContentService siteContent)
    {
        _news = news;
        _siteContent = siteContent;
    }

    [HttpGet("/tin-tuc")]
    public async Task<IActionResult> Index(string? chuyenMuc, string? q, int page = 1)
    {
        var data = await _news.GetListAsync(chuyenMuc, q, page, PageSize);
        var settings = await _siteContent.GetSettingsAsync();

        var vm = new NewsListViewModel
        {
            Data = data,
            PageTitle = settings.Get(SettingKeys.NewsPageTitle, "Tin công nghệ"),
            PageIntro = settings.Get(SettingKeys.NewsPageIntro,
                "Tin tức, đánh giá và mẹo dùng thiết bị Apple.")
        };

        ViewData["Title"] = vm.PageTitle;
        ViewData["MetaDescription"] = vm.PageIntro;

        return View(vm);
    }
}
