using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;
using RenewZone.Web.Models.ViewModels;

namespace RenewZone.Web.Controllers;

/// <summary>
/// Các trang catalog có URL TĨNH (không phải slug): danh sách tất cả sản phẩm và tìm kiếm.
/// Dùng attribute routing — literal "san-pham"/"tim-kiem" thắng fallback "{slug}" nhờ độ ưu tiên route.
/// </summary>
public class ProductController : Controller
{
    private const int PageSize = 12;

    private readonly ICatalogQueryService _catalog;

    public ProductController(ICatalogQueryService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("/san-pham")]
    public async Task<IActionResult> All(int page = 1)
    {
        var results = await _catalog.GetAllProductsAsync(page, PageSize);

        var vm = new ProductListViewModel
        {
            Title = "Tất cả sản phẩm",
            BasePath = "/san-pham",
            Results = results
        };

        return View(vm);
    }

    [HttpGet("/tim-kiem")]
    public async Task<IActionResult> Search(string? q, int page = 1)
    {
        var results = await _catalog.SearchAsync(q, page, PageSize);

        var vm = new SearchViewModel
        {
            Keyword = q,
            Results = results
        };

        return View(vm);
    }
}
