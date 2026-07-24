using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Web.Helpers;
using RenewZone.Web.Hubs;

namespace RenewZone.Web.Controllers;

/// <summary>
/// Nhận đánh giá của khách gửi từ trang chi tiết sản phẩm (Giai đoạn 8). Endpoint AJAX 2 segment
/// nên fallback route "{slug}" (1 segment) không chạm tới. Đánh giá vào trạng thái CHỜ DUYỆT,
/// chưa hiện ra web khách — nên trả JSON để JS báo "đã gửi, chờ duyệt" thay vì reload cả PDP.
/// Chống spam giống form liên hệ: honeypot + chặn tần suất 30s (KHÔNG giới hạn theo sản phẩm).
/// </summary>
public class ReviewController(
    IReviewService reviews,
    IHubContext<AdminNotifyHub> adminHub,
    ILogger<ReviewController> logger) : Controller
{
    private const string LastSubmitSessionKey = "rz_review_last";
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(30);

    [HttpPost("/danh-gia/gui")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([FromBody] ReviewSubmitRequest? request)
    {
        if (request is null)
        {
            return Json(new { ok = false, message = "Dữ liệu không hợp lệ." });
        }

        // Honeypot: người thật không điền ⇒ giả vờ thành công nhưng KHÔNG ghi DB
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            return Json(new { ok = true, message = "Cảm ơn bạn đã đánh giá! Nội dung sẽ hiển thị sau khi được duyệt." });
        }

        // Chặn tần suất
        var lastTicks = HttpContext.Session.GetString(LastSubmitSessionKey);
        if (lastTicks is not null
            && long.TryParse(lastTicks, out var ticks)
            && DateTime.Now - new DateTime(ticks) < MinInterval)
        {
            return Json(new { ok = false, message = "Bạn vừa gửi đánh giá xong. Vui lòng đợi khoảng 30 giây rồi gửi tiếp nhé." });
        }

        // Validate ở SERVER — không tin client
        var name = (request.AuthorName ?? "").Trim();
        var content = (request.Content ?? "").Trim();

        if (request.Rating is < 1 or > 5)
        {
            return Json(new { ok = false, message = "Vui lòng chọn số sao từ 1 đến 5." });
        }
        if (name.Length is < 2 or > 150)
        {
            return Json(new { ok = false, message = "Vui lòng nhập tên hiển thị (2–150 ký tự)." });
        }
        if (content.Length is < 10 or > 2000)
        {
            return Json(new { ok = false, message = "Nội dung đánh giá cần từ 10 đến 2000 ký tự." });
        }

        var review = await reviews.SubmitAsync(new ReviewRequest
        {
            ProductId = request.ProductId,
            AuthorName = name,
            Rating = request.Rating,
            Content = content
        });

        if (review is null)
        {
            return Json(new { ok = false, message = "Sản phẩm không tồn tại hoặc đã ngừng bán." });
        }

        HttpContext.Session.SetString(LastSubmitSessionKey, DateTime.Now.Ticks.ToString());

        await NotifyAdminsAsync(review, request.ProductName);

        return Json(new { ok = true, message = "Cảm ơn bạn đã đánh giá! Nội dung sẽ hiển thị sau khi được shop duyệt." });
    }

    /// <summary>
    /// Đẩy sự kiện "đánh giá mới" xuống mọi trang admin đang mở (chuông riêng + toast + ting).
    /// Bọc try/catch chỉ LOG: đánh giá ĐÃ lưu DB xong, sự cố realtime tuyệt đối không làm hỏng
    /// phản hồi cho khách — y hệt cách CheckoutController đẩy "donMoi".
    /// </summary>
    private async Task NotifyAdminsAsync(ProductReview review, string? productName)
    {
        try
        {
            await adminHub.Clients.Group(AdminNotifyHub.AdminGroup).SendAsync("binhLuanMoi", new
            {
                id = review.Id,
                productId = review.ProductId,
                productName = string.IsNullOrWhiteSpace(productName) ? "sản phẩm" : productName,
                authorName = review.AuthorName,
                rating = review.Rating,
                createdAt = review.CreatedAt
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không gửi được thông báo realtime cho đánh giá {Id}.", review.Id);
        }
    }
}

/// <summary>Body JSON của POST /danh-gia/gui — khách chỉ gửi tên + sao + nội dung, kiểm ở server.</summary>
public class ReviewSubmitRequest
{
    public int ProductId { get; set; }
    public string? AuthorName { get; set; }
    public int Rating { get; set; }
    public string? Content { get; set; }

    /// <summary>Tên sản phẩm để hiện trong toast admin (chỉ hiển thị — không lưu, không tin cậy).</summary>
    public string? ProductName { get; set; }

    /// <summary>Honeypot chống bot — người thật không điền.</summary>
    public string? Website { get; set; }
}
