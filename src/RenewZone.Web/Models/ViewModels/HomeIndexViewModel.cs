using RenewZone.Domain.Entities;
using RenewZone.Domain.Models;

namespace RenewZone.Web.Models.ViewModels;

/// <summary>
/// Dữ liệu render trang chủ. HomeController map từ HomeContent (tầng Domain) sang đây
/// để View không phụ thuộc trực tiếp vào model truy vấn của tầng dưới.
/// </summary>
public class HomeIndexViewModel
{
    public IReadOnlyList<Banner> HeroBanners { get; init; } = [];
    public IReadOnlyList<Banner> SideBanners { get; init; } = [];
    public IReadOnlyList<Category> Categories { get; init; } = [];
    public IReadOnlyList<CategorySection> CategorySections { get; init; } = [];
    public IReadOnlyList<Product> FeaturedProducts { get; init; } = [];
    public IReadOnlyList<Product> BestSellers { get; init; } = [];
    public IReadOnlyList<Testimonial> Testimonials { get; init; } = [];
    public IReadOnlyList<NewsArticle> LatestNews { get; init; } = [];
}
