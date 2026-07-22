using Microsoft.AspNetCore.Mvc;
using RenewZone.Web.Services;

namespace RenewZone.Web.ViewComponents;

/// <summary>
/// Số lượng trên icon giỏ ở header. Render server-side để lần tải trang đầu đã đúng số;
/// các thay đổi sau đó do cart.js cập nhật qua <c>RZ.cartBadge()</c>, không reload trang.
/// </summary>
public class CartBadgeViewComponent(ICartService cart) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var current = await cart.GetCartAsync();
        return View(current.TotalQuantity);
    }
}
