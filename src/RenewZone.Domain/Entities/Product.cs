using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Sản phẩm. Gắn vào danh mục cấp CON (series) nếu có, không thì gắn danh mục cha.
/// Giá cuối cùng khi mua = DisplayPrice + tổng PriceAdjustment của các option khách chọn.
/// </summary>
public class Product : AuditableEntity, ISoftDeletable
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    /// <summary>Giá niêm yết — hiển thị gạch ngang khi có SalePrice.</summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>Giá khuyến mãi (đỏ, nổi bật). Null ⇒ bán đúng giá niêm yết.</summary>
    public decimal? SalePrice { get; set; }

    /// <summary>true ⇒ card hiện "Liên hệ", KHÔNG cho thêm giỏ hàng (Lưu ý 6.5).</summary>
    public bool IsContactPrice { get; set; }

    public string? ShortDescription { get; set; }

    /// <summary>Mô tả dài (rich text) — tab "Mô tả" trang chi tiết.</summary>
    public string? DescriptionHtml { get; set; }

    /// <summary>Thông số kỹ thuật (camera/màn hình, pin/sạc, thiết kế...) — tab "Thông số".</summary>
    public string? SpecsHtml { get; set; }

    /// <summary>Badge "Trả góp 0%" trên card.</summary>
    public bool IsInstallment0Percent { get; set; } = true;

    /// <summary>Xuất hiện ở khối "Ưu đãi đặc biệt" trang chủ.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Xuất hiện ở carousel "Sản phẩm bán chạy" trang chủ.</summary>
    public bool IsBestSeller { get; set; }

    public bool IsActive { get; set; } = true;
    public int ViewCount { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    /// <summary>Giá đang bán thực tế (chưa cộng option) — dùng thống nhất mọi nơi hiển thị.</summary>
    public decimal DisplayPrice => SalePrice ?? OriginalPrice;

    public bool HasDiscount => SalePrice.HasValue && SalePrice.Value < OriginalPrice;

    public Category Category { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductOptionGroup> OptionGroups { get; set; } = [];
}
