using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RenewZone.Web.Hubs;

/// <summary>
/// Kênh realtime đẩy thông báo xuống trang của KHÁCH đang đăng nhập (Giai đoạn 9 —
/// báo khi đơn của họ đổi trạng thái). Đối xứng với <see cref="AdminNotifyHub"/>.
/// <para>
/// Khác biệt cốt lõi: admin nhận theo GROUP (mọi admin nghe chung một luồng), còn khách phải
/// nhận theo TỪNG NGƯỜI. Ở đây dùng thẳng <c>Clients.User(userId)</c> — SignalR tự gom mọi kết
/// nối của cùng một tài khoản (nhiều tab, điện thoại + máy tính) qua claim NameIdentifier, nên
/// không cần tự quản lý group và cũng không thể lỡ tay gửi đơn của người này cho người khác.
/// </para>
/// <para>
/// <c>[Authorize]</c> (không kèm role): khách vãng lai không bắt tay được WebSocket, còn admin
/// vừa quản trị vừa mua hàng thì vẫn nhận được thông báo đơn của chính họ.
/// </para>
/// </summary>
[Authorize]
public class CustomerNotifyHub : Hub
{
    // Không có method nào cho client gọi lên — luồng dữ liệu một chiều từ server xuống.
}
