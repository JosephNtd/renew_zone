namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Số đếm hiện trên sidebar/chuông thông báo — layout admin gọi mỗi request.
/// Tách riêng khỏi <c>IDashboardService</c> (vốn trả cả biểu đồ + danh sách) vì layout chỉ cần
/// 2 con số: nạp cả gói tổng quan cho mọi trang là lãng phí.
/// KHÔNG cache — admin vừa xử lý xong một đơn mà badge còn treo số cũ thì gây hoang mang.
/// </summary>
public interface IAdminBadgeService
{
    Task<AdminBadgeCounts> GetCountsAsync();
}

/// <param name="NewOrders">Đơn ở trạng thái New chờ xác nhận.</param>
/// <param name="UnreadContacts">Tin nhắn liên hệ chưa đọc.</param>
/// <param name="PendingReviews">Đánh giá sản phẩm đang chờ duyệt.</param>
public record AdminBadgeCounts(int NewOrders, int UnreadContacts, int PendingReviews);
