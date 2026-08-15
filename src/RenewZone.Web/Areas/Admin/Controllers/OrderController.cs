using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Web.Areas.Admin.Models;
using RenewZone.Web.Helpers;
using RenewZone.Web.Hubs;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// Quản lý đơn hàng. Route tiếng Việt bằng attribute routing giống web khách.
/// Đơn hàng KHÔNG có thao tác xóa — muốn hủy thì chuyển trạng thái sang Cancelled.
/// </summary>
public class OrderController(
    IOrderAdminService orders,
    IHubContext<CustomerNotifyHub> customerHub,
    ILogger<OrderController> logger) : BaseAdminController
{
    [HttpGet("/admin/don-hang")]
    public async Task<IActionResult> Index(
        OrderStatus? trangThai = null,
        PaymentMethod? thanhToan = null,
        string? q = null,
        DateTime? tuNgay = null,
        DateTime? denNgay = null,
        string? sort = null,
        bool desc = false,
        int page = 1)
    {
        var request = new OrderListRequest
        {
            Status = trangThai,
            PaymentMethod = thanhToan,
            Search = q,
            FromDate = tuNgay,
            ToDate = denNgay,
            SortBy = sort,
            SortDesc = desc,
            Page = page
        };

        var model = new OrderListViewModel
        {
            Result = await orders.GetListAsync(request),
            StatusCounts = await orders.GetStatusCountsAsync(),
            Status = trangThai,
            PaymentMethod = thanhToan,
            Search = q,
            FromDate = tuNgay,
            ToDate = denNgay,
            SortBy = sort,
            SortDesc = desc
        };

        return View(model);
    }

    [HttpGet("/admin/don-hang/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var order = await orders.GetDetailAsync(id);

        if (order is null)
        {
            return NotFound();
        }

        ViewData["Crumb"] = "Đơn hàng";
        ViewData["CrumbUrl"] = Url.Action(nameof(Index));

        return View(order);
    }

    /// <summary>
    /// Đổi trạng thái bằng AJAX. Trả JSON đủ dữ liệu để JS vẽ lại badge + dropdown tại chỗ,
    /// không phải tải lại cả trang.
    /// </summary>
    [HttpPost("/admin/don-hang/{id:int}/trang-thai")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusInput? input)
    {
        // Body sai định dạng (trạng thái lạ, JSON hỏng) ⇒ binder trả null. Không chặn ở đây thì
        // action ném NullReferenceException và client nhận trang lỗi HTML thay vì JSON.
        if (input is null || !Enum.IsDefined(input.Status))
        {
            return BadRequest(new { ok = false, message = "Dữ liệu trạng thái không hợp lệ." });
        }

        // Người đổi = admin đang đăng nhập. Snapshot tên vào sổ nhật ký (xem OrderStatusHistory).
        var actor = User.Identity?.Name ?? "Admin";

        var result = await orders.ChangeStatusAsync(id, input.Status, actor, input.Note);

        if (result.Ok)
        {
            await NotifyCustomerAsync(result, input.Note);
        }

        return Json(new
        {
            ok = result.Ok,
            message = result.Message,
            status = result.Status.ToString(),
            statusText = result.Status.ToVietnamese(),
            statusClass = result.Status.ToBadgeClass(),
            allowedNext = result.AllowedNext.Select(s => new
            {
                value = s.ToString(),
                text = s.ToVietnamese(),
                icon = s.ToIcon()
            })
        });
    }

    /// <summary>
    /// Báo cho CHÍNH khách đặt đơn biết đơn vừa đổi trạng thái (Giai đoạn 9) — họ đang mở web
    /// thì thấy ngay, không phải F5 trang "Đơn hàng của tôi".
    /// <para>
    /// <c>Clients.User(userId)</c> nhắm đúng một tài khoản (SignalR gom mọi kết nối của họ qua
    /// claim NameIdentifier) ⇒ không có đường nào lọt thông tin đơn sang người khác.
    /// </para>
    /// <para>
    /// Bọc try/catch chỉ LOG, y hệt cách <c>CheckoutController</c> đẩy "donMoi": trạng thái ĐÃ
    /// lưu vào DB xong khi hàm này chạy, sự cố realtime tuyệt đối không được biến một thao tác
    /// thành công của admin thành thông báo lỗi.
    /// </para>
    /// </summary>
    private async Task NotifyCustomerAsync(ChangeStatusResult result, string? note)
    {
        // Đơn của khách vãng lai đặt trước Giai đoạn 9 không có chủ ⇒ không có ai để báo
        if (string.IsNullOrEmpty(result.UserId))
        {
            return;
        }

        try
        {
            await customerHub.Clients.User(result.UserId).SendAsync("donCapNhat", new
            {
                status = result.Status.ToString(),

                // Nhãn + gợi ý format Ở SERVER bằng bộ từ vựng dành cho KHÁCH ("Chờ xác nhận"),
                // không dùng nhãn quản trị ("Mới") — xem AccountDisplayExtensions.
                statusText = result.Status.ToCustomerLabel(),
                statusHint = result.Status.ToCustomerHint(),
                statusClass = result.Status.ToCustomerStatusClass(),
                statusIcon = result.Status.ToCustomerIcon(),
                note
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không gửi được thông báo realtime cho khách {UserId}.", result.UserId);
        }
    }

    /// <summary>
    /// <paramref name="Note"/> tùy chọn — admin nhập ghi chú khi đổi trạng thái (giới hạn 500 ký tự
    /// khớp cột DB, cắt bớt để body rác không làm vỡ SaveChanges).
    /// </summary>
    public record ChangeStatusInput(OrderStatus Status, string? Note = null)
    {
        public string? Note { get; init; } = Note?.Length > 500 ? Note[..500] : Note;
    }
}
