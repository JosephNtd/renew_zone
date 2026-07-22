using RenewZone.Domain.Common;
using RenewZone.Domain.Enums;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Một mục điều hướng (menu chính, offcanvas mobile, các cột link ở footer).
/// Thay toàn bộ link hardcode trong _Layout — admin sửa được không cần deploy (Giai đoạn 6).
/// Menu 2 cấp qua <see cref="ParentId"/> self-reference (Restrict: còn con thì không xóa được cha).
/// </summary>
public class MenuItem : BaseEntity
{
    public MenuGroup Group { get; set; }

    public MenuItemType Type { get; set; } = MenuItemType.Link;

    public string Text { get; set; } = null!;

    /// <summary>Đường dẫn tương đối (vd "/gioi-thieu"). Null khi Type = ProductMega không cần link riêng.</summary>
    public string? Url { get; set; }

    /// <summary>Class icon Bootstrap Icons, vd "bi-shield-check". Null ⇒ không hiện icon.</summary>
    public string? Icon { get; set; }

    public int? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool OpenInNewTab { get; set; }

    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = [];
}
