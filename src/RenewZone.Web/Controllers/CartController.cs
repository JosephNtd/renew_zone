using Microsoft.AspNetCore.Mvc;
using RenewZone.Web.Helpers;
using RenewZone.Web.Models.ViewModels;
using RenewZone.Web.Services;

namespace RenewZone.Web.Controllers;

/// <summary>
/// Giỏ hàng (Giai đoạn 4). Dùng attribute routing: "/gio-hang" thắng fallback "{slug}",
/// còn các endpoint AJAX 2 segment thì fallback (1 segment) vốn không chạm tới.
/// Các action POST nhận JSON + anti-forgery qua header "RequestVerificationToken".
/// </summary>
public class CartController(ICartService cart) : Controller
{
    [HttpGet("/gio-hang")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Giỏ hàng";
        return View(new CartViewModel { Cart = await cart.GetCartAsync() });
    }

    /// <summary>Nội dung offcanvas mini-cart — gọi lại mỗi lần mở/sau khi giỏ đổi.</summary>
    [HttpGet("/gio-hang/mini")]
    public async Task<IActionResult> Mini() => PartialView("_MiniCartBody", await cart.GetCartAsync());

    [HttpPost("/gio-hang/them")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromBody] AddToCartRequest request)
    {
        var result = await cart.AddAsync(
            request.ProductId,
            request.OptionValueIds ?? [],
            request.Quantity <= 0 ? 1 : request.Quantity);

        return CartJson(result);
    }

    [HttpPost("/gio-hang/cap-nhat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] UpdateCartRequest request)
    {
        var result = await cart.UpdateQuantityAsync(request.Key ?? "", request.Quantity);
        return CartJson(result);
    }

    [HttpPost("/gio-hang/xoa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove([FromBody] RemoveCartRequest request)
    {
        var result = await cart.RemoveAsync(request.Key ?? "");
        return CartJson(result, removedKey: request.Key);
    }

    /// <summary>
    /// Hợp đồng JSON dùng chung cho 3 endpoint. Mọi số tiền được server format sẵn (…đ)
    /// để client khỏi tự tính — tránh lệch làm tròn giữa hai bên.
    /// </summary>
    private JsonResult CartJson(CartOpResult result, string? removedKey = null)
    {
        var c = result.Cart;

        return Json(new
        {
            ok = result.Ok,
            message = result.Message,
            removedKey,
            cart = new
            {
                count = c.TotalQuantity,
                subtotal = c.Subtotal,
                subtotalText = c.Subtotal.ToVnd(),
                isEmpty = c.IsEmpty
            },
            item = result.Item is null ? null : new
            {
                key = result.Item.Key,
                name = result.Item.ProductName,
                variant = result.Item.VariantDescription,
                imageUrl = result.Item.ImageUrl,
                url = "/" + result.Item.Slug,
                unitPriceText = result.Item.UnitPrice.ToVnd(),
                quantity = result.Item.Quantity,
                lineTotalText = result.Item.LineTotal.ToVnd()
            }
        });
    }
}

/// <summary>Body JSON của POST /gio-hang/them — client chỉ gửi id, KHÔNG gửi giá.</summary>
public class AddToCartRequest
{
    public int ProductId { get; set; }
    public List<int>? OptionValueIds { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartRequest
{
    public string? Key { get; set; }
    public int Quantity { get; set; }
}

public class RemoveCartRequest
{
    public string? Key { get; set; }
}
