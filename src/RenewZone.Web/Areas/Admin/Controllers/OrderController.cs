using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Web.Areas.Admin.Models;
using RenewZone.Web.Helpers;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// Quản lý đơn hàng. Route tiếng Việt bằng attribute routing giống web khách.
/// Đơn hàng KHÔNG có thao tác xóa — muốn hủy thì chuyển trạng thái sang Cancelled.
/// </summary>
public class OrderController(IOrderAdminService orders) : BaseAdminController
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

        var result = await orders.ChangeStatusAsync(id, input.Status);

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

    public record ChangeStatusInput(OrderStatus Status);
}
