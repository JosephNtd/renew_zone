using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Web.Models.ViewModels;

namespace RenewZone.Web.Controllers;

/// <summary>
/// Đích của fallback route "{slug}" (CLAUDE.md mục 4.3). Tra slug phẳng ở root rồi render đúng
/// view theo loại: danh mục / sản phẩm / bài viết / trang tĩnh. Không khớp ⇒ 404.
/// </summary>
public class SlugController : Controller
{
    private const int CategoryPageSize = 12;

    private readonly ISlugResolver _resolver;
    private readonly ICatalogQueryService _catalog;
    private readonly IContentQueryService _content;

    public SlugController(ISlugResolver resolver, ICatalogQueryService catalog, IContentQueryService content)
    {
        _resolver = resolver;
        _catalog = catalog;
        _content = content;
    }

    // GET /{slug}?series=&page=
    public async Task<IActionResult> Resolve(string slug, string? series, int page = 1)
    {
        var match = await _resolver.ResolveAsync(slug);
        if (match is null)
        {
            return NotFound();
        }

        return match.Type switch
        {
            SlugTargetType.Category => await CategoryView(match.Id, series, page),
            SlugTargetType.Product => await ProductView(match.Id),
            SlugTargetType.NewsArticle => await NewsView(match.Id),
            SlugTargetType.StaticPage => await StaticPageView(match.Id),
            _ => NotFound()
        };
    }

    private async Task<IActionResult> CategoryView(int id, string? series, int page)
    {
        var data = await _catalog.GetCategoryPageAsync(id, series, page, CategoryPageSize);
        if (data is null)
        {
            return NotFound();
        }

        return View("Category", new CategoryViewModel { Data = data });
    }

    private async Task<IActionResult> ProductView(int id)
    {
        var detail = await _catalog.GetProductDetailAsync(id);
        if (detail is null)
        {
            return NotFound();
        }

        return View("Product", detail);
    }

    private async Task<IActionResult> NewsView(int id)
    {
        var detail = await _content.GetNewsDetailAsync(id);
        if (detail is null)
        {
            return NotFound();
        }

        return View("NewsDetail", detail);
    }

    private async Task<IActionResult> StaticPageView(int id)
    {
        var pageData = await _content.GetStaticPageAsync(id);
        if (pageData is null)
        {
            return NotFound();
        }

        return View("StaticPage", pageData);
    }
}
