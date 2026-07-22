using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Interfaces;
using RenewZone.Web.Helpers;
using RenewZone.Web.Models.ViewModels;
using RenewZone.Web.Services;

namespace RenewZone.Web.Controllers;

/// <summary>
/// Đặt hàng cho khách vãng lai (Giai đoạn 4). Checkout là POST form thường (không AJAX)
/// theo mô hình PRG: POST hợp lệ ⇒ tạo đơn ⇒ xóa giỏ ⇒ redirect sang trang cảm ơn,
/// nên F5 ở trang cảm ơn không tạo đơn trùng.
/// </summary>
public class CheckoutController(ICartService cart, IOrderService orders) : Controller
{
    /// <summary>Khóa TempData mang dữ liệu tóm tắt đơn sang trang cảm ơn.</summary>
    private const string CompletedKey = "rz_order_done";

    [HttpGet("/dat-hang")]
    public async Task<IActionResult> Index()
    {
        var current = await cart.GetCartAsync();
        if (current.IsEmpty)
        {
            return RedirectToAction(nameof(CartController.Index), "Cart");
        }

        ViewData["Title"] = "Đặt hàng";
        return View(new CheckoutViewModel { Input = new CheckoutInput(), Cart = current });
    }

    [HttpPost("/dat-hang")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutInput input)
    {
        var current = await cart.GetCartAsync();
        if (current.IsEmpty)
        {
            return RedirectToAction(nameof(CartController.Index), "Cart");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Đặt hàng";
            return View(new CheckoutViewModel { Input = input, Cart = current });
        }

        var order = await orders.CreateOrderAsync(input.ToRequest(), current);
        await cart.ClearAsync();

        // Tiền đã format sẵn ở đây: TempData chỉ giữ được kiểu đơn giản, và trang cảm ơn
        // không được tính lại tổng từ giỏ (giỏ vừa bị xóa).
        var done = new OrderCompleteViewModel
        {
            Code = order.Code,
            CustomerName = order.CustomerName,
            Phone = order.Phone,
            TotalText = order.TotalAmount.ToVnd(),
            PaymentMethod = order.PaymentMethod
        };

        TempData[CompletedKey] = JsonSerializer.Serialize(done);
        return RedirectToAction(nameof(Complete));
    }

    [HttpGet("/dat-hang-thanh-cong")]
    public IActionResult Complete()
    {
        // Vào thẳng URL (hoặc F5 sau khi TempData đã tiêu thụ) ⇒ không có gì để khoe, về trang chủ.
        if (TempData[CompletedKey] is not string raw)
        {
            return Redirect("/");
        }

        var vm = JsonSerializer.Deserialize<OrderCompleteViewModel>(raw);
        if (vm is null)
        {
            return Redirect("/");
        }

        ViewData["Title"] = "Đặt hàng thành công";
        return View(vm);
    }
}
