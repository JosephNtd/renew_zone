using System.ComponentModel.DataAnnotations;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Web.Areas.Admin.Models;

/// <summary>Form thêm/sửa thuộc tính ở màn Thuộc tính sản phẩm.</summary>
public class AttributeFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thuộc tính.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 9999)]
    public int DisplayOrder { get; set; }

    public AttributeInput ToInput() => new() { Id = Id, Name = Name, DisplayOrder = DisplayOrder };
}

/// <summary>Form thêm/sửa một giá trị của thuộc tính.</summary>
public class AttributeValueFormViewModel
{
    public int? Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thuộc tính.")]
    public int AttributeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá trị.")]
    [StringLength(100)]
    public string Value { get; set; } = string.Empty;

    [Range(0, 9999)]
    public int DisplayOrder { get; set; }

    public AttributeValueInput ToInput() => new()
    {
        Id = Id, AttributeId = AttributeId, Value = Value, DisplayOrder = DisplayOrder
    };
}
