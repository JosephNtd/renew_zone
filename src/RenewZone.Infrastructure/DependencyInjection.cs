using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RenewZone.Domain.Interfaces;
using RenewZone.Infrastructure.Data;
using RenewZone.Infrastructure.Identity;
using RenewZone.Infrastructure.Services;

namespace RenewZone.Infrastructure;

/// <summary>
/// Điểm đăng ký DI duy nhất của tầng Infrastructure.
/// Web chỉ cần gọi AddInfrastructure() — không biết chi tiết DbContext/Identity bên trong.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection trong appsettings.json");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString);

            // Product xóa mềm (có query filter) nhưng ProductImage/ProductOptionGroup thì không
            // (chúng bị xóa CỨNG theo cha qua Cascade — xem CLAUDE.md mục 4.2), nên EF cảnh báo
            // "required end of a relationship". Ở model này là CẢNH BÁO SAI:
            //   • các bảng con chỉ được đọc qua Include từ Product ⇒ filter của cha đã lọc sẵn;
            //   • admin có truy vấn thẳng ProductImages thì cũng luôn kèm ProductId lấy từ
            //     một query Product đã qua filter.
            // Phương án thay thế (gắn filter "!x.Product.IsDeleted" cho từng bảng con) sẽ thêm
            // JOIN vào MỌI query catalog và lan xuống cả ProductOptionValue — đắt hơn lợi ích.
            options.ConfigureWarnings(w =>
                w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        // Identity — dùng CHUNG cho quản trị viên và khách mua hàng (Giai đoạn 9), phân biệt bằng role
        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Nới lỏng cho môi trường học tập; siết lại nếu deploy thật
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = false;

                // ⚠️ Giai đoạn 9 hạ xuống false: khách đăng nhập bằng SỐ ĐIỆN THOẠI, email là
                // TÙY CHỌN — để true thì Identity từ chối mọi tài khoản không có email.
                // Tính duy nhất của email (khi khách có nhập) được kiểm tay trong AuthController,
                // nên vẫn không thể có 2 tài khoản trùng email.
                options.User.RequireUniqueEmail = false;

                // UserName của khách là SĐT ⇒ phải cho phép chữ số và dấu '+' (dạng +84...).
                // Mặc định của Identity đã gồm 0-9 nhưng khai tường minh để lần sau đọc là hiểu ngay.
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddEntityFrameworkStores<AppDbContext>()

            // Thêm claim họ tên vào cookie ⇒ header/thanh bên chào khách bằng TÊN chứ không
            // phải số điện thoại, mà không phải truy vấn DB ở mỗi lần tải trang.
            .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        // Services nghiệp vụ
        services.AddMemoryCache();

        // Singleton + IServiceScopeFactory: AppDbContext phụ thuộc nó nên KHÔNG được để nó
        // phụ thuộc ngược vào các service scoped lúc dựng đối tượng (vòng DI — xem CacheInvalidator)
        services.AddSingleton<ICacheInvalidator, CacheInvalidator>();

        services.AddScoped<ISiteContentService, SiteContentService>();
        services.AddScoped<IHomeQueryService, HomeQueryService>();
        services.AddScoped<ISlugResolver, SlugResolver>();
        services.AddScoped<IContentQueryService, ContentQueryService>();
        services.AddScoped<ICatalogQueryService, CatalogQueryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<INavigationService, NavigationService>();
        services.AddScoped<IContentBlockService, ContentBlockService>();
        services.AddScoped<INewsQueryService, NewsQueryService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ISitemapService, SitemapService>();

        // Tài khoản khách hàng (Giai đoạn 9) — KHÔNG cache: dữ liệu riêng từng người và đổi
        // ngay khi admin xử lý đơn, hiện bản cũ là khách tưởng hệ thống hỏng.
        services.AddScoped<IUserCartStore, UserCartStore>();
        services.AddScoped<ICustomerOrderService, CustomerOrderService>();
        services.AddScoped<ICustomerAddressService, CustomerAddressService>();
        services.AddScoped<IWishlistService, WishlistService>();

        // Services phục vụ Admin (Giai đoạn 6)
        services.AddScoped<ISlugService, SlugService>();
        services.AddScoped<IAdminBadgeService, AdminBadgeService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IOrderAdminService, OrderAdminService>();
        services.AddScoped<ICategoryAdminService, CategoryAdminService>();
        services.AddScoped<IProductAdminService, ProductAdminService>();
        services.AddScoped<IProductAttributeAdminService, ProductAttributeAdminService>();
        services.AddScoped<INewsAdminService, NewsAdminService>();
        services.AddScoped<IPageAdminService, PageAdminService>();
        services.AddScoped<INavigationAdminService, NavigationAdminService>();
        services.AddScoped<IContentBlockAdminService, ContentBlockAdminService>();
        services.AddScoped<IContactAdminService, ContactAdminService>();
        services.AddScoped<IReviewAdminService, ReviewAdminService>();
        services.AddScoped<ICustomerAdminService, CustomerAdminService>();
        services.AddScoped<ISettingAdminService, SettingAdminService>();
        services.AddScoped<ITrashService, TrashService>();

        return services;
    }
}
