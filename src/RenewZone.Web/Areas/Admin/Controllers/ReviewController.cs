using Microsoft.AspNetCore.Mvc;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Web.Areas.Admin.Models;

namespace RenewZone.Web.Areas.Admin.Controllers;

/// <summary>
/// Quản lý đánh giá sản phẩm (Giai đoạn 8). Khách gửi đánh giá ở PDP ⇒ vào trạng thái chờ duyệt;
/// admin duyệt / từ chối / trả lời công khai / xóa tại đây. Mọi thao tác đổi trạng thái đều khiến
/// service tính lại Product.AvgRating + ReviewCount (card + PDP đọc thẳng 2 cột đó).
/// </summary>
public class ReviewController(IReviewAdminService reviews) : BaseAdminController
{
    [HttpGet("/admin/danh-gia-san-pham")]
    public async Task<IActionResult> Index(string? trangThai = null, string? q = null, int page = 1)
    {
        ReviewStatus? status = Enum.TryParse<ReviewStatus>(trangThai, out var s) && Enum.IsDefined(s)
            ? s : null;

        var result = await reviews.GetListAsync(new ReviewListRequest
        {
            Status = status,
            Search = q,
            Page = page,
            PageSize = 20
        });

        return View(new ReviewListViewModel
        {
            Result = result,
            Counts = await reviews.GetCountsAsync(),
            Status = status,
            Search = q
        });
    }

    /// <summary>Duyệt / từ chối / trả về chờ duyệt bằng AJAX.</summary>
    [HttpPost("/admin/danh-gia-san-pham/{id:int}/trang-thai")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] SetReviewStatusInput? input)
    {
        if (input is null || !Enum.IsDefined(input.Status))
        {
            return BadRequest(new { ok = false, message = "Dữ liệu trạng thái không hợp lệ." });
        }

        var result = await reviews.SetStatusAsync(id, input.Status);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    /// <summary>Ghi/gỡ phản hồi công khai của shop bằng AJAX.</summary>
    [HttpPost("/admin/danh-gia-san-pham/{id:int}/tra-loi")]
    public async Task<IActionResult> Reply(int id, [FromBody] ReplyReviewInput? input)
    {
        var result = await reviews.ReplyAsync(id, input?.Reply);
        return Json(new { ok = result.Ok, message = result.Message });
    }

    [HttpPost("/admin/danh-gia-san-pham/{id:int}/xoa")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await reviews.DeleteAsync(id);

        if (result.Ok) { ToastSuccess(result.Message); } else { ToastError(result.Message); }

        return RedirectToAction(nameof(Index));
    }

    public record SetReviewStatusInput(ReviewStatus Status);

    public record ReplyReviewInput(string? Reply)
    {
        // Cắt về đúng giới hạn cột (2000) để body rác không làm vỡ SaveChanges
        public string? Reply { get; init; } = Reply?.Length > 2000 ? Reply[..2000] : Reply;
    }
}
