namespace RenewZone.Domain.Models.Admin;

/// <summary>Một nút trong cây danh mục 2 cấp ở màn quản trị.</summary>
public record CategoryTreeNode(
    int Id,
    string Name,
    string Slug,
    string? ImageUrl,
    string? IconClass,
    int DisplayOrder,
    bool IsActive,
    int ProductCount,
    IReadOnlyList<CategoryTreeNode> Children)
{
    /// <summary>Tổng sản phẩm tính cả danh mục con — cha thường không gắn sản phẩm trực tiếp.</summary>
    public int TotalProductCount => ProductCount + Children.Sum(c => c.TotalProductCount);
}

/// <summary>Dữ liệu tạo/sửa danh mục. Map tay từ ViewModel — KHÔNG bind thẳng entity.</summary>
public class CategoryInput
{
    public int? Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public int? ParentId { get; init; }
    public string? ImageUrl { get; init; }
    public string? IconClass { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Kết quả một thao tác ghi. Không dùng exception cho lỗi NGHIỆP VỤ (slug trùng, còn sản phẩm)
/// — đó là tình huống bình thường cần báo cho người dùng, không phải sự cố hệ thống.
/// </summary>
public record AdminResult(bool Ok, string Message, int? Id = null)
{
    public static AdminResult Success(string message, int? id = null) => new(true, message, id);
    public static AdminResult Fail(string message) => new(false, message);
}
