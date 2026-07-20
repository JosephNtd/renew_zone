using RenewZone.Domain.Entities;

namespace RenewZone.Domain.Models;

/// <summary>
/// Dữ liệu trang danh mục: danh mục đang xem, các tab series để lọc, và lưới sản phẩm đã phân trang.
/// </summary>
public class CategoryPage
{
    /// <summary>Danh mục CHA của trang (để breadcrumb + tiêu đề). Nếu vào thẳng series con thì là cha của nó.</summary>
    public Category Parent { get; init; } = null!;

    /// <summary>Các series con của Parent — render thành tab lọc. Rỗng nếu danh mục không có cấp con.</summary>
    public IReadOnlyList<Category> Series { get; init; } = [];

    /// <summary>Series đang được chọn (null ⇒ tab "Tất cả").</summary>
    public Category? ActiveSeries { get; init; }

    public PagedResult<Product> Products { get; init; } = new();
}
