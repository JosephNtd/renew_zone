using RenewZone.Domain.Entities;
using RenewZone.Domain.Models;

namespace RenewZone.Web.Models.ViewModels;

/// <summary>
/// Bọc <see cref="CategoryPage"/> và tự dựng URL cho tab series + phân trang.
/// Mọi link đều quy về slug của danh mục CHA để URL nhất quán dù khách vào bằng slug con.
/// </summary>
public class CategoryViewModel
{
    public required CategoryPage Data { get; init; }

    private string ParentSlug => Data.Parent.Slug;

    /// <summary>URL cho một tab series (null ⇒ tab "Tất cả").</summary>
    public string SeriesUrl(Category? series) =>
        series is null ? $"/{ParentSlug}" : $"/{ParentSlug}?series={series.Slug}";

    /// <summary>URL tới trang số <paramref name="page"/>, giữ nguyên series đang lọc.</summary>
    public string PageUrl(int page)
    {
        var series = Data.ActiveSeries?.Slug;
        return series is null
            ? $"/{ParentSlug}?page={page}"
            : $"/{ParentSlug}?series={series}&page={page}";
    }

    public bool IsActiveSeries(Category? series) => Data.ActiveSeries?.Id == series?.Id;
}
