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

/// <summary>Chi tiết đơn: thông tin khách + các dòng hàng (snapshot lúc đặt).</summary>
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
    IReadOnlyList<OrderDetailItem> Items)
{
    /// <summary>Các trạng thái được phép chuyển tiếp — view chỉ hiện đúng những lựa chọn này.</summary>
    public IReadOnlyList<OrderStatus> AllowedNext => OrderStatusRules.AllowedNext(Status);
}

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
public record ChangeStatusResult(
    bool Ok,
    string Message,
    OrderStatus Status,
    IReadOnlyList<OrderStatus> AllowedNext);
