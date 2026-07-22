namespace RenewZone.Domain.Common;

/// <summary>
/// Entity NỘI DUNG xóa được vào "Thùng rác" thay vì mất hẳn (Giai đoạn 6).
/// <para>
/// Cách hoạt động — 3 mảnh, thiếu mảnh nào là hở:
/// <list type="number">
///   <item><c>AppDbContext.ApplySoftDelete()</c> biến mọi <c>Remove()</c> thành UPDATE
///   ⇒ code nghiệp vụ cứ gọi Remove như bình thường, không ai lỡ tay xóa cứng được.</item>
///   <item>Global query filter trong <c>OnModelCreating</c> tự loại bản ghi đã xóa khỏi MỌI query
///   ⇒ web khách và danh sách admin không cần thêm điều kiện nào.</item>
///   <item>Màn Thùng rác dùng <c>IgnoreQueryFilters()</c> để nhìn thấy chúng; xóa vĩnh viễn
///   dùng <c>ExecuteDeleteAsync()</c> (đi vòng qua change tracker nên không bị mảnh 1 chặn).</item>
/// </list>
/// </para>
/// ⚠️ Entity GIAO DỊCH (Order/OrderItem) KHÔNG dùng interface này — sổ sách không được biến mất;
/// muốn hủy đơn thì đổi <c>Status = Cancelled</c>.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTime? DeletedAt { get; set; }
}
