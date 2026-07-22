using System.ComponentModel.DataAnnotations;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Web.Areas.Admin.Models;

/// <summary>
/// Form thêm/sửa danh mục. ViewModel RIÊNG chứ không bind thẳng entity —
/// entity có `IsDeleted`, `CreatedAt`, navigation… lọt vào form là lỗ hổng mass-assignment.
/// </summary>
public class CategoryFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(150, ErrorMessage = "Tên danh mục tối đa 150 ký tự.")]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập slug.")]
    [StringLength(200, ErrorMessage = "Slug tối đa 200 ký tự.")]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ gồm chữ thường không dấu, số và dấu gạch nối.")]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [Display(Name = "Danh mục cha")]
    public int? ParentId { get; set; }

    [StringLength(400)]
    [Display(Name = "Ảnh danh mục")]
    public string? ImageUrl { get; set; }

    [StringLength(60)]
    [Display(Name = "Icon")]
    public string? IconClass { get; set; }

    [Range(0, 9999, ErrorMessage = "Thứ tự phải từ 0 đến 9999.")]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hiển thị trên website")]
    public bool IsActive { get; set; } = true;

    /// <summary>Danh sách chọn cha — controller đổ vào, không phải dữ liệu người dùng nhập.</summary>
    public IReadOnlyList<Category> ParentOptions { get; set; } = [];

    public bool IsEdit => Id is not null;

    public CategoryInput ToInput() => new()
    {
        Id = Id,
        Name = Name,
        Slug = Slug,
        ParentId = ParentId,
        ImageUrl = ImageUrl,
        IconClass = IconClass,
        DisplayOrder = DisplayOrder,
        IsActive = IsActive
    };

    public static CategoryFormViewModel FromEntity(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Slug = category.Slug,
        ParentId = category.ParentId,
        ImageUrl = category.ImageUrl,
        IconClass = category.IconClass,
        DisplayOrder = category.DisplayOrder,
        IsActive = category.IsActive
    };
}
