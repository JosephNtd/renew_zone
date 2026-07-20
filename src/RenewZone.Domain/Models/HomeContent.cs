using RenewZone.Domain.Entities;

namespace RenewZone.Domain.Models;

/// <summary>
/// Gói dữ liệu động của trang chủ (banner, danh mục, các nhóm sản phẩm, testimonial, tin mới).
/// Model thuần — không phụ thuộc EF; do IHomeQueryService dựng và cache.
/// </summary>
public class HomeContent
{
    /// <summary>Banner slider lớn đầu trang (BannerPosition.HomeSlider).</summary>
    public IReadOnlyList<Banner> HeroBanners { get; init; } = [];

    /// <summary>Banner phụ xếp dọc bên phải hero (BannerPosition.HomeMiddle).</summary>
    public IReadOnlyList<Banner> SideBanners { get; init; } = [];

    /// <summary>Danh mục cha đang active — khối 6 tile danh mục.</summary>
    public IReadOnlyList<Category> Categories { get; init; } = [];

    /// <summary>Mỗi danh mục cha có sản phẩm kèm tối đa 8 sản phẩm của nó (hoặc series con).</summary>
    public IReadOnlyList<CategorySection> CategorySections { get; init; } = [];

    /// <summary>Sản phẩm nổi bật — khối "Ưu đãi đặc biệt".</summary>
    public IReadOnlyList<Product> FeaturedProducts { get; init; } = [];

    /// <summary>Sản phẩm bán chạy — carousel.</summary>
    public IReadOnlyList<Product> BestSellers { get; init; } = [];

    public IReadOnlyList<Testimonial> Testimonials { get; init; } = [];

    /// <summary>Bài viết mới nhất — khối "Tin công nghệ".</summary>
    public IReadOnlyList<NewsArticle> LatestNews { get; init; } = [];
}

/// <summary>Một danh mục cha kèm danh sách sản phẩm hiển thị dưới nó ở trang chủ.</summary>
public class CategorySection
{
    public Category Category { get; init; } = null!;
    public IReadOnlyList<Product> Products { get; init; } = [];
}
