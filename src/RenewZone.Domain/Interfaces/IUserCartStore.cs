namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Nơi cất giỏ hàng BỀN của một tài khoản (bảng <c>UserCarts</c>, Giai đoạn 9).
/// <para>
/// Cố ý làm việc với CHUỖI JSON chứ không với <c>Cart</c>: toàn bộ luật tính giá và gộp dòng
/// nằm ở <c>CartService</c> (tầng Web) — kho chứa chỉ có mỗi việc đọc/ghi, không được biết
/// bên trong giỏ có gì. Nhờ vậy giỏ trong Session và giỏ trong DB dùng CHUNG một định dạng
/// và CHUNG một đoạn logic, không thể lệch nhau.
/// </para>
/// <para>
/// Interface này tồn tại vì tầng Web KHÔNG được đụng <c>AppDbContext</c> (CLAUDE.md mục 4.9).
/// </para>
/// </summary>
public interface IUserCartStore
{
    /// <summary>Chuỗi JSON giỏ hàng đã lưu, hoặc null nếu tài khoản chưa có giỏ nào.</summary>
    Task<string?> LoadAsync(string userId);

    Task SaveAsync(string userId, string itemsJson);

    Task ClearAsync(string userId);
}
