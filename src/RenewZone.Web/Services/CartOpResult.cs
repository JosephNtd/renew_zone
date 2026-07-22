using RenewZone.Domain.Models;

namespace RenewZone.Web.Services;

/// <summary>
/// Kết quả một thao tác lên giỏ hàng. Luôn kèm giỏ hiện tại để controller trả thẳng
/// số lượng + tổng tiền mới cho client (badge & tóm tắt cập nhật không cần reload).
/// </summary>
public class CartOpResult
{
    public bool Ok { get; init; }

    /// <summary>Thông điệp hiển thị cho khách (toast). Null ⇒ không cần báo gì.</summary>
    public string? Message { get; init; }

    public Cart Cart { get; init; } = new();

    /// <summary>Dòng vừa được tác động (thêm/sửa). Null khi thao tác xóa hoặc khi lỗi.</summary>
    public CartItem? Item { get; init; }

    public static CartOpResult Success(Cart cart, CartItem? item = null, string? message = null) =>
        new() { Ok = true, Cart = cart, Item = item, Message = message };

    public static CartOpResult Fail(Cart cart, string message) =>
        new() { Ok = false, Cart = cart, Message = message };
}
