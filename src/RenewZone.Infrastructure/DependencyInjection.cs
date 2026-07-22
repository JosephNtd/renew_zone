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
        // ----- Database -----
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

        // ----- Identity (chỉ phục vụ Admin) -----
        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Nới lỏng cho môi trường học tập; siết lại nếu deploy thật
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // ----- Services nghiệp vụ -----
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

        // ----- Services phục vụ Admin (Giai đoạn 6) -----
        services.AddScoped<ISlugService, SlugService>();
        services.AddScoped<IAdminBadgeService, AdminBadgeService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IOrderAdminService, OrderAdminService>();

        return services;
    }
}
