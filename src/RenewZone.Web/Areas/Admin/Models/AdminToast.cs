namespace RenewZone.Web.Areas.Admin.Models;

/// <summary>
/// Thông báo hiện sau khi chuyển trang theo mô hình PRG. Đi qua TempData dưới dạng JSON
/// (TempData chỉ nhận kiểu nguyên thủy) rồi <c>_AdminLayout</c> đọc ra và gọi <c>RZAdmin.toast()</c>.
/// </summary>
public class AdminToast
{
    /// <summary>Khóa TempData — khai một chỗ để controller và layout không gõ lệch chuỗi.</summary>
    public const string TempDataKey = "rz_admin_toast";

    /// <summary>success | error | warning | info — khớp tên class CSS trong admin.css.</summary>
    public string Type { get; set; } = "success";

    public string Message { get; set; } = string.Empty;
}
