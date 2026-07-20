using RenewZone.Domain.Entities;
using RenewZone.Domain.Models;

namespace RenewZone.Web.Models.ViewModels;

/// <summary>Danh sách sản phẩm phẳng có phân trang — dùng cho "/san-pham" (tất cả sản phẩm).</summary>
public class ProductListViewModel
{
    public required string Title { get; init; }
    public required string BasePath { get; init; }
    public required PagedResult<Product> Results { get; init; }

    public string PageUrl(int page) => $"{BasePath}?page={page}";
}
