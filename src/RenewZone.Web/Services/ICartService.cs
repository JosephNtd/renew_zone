using RenewZone.Domain.Models;

namespace RenewZone.Web.Services;

/// <summary>
/// Giỏ hàng (thuần tầng Web — không đụng DB trực tiếp; sản phẩm lấy qua
/// <see cref="RenewZone.Domain.Interfaces.ICatalogQueryService"/>, giỏ bền qua
/// <see cref="RenewZone.Domain.Interfaces.IUserCartStore"/>).
/// <para>
/// Từ Giai đoạn 9 giỏ có HAI nơi cất, chọn tự động theo trạng thái đăng nhập:
/// <list type="bullet">
///   <item>Đã đăng nhập ⇒ bảng <c>UserCarts</c>, nên đổi máy/đóng trình duyệt vẫn còn giỏ.</item>
///   <item>Chưa đăng nhập ⇒ Session. Theo yêu cầu hiện tại thì khách vãng lai KHÔNG thêm được
///   hàng (<c>CartController</c> chặn và bật hộp thoại đăng nhập), nên nhánh này trên thực tế
///   luôn rỗng — giữ lại làm lưới an toàn: nếu sau này chủ shop muốn cho khách vãng lai xếp
///   sẵn giỏ rồi mới đăng nhập ở bước cuối thì chỉ cần gỡ chốt chặn ở controller, phần lưu
///   trữ và gộp giỏ đã sẵn sàng.</item>
/// </list>
/// Mọi phương thức bên dưới không đổi chữ ký, nên controller/JS của Giai đoạn 4 chạy nguyên vẹn.
/// </para>
/// </summary>
public interface ICartService
{
    Task<Cart> GetCartAsync();

    /// <summary>
    /// Thêm sản phẩm vào giỏ. Giá và mô tả biến thể được TÍNH LẠI Ở SERVER từ
    /// <paramref name="optionValueIds"/>; client không gửi giá. Cùng bộ option ⇒ cộng dồn số lượng.
    /// </summary>
    Task<CartOpResult> AddAsync(int productId, IReadOnlyList<int> optionValueIds, int quantity);

    Task<CartOpResult> UpdateQuantityAsync(string key, int quantity);

    Task<CartOpResult> RemoveAsync(string key);

    Task ClearAsync();

    /// <summary>
    /// Chuyển giỏ khách vãng lai (Session) sang giỏ của tài khoản vừa đăng nhập, rồi xóa giỏ Session.
    /// <para>
    /// Nhận <paramref name="userId"/> tường minh chứ KHÔNG đọc <c>HttpContext.User</c>: hàm này
    /// chạy ngay sau <c>SignInAsync</c> trong cùng một request, mà việc principal của request hiện
    /// tại đã được cập nhật hay chưa là chi tiết nội bộ của Identity — phụ thuộc vào đó thì giỏ
    /// sẽ âm thầm được gộp vào… chính Session cũ.
    /// </para>
    /// </summary>
    Task MergeGuestCartAsync(string userId);
}
