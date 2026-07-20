using RenewZone.Domain.Entities;

namespace RenewZone.Domain.Models;

/// <summary>Dữ liệu trang chi tiết sản phẩm: sản phẩm (kèm ảnh + option) và sản phẩm liên quan.</summary>
public class ProductDetail
{
    public Product Product { get; init; } = null!;

    /// <summary>Danh mục cha (breadcrumb "Trang chủ > iPhone > ...").</summary>
    public Category? ParentCategory { get; init; }

    /// <summary>Sản phẩm cùng danh mục — carousel "Sản phẩm liên quan".</summary>
    public IReadOnlyList<Product> Related { get; init; } = [];
}
