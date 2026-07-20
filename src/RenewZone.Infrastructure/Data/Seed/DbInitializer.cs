using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RenewZone.Infrastructure.Identity;

namespace RenewZone.Infrastructure.Data.Seed;

/// <summary>
/// Khởi tạo database khi app start: áp migrations còn thiếu rồi seed dữ liệu (nếu bảng trống).
/// Đủ an toàn để gọi mỗi lần chạy — mọi bước seed đều idempotent.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        await SeedIdentityAsync(services);
        await SeedData.SeedAsync(db);
    }

    /// <summary>Tài khoản quản trị mặc định: admin / Admin@123 (đổi mật khẩu nếu deploy thật).</summary>
    private static async Task SeedIdentityAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(AppRoles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole(AppRoles.Admin));
        }

        const string adminUserName = "admin";
        if (await userManager.FindByNameAsync(adminUserName) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminUserName,
                Email = "admin@renewzone-demo.local",
                FullName = "Quản trị viên",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Seed tài khoản admin thất bại: " + string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }
    }
}
