using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Giỏ hàng của một tài khoản, lưu bền trong DB để khách đổi máy/hết session vẫn còn giỏ
/// (Giai đoạn 9). Mỗi user có ĐÚNG một dòng — <c>UserId</c> là unique index.
/// <para>
/// <b>Vì sao lưu JSON nguyên khối thay vì bảng dòng-giỏ chuẩn hóa:</b> giỏ hàng đã là POCO
/// <c>Cart</c>/<c>CartItem</c> (Domain/Models) được serialize sẵn cho Session từ Giai đoạn 4.
/// Dùng lại chính chuỗi JSON đó ⇒ hai nơi lưu (session cho khách vãng lai, DB cho khách đã
/// đăng nhập) chạy qua CÙNG một logic tính giá, không có nhánh code thứ hai để lệch nhau.
/// Badge giỏ đọc ở MỌI trang nên phương án chuẩn hóa sẽ phải JOIN sản phẩm + biến thể mỗi
/// lần tải trang; ở đây chỉ là 1 SELECT theo khóa.
/// </para>
/// <para>
/// Giá trong JSON là snapshot — được tính lại ở server mỗi lần khách thêm hàng (đúng như
/// giỏ session xưa nay) và chốt lần cuối lúc tạo đơn, nên không có rủi ro khách trả giá cũ.
/// </para>
/// </summary>
public class UserCart : BaseEntity
{
    public string UserId { get; set; } = null!;

    /// <summary>Chuỗi JSON của <c>Domain.Models.Cart</c> (camelCase, y hệt bản lưu trong Session).</summary>
    public string ItemsJson { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}
