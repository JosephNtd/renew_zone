using System.ComponentModel.DataAnnotations;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Web.Areas.Admin.Models;

/// <summary>
/// Form sản phẩm 5 tab. ViewModel riêng — entity có `IsDeleted`, `ViewCount`, `CreatedAt`,
/// navigation… không được phép lọt vào form (mass-assignment).
/// </summary>
public class ProductFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(250, ErrorMessage = "Tên sản phẩm tối đa 250 ký tự.")]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập slug.")]
    [StringLength(250)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ gồm chữ thường không dấu, số và dấu gạch nối.")]
    public string Slug { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Range(0, 999_999_999, ErrorMessage = "Giá gốc không hợp lệ.")]
    [Display(Name = "Giá gốc")]
    public decimal OriginalPrice { get; set; }

    [Range(0, 999_999_999, ErrorMessage = "Giá khuyến mãi không hợp lệ.")]
    [Display(Name = "Giá khuyến mãi")]
    public decimal? SalePrice { get; set; }

    [Display(Name = "Giá liên hệ")]
    public bool IsContactPrice { get; set; }

    [StringLength(500, ErrorMessage = "Mô tả ngắn tối đa 500 ký tự.")]
    [Display(Name = "Mô tả ngắn")]
    public string? ShortDescription { get; set; }

    [Display(Name = "Mô tả chi tiết")]
    public string? DescriptionHtml { get; set; }

    [Display(Name = "Thông số kỹ thuật")]
    public string? SpecsHtml { get; set; }

    [Display(Name = "Trả góp 0%")]
    public bool IsInstallment0Percent { get; set; } = true;

    [Display(Name = "Ưu đãi đặc biệt")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Bán chạy")]
    public bool IsBestSeller { get; set; }

    [Display(Name = "Đang bán")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Nhóm biến thể. Model binder dừng ở chỗ ĐỨT index nên JS repeater phải đánh lại số
    /// liên tục sau mỗi lần thêm/xóa hàng, nếu không các dòng phía sau biến mất âm thầm.
    /// </summary>
    public List<OptionGroupViewModel> OptionGroups { get; set; } = [];

    // ----- Dữ liệu phụ trợ do controller đổ vào (không phải input của người dùng) -----
    public IReadOnlyList<CategoryOption> CategoryOptions { get; set; } = [];
    public IReadOnlyList<ProductImageItem> Images { get; set; } = [];

    public bool IsEdit => Id is not null;

    public ProductInput ToInput() => new()
    {
        Id = Id,
        CategoryId = CategoryId,
        Name = Name,
        Slug = Slug,
        OriginalPrice = OriginalPrice,
        // Sản phẩm "Liên hệ" không công khai giá ⇒ bỏ luôn giá KM cho khỏi lệch dữ liệu
        SalePrice = IsContactPrice ? null : SalePrice,
        IsContactPrice = IsContactPrice,
        ShortDescription = ShortDescription,
        DescriptionHtml = DescriptionHtml,
        SpecsHtml = SpecsHtml,
        IsInstallment0Percent = IsInstallment0Percent,
        IsFeatured = IsFeatured,
        IsBestSeller = IsBestSeller,
        IsActive = IsActive,
        OptionGroups = [.. OptionGroups
            .Where(g => !string.IsNullOrWhiteSpace(g.Name))
            .Select(g => new OptionGroupInput
            {
                Id = g.Id,
                Name = g.Name,
                Values = [.. g.Values
                    .Where(v => !string.IsNullOrWhiteSpace(v.Value))
                    .Select(v => new OptionValueInput
                    {
                        Id = v.Id,
                        Value = v.Value,
                        PriceAdjustment = v.PriceAdjustment,
                        IsDefault = v.IsDefault
                    })]
            })]
    };

    public static ProductFormViewModel FromEntity(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Slug = product.Slug,
        CategoryId = product.CategoryId,
        OriginalPrice = product.OriginalPrice,
        SalePrice = product.SalePrice,
        IsContactPrice = product.IsContactPrice,
        ShortDescription = product.ShortDescription,
        DescriptionHtml = product.DescriptionHtml,
        SpecsHtml = product.SpecsHtml,
        IsInstallment0Percent = product.IsInstallment0Percent,
        IsFeatured = product.IsFeatured,
        IsBestSeller = product.IsBestSeller,
        IsActive = product.IsActive,
        OptionGroups = [.. product.OptionGroups
            .OrderBy(g => g.DisplayOrder)
            .Select(g => new OptionGroupViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Values = [.. g.Values.Select(v => new OptionValueViewModel
                {
                    Id = v.Id,
                    Value = v.Value,
                    PriceAdjustment = v.PriceAdjustment,
                    IsDefault = v.IsDefault
                })]
            })],
        Images = [.. product.Images
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new ProductImageItem(i.Id, i.Url, i.DisplayOrder, i.IsMain))]
    };
}

public class OptionGroupViewModel
{
    public int? Id { get; set; }

    [StringLength(100, ErrorMessage = "Tên nhóm tối đa 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    public List<OptionValueViewModel> Values { get; set; } = [];
}

public class OptionValueViewModel
{
    public int? Id { get; set; }

    [StringLength(100, ErrorMessage = "Giá trị tối đa 100 ký tự.")]
    public string Value { get; set; } = string.Empty;

    /// <summary>Cho phép SỐ ÂM: bản 98% thường rẻ hơn giá gốc.</summary>
    [Range(-999_999_999, 999_999_999, ErrorMessage = "Chênh lệch giá không hợp lệ.")]
    public decimal PriceAdjustment { get; set; }

    public bool IsDefault { get; set; }
}

/// <param name="Depth">0 = danh mục cha, 1 = dòng con — view thụt lề theo giá trị này.</param>
public record CategoryOption(int Id, string Name, int Depth);
