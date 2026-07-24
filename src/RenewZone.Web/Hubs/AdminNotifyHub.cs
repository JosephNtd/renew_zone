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
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
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
