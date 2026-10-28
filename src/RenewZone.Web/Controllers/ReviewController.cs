using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models;
using RenewZone.Infrastructure.Identity;
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
    ICustomerOrderService customerOrders,
    UserManager<ApplicationUser> userManager,
    IHubContext<AdminNotifyHub> adminHub,
    ILogger<ReviewController> logger) : Controller
{
    private const string LastSubmitSessionKey = "rz_review_last";
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(30);

    // Cùng định dạng SĐT với form liên hệ / checkout để nhất quán toàn site.
    private static readonly Regex PhoneRegex = new(@"^(0|\+84)\d{9,10}$", RegexOptions.Compiled);

    [HttpPost("/danh-gia/gui")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([FromBody] ReviewSubmitRequest? request)
    {
        if (request is null)
        {
            return Json(new { ok = false, message = "Dữ liệu không hợp lệ." });
        }

        // Giai đoạn 9: chỉ người ĐÃ MUA sản phẩm này mới được đánh giá (quyết định 2026-07-28).
        // Trả cờ requireLogin để PDP bật hộp thoại đăng nhập thay vì chỉ hiện thông báo lỗi.
        if (User.Identity?.IsAuthenticated != true)
        {
            return Json(new
            {
                ok = false,
                requireLogin = true,
                message = "Vui lòng đăng nhập để đánh giá sản phẩm."
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Chốt chặn THẬT nằm ở đây, không phải ở chỗ ẩn/hiện form trên PDP: gọi thẳng bằng curl
        // với cookie hợp lệ vẫn bị chặn nếu tài khoản chưa có đơn hoàn thành chứa sản phẩm này.
        // Giai đoạn 10: mỗi LẦN MUA (một dòng trong đơn Hoàn thành) chỉ đánh giá được một lần.
        // Gửi từ trang chi tiết đơn thì có sẵn orderItemId; gửi từ PDP thì lấy lần mua cũ nhất chưa đánh giá.
        int? orderItemId = request.OrderItemId is { } wanted
            ? (await customerOrders.CanReviewOrderItemAsync(userId, wanted, request.ProductId) ? wanted : null)
            : await customerOrders.FindReviewableOrderItemAsync(userId, request.ProductId);

        if (orderItemId is null)
        {
            var purchased = await customerOrders.HasPurchasedAsync(userId, request.ProductId);
            return Json(new
            {
                ok = false,
                message = purchased
                    ? "Bạn đã đánh giá sản phẩm này cho mọi lần mua rồi. Cảm ơn bạn!"
                    : "Chỉ khách đã mua và nhận sản phẩm này mới đánh giá được. "
                      + "Nếu bạn vừa nhận hàng, hãy đợi shop cập nhật đơn sang “Hoàn thành” nhé."
            });
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

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Json(new { ok = false, requireLogin = true, message = "Phiên đăng nhập đã hết hạn." });
        }

        // Validate ở SERVER — không tin client
        var content = (request.Content ?? "").Trim();
        var phone = (request.Phone ?? "").Trim();

        // Tên hiển thị lấy từ HỒ SƠ, không nhận từ client nữa: đã đăng nhập rồi thì không có lý do
        // gì để người gửi tự khai tên — đó là cửa để mạo danh khách khác dưới một đánh giá đã duyệt.
        var name = string.IsNullOrWhiteSpace(user.FullName) ? (user.UserName ?? "Khách hàng") : user.FullName.Trim();
        if (name.Length > 150)
        {
            name = name[..150];
        }

        // SĐT mặc định lấy từ hồ sơ (khách vẫn được ghi đè bằng số khác nếu muốn shop gọi số đó)
        if (phone.Length == 0)
        {
            phone = user.PhoneNumber ?? "";
        }

        if (request.Rating is < 1 or > 5)
        {
            return Json(new { ok = false, message = "Vui lòng chọn số sao từ 1 đến 5." });
        }
        if (content.Length is < 10 or > 2000)
        {
            return Json(new { ok = false, message = "Nội dung đánh giá cần từ 10 đến 2000 ký tự." });
        }
        // SĐT là TÙY CHỌN: bỏ trống thì OK; đã nhập thì phải đúng định dạng số VN
        if (phone.Length > 0 && !PhoneRegex.IsMatch(phone))
        {
            return Json(new { ok = false, message = "Số điện thoại không hợp lệ. Vui lòng để trống hoặc nhập đúng số VN (VD: 0912345678)." });
        }

        var review = await reviews.SubmitAsync(new ReviewRequest
        {
            ProductId = request.ProductId,
            UserId = userId,

            // Đã qua HasPurchasedAsync ở trên nên luôn true. Chốt thành cột trong DB thay vì tính
            // lại lúc hiển thị: đơn có thể bị đổi trạng thái về sau, nhưng huy hiệu "Đã mua hàng"
            // phải phản ánh sự thật TẠI THỜI ĐIỂM đánh giá.
            IsVerifiedPurchase = true,
            OrderItemId = orderItemId,
            AuthorName = name,
            Phone = phone.Length > 0 ? phone : null,
            Rating = request.Rating,
            Content = content
        });

        if (review is null)
        {
            return Json(new { ok = false, message = "Không gửi được đánh giá: sản phẩm đã ngừng bán hoặc lần mua này vừa được đánh giá." });
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

    /// <summary>
    /// Dòng đơn hàng được đánh giá — có khi khách bấm "Đánh giá" ở trang chi tiết đơn (Giai đoạn 10).
    /// Bỏ trống (gửi từ PDP) ⇒ server tự chọn lần mua cũ nhất chưa đánh giá. Luôn kiểm lại quyền sở hữu.
    /// </summary>
    public int? OrderItemId { get; set; }

    // Giai đoạn 9 BỎ trường AuthorName: tên hiển thị lấy từ hồ sơ tài khoản đang đăng nhập.
    // Để client tự khai tên là mở đường mạo danh khách khác dưới một đánh giá đã được duyệt.

    /// <summary>
    /// SĐT tùy chọn — để admin liên hệ lại. Bỏ trống thì lấy số của tài khoản.
    /// Chỉ lưu + hiện ở admin, KHÔNG bao giờ render ra web khách (bất biến ở CLAUDE.md mục 4.15).
    /// </summary>
    public string? Phone { get; set; }

    public int Rating { get; set; }
    public string? Content { get; set; }

    /// <summary>Tên sản phẩm để hiện trong toast admin (chỉ hiển thị — không lưu, không tin cậy).</summary>
    public string? ProductName { get; set; }

    /// <summary>Honeypot chống bot — người thật không điền.</summary>
    public string? Website { get; set; }
}
