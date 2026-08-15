namespace RenewZone.Infrastructure.Identity;

/// <summary>
/// Tên các scheme xác thực + tên cookie, gom một chỗ để không gõ chuỗi rời rạc (giống <see cref="AppRoles"/>).
/// <para>
/// <b>Vì sao TÁCH hai phiên</b> (quyết định 2026-07-28): quản trị và mua hàng là hai vùng tin cậy
/// khác nhau. Dùng chung một cookie thì chỉ đặt được MỘT thời hạn cho cả hai — hoặc phiên admin
/// dài quá mức an toàn, hoặc bắt khách đăng nhập lại liên tục. Tách ra còn cho phép cookie quản
/// trị giới hạn theo đường dẫn <c>/admin</c>: trình duyệt KHÔNG gửi nó cho bất kỳ trang khách nào,
/// nên một lỗ hổng phía web khách cũng không chạm tới được phiên quản trị.
/// </para>
/// <para>
/// Scheme của KHÁCH giữ nguyên là scheme MẶC ĐỊNH của Identity — nhờ vậy toàn bộ web khách
/// (<c>_Layout</c>, giỏ hàng, nút tim, hub realtime, anti-forgery) đọc <c>User</c> như cũ,
/// không phải sửa gì. Chỉ khu admin mới khai báo scheme riêng.
/// </para>
/// </summary>
public static class AppAuthSchemes
{
    /// <summary>Scheme cookie riêng cho khu quản trị.</summary>
    public const string Admin = "RZ.Admin";

    /// <summary>Tên cookie của phiên quản trị. Đặt tên rõ ràng để dễ nhận ra khi soi DevTools.</summary>
    public const string AdminCookie = ".RZ.Admin";

    /// <summary>Tên cookie của phiên khách hàng (scheme mặc định của Identity).</summary>
    public const string CustomerCookie = ".RZ.Customer";

    /// <summary>
    /// Đường dẫn cookie quản trị. Trình duyệt chỉ gửi cookie cho URL nằm dưới đây — mọi endpoint
    /// của khu quản trị (kể cả hub SignalR <c>/admin/hub/thong-bao</c>) đều thỏa điều kiện này.
    /// ⚠️ Thêm endpoint quản trị NGOÀI <c>/admin</c> thì nó sẽ không nhận được cookie.
    /// </summary>
    public const string AdminCookiePath = "/admin";
}
