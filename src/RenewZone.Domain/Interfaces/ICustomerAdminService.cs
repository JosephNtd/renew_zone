using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Quản lý tài khoản khách hàng từ trang quản trị (Giai đoạn 9).
/// <para>
/// Phạm vi CỐ Ý hẹp: xem, khóa/mở khóa, đặt lại mật khẩu. Admin KHÔNG sửa hồ sơ, KHÔNG sửa sổ
/// địa chỉ và KHÔNG xóa tài khoản hộ khách — đó là dữ liệu riêng của người dùng, và xóa tài khoản
/// sẽ kéo theo <c>Order.UserId</c> về null (đơn mất chủ, không tra ngược được nữa).
/// Muốn ngăn một người tiếp tục mua thì KHÓA, đó mới là thao tác đảo ngược được.
/// </para>
/// </summary>
public interface ICustomerAdminService
{
    Task<PagedResult<CustomerListItem>> GetListAsync(CustomerListRequest request);

    Task<CustomerDetail?> GetDetailAsync(string userId);

    /// <summary>Khóa vô thời hạn / mở khóa. Không cho khóa chính tài khoản quản trị.</summary>
    Task<CustomerOpResult> SetLockedAsync(string userId, bool locked);

    /// <summary>
    /// Đặt lại mật khẩu — thay cho luồng "quên mật khẩu" qua email mà site chưa có
    /// (quyết định 2026-07-28). Sinh mật khẩu tạm ngẫu nhiên và trả về ĐÚNG MỘT LẦN
    /// để admin đọc cho khách; hệ thống không lưu lại bản rõ ở bất kỳ đâu.
    /// </summary>
    Task<CustomerOpResult> ResetPasswordAsync(string userId);

    /// <summary>Số tài khoản khách — cho ô thống kê ở Dashboard.</summary>
    Task<int> CountAsync();
}
