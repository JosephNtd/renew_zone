using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Infrastructure.Identity;
using RenewZone.Web.Helpers;
using RenewZone.Web.Hubs;
using RenewZone.Web.Models.ViewModels;
using RenewZone.Web.Services;

namespace RenewZone.Web.Controllers;

/// <summary>
/// Đặt hàng. Checkout là POST form thường (không AJAX) theo mô hình PRG: POST hợp lệ ⇒ tạo đơn
/// ⇒ xóa giỏ ⇒ redirect sang trang cảm ơn, nên F5 ở trang cảm ơn không tạo đơn trùng.
/// <para>
/// ⚠️ Giai đoạn 9 bỏ guest checkout: <c>[Authorize]</c> ở cấp class ⇒ khách vãng lai bị cookie
/// event (Program.cs) đưa về <c>/dang-nhap?returnUrl=/dat-hang</c>, đăng nhập xong quay lại đúng
/// chỗ với giỏ hàng đã được gộp sang tài khoản. <c>Complete</c> vẫn nằm trong vùng bảo vệ vì chỉ
/// người vừa đặt đơn mới tới đó.
/// </para>
/// </summary>
[Authorize]
public class CheckoutController(
    ICartService cart,
    IOrderService orders,
    ICustomerAddressService addresses,
    UserManager<ApplicationUser> userManager,
    IHubContext<AdminNotifyHub> adminHub,
    ILogger<CheckoutController> logger) : Controller
{
    /// <summary>Khóa TempData mang dữ liệu tóm tắt đơn sang trang cảm ơn.</summary>
    private const string CompletedKey = "rz_order_done";

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("/dat-hang")]
    public async Task<IActionResult> Index()
    {
        var current = await cart.GetCartAsync();
        if (current.IsEmpty)
        {
            return RedirectToAction(nameof(CartController.Index), "Cart");
        }

        var book = await addresses.GetMyAddressesAsync(UserId);
        var input = new CheckoutInput();

        // Tự điền: ưu tiên địa chỉ mặc định trong sổ, không có thì lấy tên/SĐT từ hồ sơ.
        // Khách vẫn sửa được mọi ô — đây là gợi ý cho nhanh, không phải khóa cứng.
        var preferred = book.FirstOrDefault(a => a.IsDefault) ?? book.FirstOrDefault();
        if (preferred is not null)
        {
            input.CustomerName = preferred.ReceiverName;
            input.Phone = preferred.Phone;
            input.Address = preferred.AddressLine;
            input.SelectedAddressId = preferred.Id;
        }
        else if (await userManager.GetUserAsync(User) is { } user)
        {
            input.CustomerName = user.FullName ?? "";
            input.Phone = user.PhoneNumber ?? user.UserName ?? "";
            input.Email = user.Email;
        }

        ViewData["Title"] = "Đặt hàng";
        return View(new CheckoutViewModel { Input = input, Cart = current, Addresses = book });
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
            return View(new CheckoutViewModel
            {
                Input = input,
                Cart = current,
                Addresses = await addresses.GetMyAddressesAsync(UserId)
            });
        }

        var order = await orders.CreateOrderAsync(input.ToRequest(UserId), current);
        await cart.ClearAsync();

        await NotifyAdminsAsync(order);

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

    /// <summary>
    /// Đẩy sự kiện "đơn mới" xuống mọi trang admin đang mở (chuông + toast + chèn dòng).
    /// Tiền và nhãn được format Ở SERVER (giống Lưu ý 5c — client không tự tính).
    ///
    /// Đơn ĐÃ lưu DB xong khi hàm này chạy ⇒ mọi sự cố realtime (không có client admin nào,
    /// lỗi mạng nội bộ) chỉ được LOG chứ tuyệt đối không làm hỏng luồng đặt hàng của khách.
    /// </summary>
    private async Task NotifyAdminsAsync(Domain.Entities.Order order)
    {
        try
        {
            await adminHub.Clients.Group(AdminNotifyHub.AdminGroup).SendAsync("donMoi", new
            {
                id = order.Id,
                code = order.Code,
                customerName = order.CustomerName,
                phone = order.Phone,
                itemCount = order.Items.Count,
                totalText = order.TotalAmount.ToVnd(),
                paymentIcon = order.PaymentMethod.ToIcon(),
                paymentLabel = order.PaymentMethod.ToShortLabel(),
                statusText = OrderStatus.New.ToVietnamese(),
                statusClass = OrderStatus.New.ToBadgeClass(),
                statusIcon = OrderStatus.New.ToIcon(),
                createdAt = order.CreatedAt,

                // Các bước chuyển hợp lệ cho dropdown đổi trạng thái ở đầu bảng (khớp hình dạng
                // response của OrderController.ChangeStatus ⇒ JS dựng dropdown y như hàng render sẵn).
                allowedNext = OrderStatusRules.AllowedNext(OrderStatus.New).Select(s => new
                {
                    value = s.ToString(),
                    text = s.ToVietnamese(),
                    icon = s.ToIcon()
                })
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không gửi được thông báo realtime cho đơn {Code}.", order.Code);
        }
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
