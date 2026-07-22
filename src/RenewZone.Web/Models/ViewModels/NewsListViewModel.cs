using System.Text;
using RenewZone.Domain.Models;

namespace RenewZone.Web.Models.ViewModels;

/// <summary>Dữ liệu trang /tin-tuc: nội dung từ service + các hàm dựng URL giữ nguyên bộ lọc.</summary>
public class NewsListViewModel
{
    public required NewsListPage Data { get; init; }

    /// <summary>Tiêu đề + phụ đề đọc từ SiteSettings (đã áp fallback ở controller).</summary>
    public required string PageTitle { get; init; }
    public required string PageIntro { get; init; }

    /// <summary>URL đổi chuyên mục — GIỮ NGUYÊN từ khóa tìm kiếm, luôn về trang 1.</summary>
    public string CategoryUrl(string? categorySlug) => BuildUrl(categorySlug, Data.Keyword, 1);

    /// <summary>URL đổi trang — giữ nguyên cả chuyên mục lẫn từ khóa (partial _Pagination gọi).</summary>
    public string PageUrl(int page) => BuildUrl(Data.ActiveCategory?.Slug, Data.Keyword, page);

    private static string BuildUrl(string? categorySlug, string? keyword, int page)
    {
        var sb = new StringBuilder("/tin-tuc");
        var sep = '?';

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            sb.Append(sep).Append("chuyenMuc=").Append(Uri.EscapeDataString(categorySlug));
            sep = '&';
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            sb.Append(sep).Append("q=").Append(Uri.EscapeDataString(keyword));
            sep = '&';
        }

        if (page > 1)
        {
            sb.Append(sep).Append("page=").Append(page);
        }

        return sb.ToString();
    }
}
