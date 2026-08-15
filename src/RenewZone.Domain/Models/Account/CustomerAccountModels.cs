using RenewZone.Domain.Enums;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Domain.Models.Account;

/// <summary>Một dòng trong "Đơn hàng của tôi". Ảnh sản phẩm đầu tiên để khách nhận ra đơn bằng mắt.</summary>
public record MyOrderListItem(
    int Id,
    string Code,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    decimal TotalAmount,
    int ItemCount,
    string? FirstItemName,
    string? FirstImageUrl,
    DateTime CreatedAt)
{
    /// <summary>Khách chỉ được tự hủy khi shop chưa giao — xem <see cref="OrderStatusRules"/>.</summary>
    public bool CanCancel => OrderStatusRules.CustomerCanCancel(Status);
}

/// <summary>
/// Chi tiết một đơn của CHÍNH khách đang đăng nhập. Khác <see cref="OrderDetail"/> của admin ở
/// hai điểm cố ý: không có <c>AllowedNext</c> (khách không được đổi trạng thái tùy ý, chỉ hủy),
/// và nhật ký đã che tên quản trị viên (xem <see cref="MyOrderHistoryEntry"/>).
/// </summary>
public record MyOrderDetail(
    int Id,
    string Code,
    string CustomerName,
    string Phone,
    string? Email,
    string Address,
    string? Note,
    PaymentMethod PaymentMethod,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    IReadOnlyList<OrderDetailItem> Items,
    IReadOnlyList<MyOrderHistoryEntry> History)
{
    public bool CanCancel => OrderStatusRules.CustomerCanCancel(Status);
}

/// <summary>
/// Một mốc xử lý đơn hiển thị cho KHÁCH.
/// <para>
/// ⚠️ Cố ý KHÔNG mang <c>ChangedBy</c> thô: cột đó chứa tên đăng nhập của quản trị viên
/// (vd "admin") — không có lý do gì để khách hàng biết tài khoản nội bộ của shop.
/// Service quy về "Bạn" (khách tự thao tác) hoặc "Cửa hàng".
/// </para>
/// </summary>
public record MyOrderHistoryEntry(
    OrderStatus? FromStatus,
    OrderStatus ToStatus,
    string ActorLabel,
    string? Note,
    DateTime ChangedAt);

/// <summary>Số liệu tóm tắt ở trang tổng quan tài khoản — mỗi ô là một con số khách quan tâm.</summary>
public record AccountSummary(
    int TotalOrders,
    int ActiveOrders,
    int CompletedOrders,
    decimal TotalSpent,
    int AddressCount,
    int WishlistCount,
    IReadOnlyList<MyOrderListItem> RecentOrders);

/// <summary>Dữ liệu form thêm/sửa một địa chỉ trong sổ địa chỉ.</summary>
public class AddressInput
{
    public int Id { get; set; }
    public string? Label { get; set; }
    public string ReceiverName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string AddressLine { get; set; } = null!;
    public bool IsDefault { get; set; }
}

/// <summary>Một mục trong danh sách yêu thích — đủ dữ liệu để dựng card sản phẩm.</summary>
public record WishlistEntry(
    int ProductId,
    string Name,
    string Slug,
    string? ImageUrl,
    decimal OriginalPrice,
    decimal? SalePrice,
    bool IsContactPrice,
    decimal AvgRating,
    int ReviewCount,
    bool IsAvailable,
    DateTime AddedAt);

/// <summary>
/// Kết quả một thao tác của khách (hủy đơn, lưu địa chỉ, bật/tắt tim). Trả object thay vì bool
/// để controller có sẵn câu tiếng Việt hiển thị, không phải tự chế thông báo ở nhiều nơi.
/// </summary>
public record AccountOpResult(bool Ok, string Message)
{
    public static AccountOpResult Success(string message) => new(true, message);
    public static AccountOpResult Fail(string message) => new(false, message);
}
