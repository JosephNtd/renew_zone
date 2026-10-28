namespace RenewZone.Domain.Models.Admin;

/// <summary>Bộ lọc màn danh sách sản phẩm.</summary>
public class ProductListRequest : AdminListRequest
{
    public int? CategoryId { get; init; }

    /// <summary>null = tất cả, true = đang bán, false = đang ẩn.</summary>
    public bool? IsActive { get; init; }

    /// <summary>true = chỉ SP CÓ ảnh, false = chỉ SP THIẾU ảnh (link từ khối "Cần chú ý" ở Dashboard).</summary>
    public bool? HasImage { get; init; }

    public bool? HasDescription { get; init; }
}

/// <param name="WishlistCount">Số tài khoản đang lưu sản phẩm vào Yêu thích.</param>
/// <param name="SoldCount">Số lượng đã bán = tổng số lượng trong các đơn đã Hoàn thành.</param>
public record ProductListItem(
    int Id,
    string Name,
    string Slug,
    string CategoryName,
    string? ImageUrl,
    decimal OriginalPrice,
    decimal? SalePrice,
    bool IsContactPrice,
    bool IsActive,
    bool IsFeatured,
    bool IsBestSeller,
    bool IsInstallment0Percent,
    int ViewCount,
    int ImageCount,
    int WishlistCount,
    int SoldCount);

/// <summary>Dữ liệu tạo/sửa sản phẩm (phần thông tin chung + nội dung).</summary>
public class ProductInput
{
    public int? Id { get; init; }
    public required int CategoryId { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public decimal OriginalPrice { get; init; }
    public decimal? SalePrice { get; init; }
    public bool IsContactPrice { get; init; }
    public string? ShortDescription { get; init; }
    public string? DescriptionHtml { get; init; }
    public string? SpecsHtml { get; init; }
    public bool IsInstallment0Percent { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsBestSeller { get; init; }
    public bool IsActive { get; init; }

    /// <summary>Toàn bộ nhóm biến thể — service tự đối chiếu để thêm/sửa/xóa cho khớp.</summary>
    public IReadOnlyList<OptionGroupInput> OptionGroups { get; init; } = [];
}

public class OptionGroupInput
{
    /// <summary>Null ⇒ nhóm mới thêm trên form.</summary>
    public int? Id { get; init; }

    /// <summary>Thuộc tính chọn từ danh sách chung (Dung lượng, Màu sắc…) — không còn gõ tay tên nhóm.</summary>
    public required int AttributeId { get; init; }
    public int DisplayOrder { get; init; }
    public IReadOnlyList<OptionValueInput> Values { get; init; } = [];
}

public class OptionValueInput
{
    public int? Id { get; init; }

    /// <summary>Giá trị chọn từ danh sách chung của thuộc tính (256GB, Titan Đen…).</summary>
    public required int AttributeValueId { get; init; }

    /// <summary>Cộng/trừ vào giá bán khi khách chọn — cho phép SỐ ÂM (bản 98% rẻ hơn bản 99%).</summary>
    public decimal PriceAdjustment { get; init; }

    public bool IsDefault { get; init; }
}

/// <summary>Ảnh sản phẩm ở màn quản trị.</summary>
public record ProductImageItem(int Id, string Url, int DisplayOrder, bool IsMain);
