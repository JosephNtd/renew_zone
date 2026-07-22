using RenewZone.Domain.Enums;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Web.Areas.Admin.Models;

/// <summary>
/// Màn danh sách đơn hàng. Toàn bộ bộ lọc nằm trên QUERYSTRING nên F5 giữ nguyên,
/// gửi link cho người khác được, và nút Back của trình duyệt hoạt động đúng.
/// </summary>
public class OrderListViewModel
{
    public required PagedResult<OrderListItem> Result { get; init; }
    public required IReadOnlyDictionary<OrderStatus, int> StatusCounts { get; init; }

    // ----- Giá trị đang lọc (đổ ngược lại vào form) -----
    public OrderStatus? Status { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public string? Search { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? SortBy { get; init; }
    public bool SortDesc { get; init; }

    public int TotalOrders => StatusCounts.Values.Sum();

    public bool HasAnyFilter =>
        Status is not null || PaymentMethod is not null
        || !string.IsNullOrWhiteSpace(Search) || FromDate is not null || ToDate is not null;

    /// <summary>
    /// Dựng URL giữ NGUYÊN mọi bộ lọc hiện tại, chỉ thay những thứ được truyền vào.
    /// Bấm sang trang 2 mà mất bộ lọc là lỗi khó chịu điển hình của trang quản trị.
    /// </summary>
    public string BuildUrl(
        int? page = null,
        OrderStatus? status = null, bool clearStatus = false,
        string? sortBy = null, bool? sortDesc = null)
    {
        var parts = new List<string>();

        var effectiveStatus = clearStatus ? null : status ?? Status;
        if (effectiveStatus is not null) { parts.Add($"trangThai={effectiveStatus}"); }

        if (PaymentMethod is not null) { parts.Add($"thanhToan={PaymentMethod}"); }

        if (!string.IsNullOrWhiteSpace(Search)) { parts.Add($"q={Uri.EscapeDataString(Search)}"); }

        if (FromDate is not null) { parts.Add($"tuNgay={FromDate:yyyy-MM-dd}"); }
        if (ToDate is not null) { parts.Add($"denNgay={ToDate:yyyy-MM-dd}"); }

        var effectiveSort = sortBy ?? SortBy;
        if (!string.IsNullOrWhiteSpace(effectiveSort))
        {
            parts.Add($"sort={effectiveSort}");
            if (sortDesc ?? SortDesc) { parts.Add("desc=true"); }
        }

        // Trang 1 không cần tham số — URL sạch hơn khi chia sẻ
        var effectivePage = page ?? Result.Page;
        if (effectivePage > 1) { parts.Add($"page={effectivePage}"); }

        return "/admin/don-hang" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
    }

    /// <summary>URL để bấm vào tiêu đề cột: cùng cột thì đảo chiều, khác cột thì sắp tăng dần.</summary>
    public string SortUrl(string column) =>
        BuildUrl(page: 1, sortBy: column, sortDesc: SortBy == column && !SortDesc);

    public bool IsSortedBy(string column) => SortBy == column;
}
