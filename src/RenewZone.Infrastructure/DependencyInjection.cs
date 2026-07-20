using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

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
        services.AddScoped<ISiteContentService, SiteContentService>();
        services.AddScoped<IHomeQueryService, HomeQueryService>();

        return services;
    }
}
