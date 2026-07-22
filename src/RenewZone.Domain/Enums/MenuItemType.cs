namespace RenewZone.Domain.Enums;

/// <summary>
/// Kiểu render của một mục menu. <see cref="ProductMega"/> là mục đặc biệt: nội dung dropdown
/// KHÔNG lấy từ bảng MenuItems mà sinh động từ cây <c>Category</c> (iPhone → iPhone 16 Series...),
/// nên admin thêm danh mục là menu tự có, không phải khai báo lại link.
/// </summary>
public enum MenuItemType
{
    Link = 0,
    ProductMega = 1
}
