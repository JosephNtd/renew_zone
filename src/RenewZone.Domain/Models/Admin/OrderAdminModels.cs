using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Models.Admin;

/// <summary>Bộ lọc màn danh sách đơn hàng.</summary>
public class OrderListRequest : AdminListRequest
{
    public OrderStatus? Status { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

/// <summary>Một dòng trong bảng đơn hàng — chỉ những cột thật sự hiển thị, không kéo cả entity.</summary>
public record OrderListItem(
    int Id,
    string Code,
    string CustomerName,
    string Phone,
    int ItemCount,
    decimal TotalAmount,
    PaymentMethod PaymentMethod,
    OrderStatus Status,
    DateTime CreatedAt);

/// <summary>Chi tiết đơn: thông tin khách + các dòng hàng (snapshot lúc đặt) + nhật ký trạng thái.</summary>
public record OrderDetail(
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
    DateTime? UpdatedAt,
    IReadOnlyList<OrderDetailItem> Items,
    IReadOnlyList<OrderStatusHistoryEntry> History)
{
    /// <summary>Các trạng thái được phép chuyển tiếp — view chỉ hiện đúng những lựa chọn này.</summary>
    public IReadOnlyList<OrderStatus> AllowedNext => OrderStatusRules.AllowedNext(Status);
}

/// <summary>
/// Một mốc trong nhật ký xử lý đơn (đọc từ <c>OrderStatusHistory</c>). <paramref name="FromStatus"/>
/// null ở mốc tạo đơn ("Khách đặt hàng"). Sắp theo <paramref name="ChangedAt"/> tăng dần để đọc
/// như dòng thời gian tự nhiên.
/// </summary>
public record OrderStatusHistoryEntry(
    OrderStatus? FromStatus,
    OrderStatus ToStatus,
    string ChangedBy,
    string? Note,
    DateTime ChangedAt);

/// <param name="ProductId">
/// Null khi sản phẩm đã bị xóa. Dòng hàng vẫn sống bằng snapshot tên/giá — đúng ý đồ
/// DeleteBehavior.SetNull từ Giai đoạn 1: xóa sản phẩm KHÔNG được làm mất lịch sử đơn.
/// </param>
public record OrderDetailItem(
    int Id,
    int? ProductId,
    string ProductName,
    string? ProductSlug,
    string? ImageUrl,
    string? VariantDescription,
    decimal UnitPrice,
    int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

/// <summary>Kết quả đổi trạng thái — trả về đủ thứ để JS vẽ lại badge + dropdown mà không reload.</summary>
/// <param name="UserId">
/// Chủ đơn (Giai đoạn 9) — controller cần để đẩy thông báo realtime xuống đúng khách hàng đó.
/// Null với đơn của khách vãng lai đặt trước Giai đoạn 9, và với mọi kết quả thất bại.
/// Trả kèm ở đây thay vì bắt controller truy vấn lại đơn chỉ để lấy một cột.
/// </param>
public record ChangeStatusResult(
    bool Ok,
    string Message,
    OrderStatus Status,
    IReadOnlyList<OrderStatus> AllowedNext,
    string? UserId = null);
