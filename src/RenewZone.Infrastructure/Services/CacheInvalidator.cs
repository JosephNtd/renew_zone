using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;

namespace RenewZone.Infrastructure.Services;

/// <summary>
/// Bảng tra "loại entity → vùng cache" + thực thi xóa cache. Xem <see cref="ICacheInvalidator"/>.
/// <para>
/// <b>Cắt vòng phụ thuộc DI:</b> chuỗi tự nhiên là
/// <c>AppDbContext → ICacheInvalidator → SiteContentService → AppDbContext</c> ⇒ app chết ngay lúc
/// khởi động với <c>A circular dependency was detected</c>. Cắt bằng cách nhận
/// <see cref="IServiceScopeFactory"/> thay vì nhận thẳng các service: scope chỉ được tạo LÚC GỌI,
/// không phải lúc dựng đối tượng, nên vòng lặp không hình thành. DbContext trong scope tạm đó
/// không bao giờ mở kết nối (mọi <c>Invalidate()</c> chỉ đụng IMemoryCache singleton).
/// </para>
/// </summary>
public class CacheInvalidator(IServiceScopeFactory scopeFactory, ILogger<CacheInvalidator> logger)
    : ICacheInvalidator
{
    /// <summary>
    /// BẢNG PHỤ THUỘC CACHE — nguồn sự thật duy nhất, khớp với CLAUDE.md mục 4.9.
    /// Entity không có trong bảng ⇒ không vùng cache nào phụ thuộc (Order, OrderItem, ContactMessage).
    /// </summary>
    private static readonly Dictionary<Type, CacheScope> Map = new()
    {
        // Danh mục: menu dropdown layout + 6 tile trang chủ + bản đồ slug
        [typeof(Category)] = CacheScope.SiteContent | CacheScope.Home | CacheScope.Slugs,

        // Sản phẩm và các bảng con: section trang chủ + bản đồ slug
        [typeof(Product)] = CacheScope.Home | CacheScope.Slugs,
        [typeof(ProductImage)] = CacheScope.Home,
        [typeof(ProductOptionGroup)] = CacheScope.Home,
        [typeof(ProductOptionValue)] = CacheScope.Home,

        // Đánh giá đổi ⇒ điểm sao trên card (trang chủ) có thể đổi. Thực tế phần lớn thao tác
        // duyệt/xóa còn ghi thẳng Product.AvgRating (đã tự xóa Home|Slugs), đây là lớp phòng thủ.
        [typeof(ProductReview)] = CacheScope.Home,

        // Tin tức: khối tin trang chủ + bản đồ slug
        [typeof(NewsArticle)] = CacheScope.Home | CacheScope.Slugs,
        [typeof(NewsCategory)] = CacheScope.Home,

        // Trang tĩnh chỉ ảnh hưởng bản đồ slug
        [typeof(StaticPage)] = CacheScope.Slugs,

        // Hero slider + khối đánh giá trang chủ
        [typeof(Banner)] = CacheScope.Home,
        [typeof(Testimonial)] = CacheScope.Home,

        // Điều hướng & khối nội dung
        [typeof(MenuItem)] = CacheScope.Navigation,
        [typeof(ContentBlock)] = CacheScope.ContentBlocks,

        // Settings đọc ra topbar/header/footer/bank/tin/liên hệ
        [typeof(SiteSetting)] = CacheScope.SiteContent
    };

    public void OnEntitiesChanged(IEnumerable<Type> changedEntityTypes)
    {
        var scopes = CacheScope.None;

        foreach (var type in changedEntityTypes)
        {
            if (Map.TryGetValue(type, out var scope))
            {
                scopes |= scope;
            }
        }

        Invalidate(scopes);
    }

    public void Invalidate(CacheScope scopes)
    {
        if (scopes == CacheScope.None)
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            if (scopes.HasFlag(CacheScope.SiteContent))
            {
                sp.GetRequiredService<ISiteContentService>().Invalidate();
            }

            if (scopes.HasFlag(CacheScope.Navigation))
            {
                sp.GetRequiredService<INavigationService>().Invalidate();
            }

            if (scopes.HasFlag(CacheScope.ContentBlocks))
            {
                sp.GetRequiredService<IContentBlockService>().Invalidate();
            }

            if (scopes.HasFlag(CacheScope.Slugs))
            {
                sp.GetRequiredService<ISlugResolver>().Invalidate();
            }

            if (scopes.HasFlag(CacheScope.Home))
            {
                sp.GetRequiredService<IHomeQueryService>().Invalidate();
            }
        }
        catch (Exception ex)
        {
            // Dữ liệu ĐÃ ghi thành công rồi — xóa cache hỏng thì cùng lắm là chờ hết TTL 5 phút,
            // không được vì thế mà ném lỗi ngược lên làm admin tưởng lưu thất bại.
            logger.LogError(ex, "Xóa cache thất bại cho vùng {Scopes}", scopes);
        }
    }
}
