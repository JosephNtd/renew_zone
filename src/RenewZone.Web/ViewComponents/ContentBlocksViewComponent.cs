using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;

namespace RenewZone.Web.ViewComponents;

/// <summary>
/// Render một nhóm ContentBlock dạng danh sách icon + chữ (cam kết PDP, dòng trấn an giỏ hàng).
/// Nhóm rỗng ⇒ không render gì cả — view gọi không cần tự kiểm tra.
/// </summary>
public class ContentBlocksViewComponent : ViewComponent
{
    private readonly IContentBlockService _blocks;

    public ContentBlocksViewComponent(IContentBlockService blocks)
    {
        _blocks = blocks;
    }

    /// <param name="groupKey">Khóa nhóm, xem <see cref="ContentBlockGroups"/>.</param>
    /// <param name="cssClass">Class của thẻ &lt;ul&gt; bao ngoài để mỗi trang tự tạo kiểu.</param>
    public async Task<IViewComponentResult> InvokeAsync(string groupKey, string cssClass = "")
    {
        var items = await _blocks.GetAsync(groupKey);

        var vm = new ContentBlocksViewModel { Items = items, CssClass = cssClass };
        return View(vm);
    }
}

public class ContentBlocksViewModel
{
    public required IReadOnlyList<ContentBlock> Items { get; init; }
    public required string CssClass { get; init; }
}
