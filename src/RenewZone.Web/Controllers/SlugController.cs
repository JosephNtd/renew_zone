using System.Security.Claims;
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
    private readonly ICustomerOrderService _customerOrders;

    public SlugController(
        ISlugResolver resolver,
        ICatalogQueryService catalog,
        IContentQueryService content,
        ICustomerOrderService customerOrders)
    {
        _resolver = resolver;
        _catalog = catalog;
        _content = content;
        _customerOrders = customerOrders;
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

        // Tăng lượt xem SAU khi chắc chắn tìm thấy (không đếm 404). +1 mỗi lần tải trang.
        await _catalog.IncrementProductViewAsync(id);

        // Có được mở form đánh giá hay không (Giai đoạn 9). Đây CHỈ là để hiện/ẩn giao diện cho
        // đúng — chốt chặn thật nằm ở ReviewController, nó tự kiểm lại nên gọi thẳng bằng curl
        // vẫn bị từ chối. Giai đoạn 10: còn lần mua nào CHƯA đánh giá thì mới mở form; đã mua
        // nhưng đánh giá hết rồi thì hiện trạng thái "đã đánh giá".
        if (User.Identity?.IsAuthenticated == true
            && User.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } userId)
        {
            var canReview = await _customerOrders.FindReviewableOrderItemAsync(userId, id) is not null;
            ViewData["CanReview"] = canReview;
            ViewData["HasReviewedAll"] = !canReview && await _customerOrders.HasPurchasedAsync(userId, id);
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

        await _content.IncrementNewsViewAsync(id);
        // Trang tin CÓ hiển thị lượt xem cho khách; entity là AsNoTracking nên +1 in-memory để
        // khách thấy đúng số đã bao gồm lượt xem hiện tại (không cần query lại DB).
        detail.Article.ViewCount++;

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
