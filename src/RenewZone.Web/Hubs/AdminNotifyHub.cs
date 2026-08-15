using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RenewZone.Infrastructure.Identity;

namespace RenewZone.Web.Hubs;

/// <summary>
/// Kênh realtime đẩy thông báo xuống MỌI trang admin đang mở (Giai đoạn 8 — báo đơn hàng mới).
/// Server chủ động push qua <c>IHubContext&lt;AdminNotifyHub&gt;</c> (xem CheckoutController) —
/// hub không có method phía server nào để client gọi lên, nên lớp này gần như rỗng.
///
/// Bảo vệ bằng [Authorize(Roles = Admin)]: chỉ client đã đăng nhập admin mới bắt tay được
/// WebSocket (cookie Identity tự đi kèm). Khách vãng lai không "nghe" được đơn của người khác.
///
/// ⚠️ Phải chỉ rõ <c>AuthenticationSchemes</c> (2026-07-28) — khu quản trị có cookie riêng.
/// Thiếu nó thì hub soi cookie của khách hàng và admin thật không kết nối được.
/// Hub nằm ở <c>/admin/hub/thong-bao</c> nên vẫn trong phạm vi đường dẫn của cookie quản trị.
/// </summary>
[Authorize(AuthenticationSchemes = AppAuthSchemes.Admin, Roles = AppRoles.Admin)]
public class AdminNotifyHub : Hub
{
    /// <summary>Tên group gom mọi kết nối admin — server nhắm vào group này khi push.</summary>
    public const string AdminGroup = "admins";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        await base.OnConnectedAsync();
    }
}
