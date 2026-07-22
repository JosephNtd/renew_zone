namespace RenewZone.Domain.Models.Admin;

/// <summary>
/// Tham số chung của MỌI màn danh sách admin (tìm/phân trang/sắp xếp).
/// <para>
/// Chỉ phần này được dùng chung — đã cân nhắc và LOẠI BỎ phương án
/// <c>IAdminCrudService&lt;TEntity&gt;</c> generic: mỗi aggregate có nghiệp vụ riêng thật sự
/// (sản phẩm có biến thể + ảnh, danh mục có ràng buộc cha-con, đơn hàng có luồng trạng thái),
/// nhét vào generic sẽ phải "thoát hiểm" liên tục bằng override — nhiều code hơn và khó đọc hơn
/// là viết tường minh từng service.
/// </para>
/// </summary>
public class AdminListRequest
{
    private const int MaxPageSize = 100;

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? SortBy { get; init; }

    public bool SortDesc { get; init; }

    /// <summary>Trang &lt; 1 (do sửa querystring tay) ⇒ ép về 1 thay vì trả trang rỗng khó hiểu.</summary>
    public int SafePage => Page < 1 ? 1 : Page;

    /// <summary>Chặn <c>?pageSize=100000</c> kéo sập server.</summary>
    public int SafePageSize => PageSize is < 1 or > MaxPageSize ? 20 : PageSize;

    public int Skip => (SafePage - 1) * SafePageSize;

    public string? NormalizedSearch =>
        string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
}
