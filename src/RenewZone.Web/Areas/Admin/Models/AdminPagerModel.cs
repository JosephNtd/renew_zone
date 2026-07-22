namespace RenewZone.Web.Areas.Admin.Models;

/// <summary>
/// Phân trang dùng chung cho mọi bảng admin.
/// <para>
/// <see cref="UrlFor"/> do từng màn cung cấp để link phân trang GIỮ NGUYÊN bộ lọc hiện tại —
/// bấm sang trang 2 mà mất bộ lọc là lỗi khó chịu điển hình của trang quản trị.
/// </para>
/// </summary>
public class AdminPagerModel
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalItems { get; init; }

    /// <summary>Sinh URL cho một số trang, kèm mọi tham số lọc đang có.</summary>
    public required Func<int, string> UrlFor { get; init; }

    public int TotalPages => TotalItems == 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public int FirstItem => TotalItems == 0 ? 0 : (Page - 1) * PageSize + 1;

    public int LastItem => Math.Min(Page * PageSize, TotalItems);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    /// <summary>
    /// Dải số trang hiển thị: luôn tối đa 5 nút quanh trang hiện tại.
    /// 137 trang mà render 137 nút thì thanh phân trang dài hơn cả bảng dữ liệu.
    /// </summary>
    public IEnumerable<int> PageNumbers()
    {
        const int window = 2;

        var start = Math.Max(1, Page - window);
        var end = Math.Min(TotalPages, Page + window);

        // Ở gần hai đầu thì kéo dài phía còn lại để luôn đủ 5 nút
        if (end - start < window * 2)
        {
            start = Math.Max(1, end - window * 2);
            end = Math.Min(TotalPages, start + window * 2);
        }

        for (var i = start; i <= end; i++)
        {
            yield return i;
        }
    }
}
