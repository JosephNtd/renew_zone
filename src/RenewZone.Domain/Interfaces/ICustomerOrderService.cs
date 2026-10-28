using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Account;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Đơn hàng nhìn từ phía KHÁCH đã đăng nhập (Giai đoạn 9).
/// <para>
/// ⚠️ Bất biến của toàn bộ interface này: MỌI phương thức nhận <c>userId</c> và tự lọc theo nó.
/// Không có hàm nào nhận mỗi <c>orderId</c> rồi tin controller đã kiểm quyền — chỉ cần một chỗ
/// quên là khách đổi số trên URL đọc được đơn của người khác (IDOR). Sai userId ⇒ trả null/thất
/// bại y như đơn không tồn tại, không phân biệt "không có quyền" với "không tồn tại".
/// </para>
/// </summary>
public interface ICustomerOrderService
{
    Task<PagedResult<MyOrderListItem>> GetMyOrdersAsync(string userId, int page, int pageSize);

    /// <summary>Tra theo MÃ đơn (khách nhớ mã chứ không nhớ Id). Không phải đơn của họ ⇒ null.</summary>
    Task<MyOrderDetail?> GetMyOrderAsync(string userId, string code);

    /// <summary>
    /// Khách tự hủy đơn. Kiểm cả quyền sở hữu lẫn <c>OrderStatusRules.CustomerCanCancel</c>
    /// ở SERVER, và ghi một mốc vào sổ nhật ký trạng thái cùng một SaveChanges (Giai đoạn 7).
    /// </summary>
    Task<AccountOpResult> CancelMyOrderAsync(string userId, string code, string? reason);

    Task<AccountSummary> GetSummaryAsync(string userId);

    /// <summary>
    /// Tài khoản này đã MUA XONG sản phẩm đó chưa (đơn thuộc về họ + trạng thái Hoàn thành).
    /// Là điều kiện được gửi đánh giá từ Giai đoạn 9.
    /// </summary>
    Task<bool> HasPurchasedAsync(string userId, int productId);

    /// <summary>
    /// Dòng đơn hàng CŨ NHẤT của tài khoản (đơn Hoàn thành) chứa sản phẩm này mà chưa được đánh giá.
    /// Null ⇒ chưa mua, hoặc đã đánh giá hết các lần mua (Giai đoạn 10: mỗi lần mua một đánh giá).
    /// </summary>
    Task<int?> FindReviewableOrderItemAsync(string userId, int productId);

    /// <summary>Dòng đơn này có đúng là của tài khoản, đơn đã Hoàn thành, đúng sản phẩm và chưa đánh giá không.</summary>
    Task<bool> CanReviewOrderItemAsync(string userId, int orderItemId, int productId);

    /// <summary>Khách chấm sao việc giao hàng của đơn (đơn của họ, đã Hoàn thành, chưa chấm lần nào).</summary>
    Task<AccountOpResult> SubmitDeliveryReviewAsync(string userId, string code, DeliveryReviewRequest request);

    /// <summary>Các dòng hàng của một đơn để dựng lại giỏ ("Mua lại"). Không phải đơn của họ ⇒ rỗng.</summary>
    Task<IReadOnlyList<ReorderLine>> GetReorderLinesAsync(string userId, string code);
}

/// <summary>
/// Một dòng hàng cũ dùng để đổ lại vào giỏ. Chỉ mang ID — giá được TÍNH LẠI ở server theo bảng
/// giá hiện tại (Lưu ý 5c): mua lại đơn năm ngoái không được giữ giá năm ngoái.
/// </summary>
public record ReorderLine(int ProductId, IReadOnlyList<int> OptionValueIds, int Quantity);
