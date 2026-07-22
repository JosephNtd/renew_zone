using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Web.Areas.Admin.Models;

/// <summary>Màn danh sách sản phẩm. Bộ lọc nằm trên querystring (F5/back/chia sẻ link đều đúng).</summary>
public class ProductListViewModel
{
    public required PagedResult<ProductListItem> Result { get; init; }
    public required IReadOnlyList<CategoryOption> CategoryOptions { get; init; }

    public int? CategoryId { get; init; }
    public bool? IsActive { get; init; }
    public bool? HasImage { get; init; }
    public bool? HasDescription { get; init; }
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public bool SortDesc { get; init; }

    public bool HasAnyFilter =>
        CategoryId is not null || IsActive is not null || HasImage is not null
        || HasDescription is not null || !string.IsNullOrWhiteSpace(Search);

    public string BuildUrl(int? page = null, string? sortBy = null, bool? sortDesc = null)
    {
        var parts = new List<string>();

        if (CategoryId is not null) { parts.Add($"danhMuc={CategoryId}"); }
        if (IsActive is not null) { parts.Add($"trangThai={(IsActive.Value ? "hien" : "an")}"); }
        if (HasImage is not null) { parts.Add($"coAnh={(HasImage.Value ? "co" : "khong")}"); }
        if (HasDescription is not null) { parts.Add($"coMoTa={(HasDescription.Value ? "co" : "khong")}"); }
        if (!string.IsNullOrWhiteSpace(Search)) { parts.Add($"q={Uri.EscapeDataString(Search)}"); }

        var effectiveSort = sortBy ?? SortBy;
        if (!string.IsNullOrWhiteSpace(effectiveSort))
        {
            parts.Add($"sort={effectiveSort}");
            if (sortDesc ?? SortDesc) { parts.Add("desc=true"); }
        }

        var effectivePage = page ?? Result.Page;
        if (effectivePage > 1) { parts.Add($"page={effectivePage}"); }

        return "/admin/san-pham" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
    }

    public string SortUrl(string column) =>
        BuildUrl(page: 1, sortBy: column, sortDesc: SortBy == column && !SortDesc);

    public bool IsSortedBy(string column) => SortBy == column;
}
