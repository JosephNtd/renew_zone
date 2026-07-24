using RenewZone.Domain.Enums;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Web.Areas.Admin.Models;

/// <summary>Dữ liệu màn "Đánh giá sản phẩm" của admin: danh sách + tab lọc theo trạng thái.</summary>
public class ReviewListViewModel
{
    public required PagedResult<ReviewListItem> Result { get; init; }
    public required ReviewStatusCounts Counts { get; init; }
    public ReviewStatus? Status { get; init; }
    public string? Search { get; init; }

    public bool HasAnyFilter => Status is not null || !string.IsNullOrWhiteSpace(Search);

    public string BuildUrl(ReviewStatus? status = null, int? page = null, bool keepStatus = true)
    {
        var parts = new List<string>();

        var effectiveStatus = keepStatus ? (status ?? Status) : status;
        if (effectiveStatus is not null) { parts.Add($"trangThai={effectiveStatus}"); }
        if (!string.IsNullOrWhiteSpace(Search)) { parts.Add($"q={Uri.EscapeDataString(Search)}"); }

        var effectivePage = page ?? Result.Page;
        if (effectivePage > 1) { parts.Add($"page={effectivePage}"); }

        return "/admin/danh-gia-san-pham" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
    }
}
