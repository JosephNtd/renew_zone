namespace RenewZone.Web.Models.ViewModels;

/// <summary>Dữ liệu cho partial _Pagination — phân trang số kiểu site gốc.</summary>
public class PaginationViewModel
{
    public required int Page { get; init; }
    public required int TotalPages { get; init; }

    /// <summary>Hàm dựng URL cho một số trang — mỗi trang liệt kê tự truyền cách build riêng.</summary>
    public required Func<int, string> Url { get; init; }

    /// <summary>Cửa sổ số trang hiển thị quanh trang hiện tại (tránh in ra 50 nút).</summary>
    public IEnumerable<int> Window(int radius = 2)
    {
        var from = Math.Max(1, Page - radius);
        var to = Math.Min(TotalPages, Page + radius);
        for (var i = from; i <= to; i++)
        {
            yield return i;
        }
    }
}
