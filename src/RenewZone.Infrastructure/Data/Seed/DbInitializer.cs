using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    /// <summary>Mật khẩu admin khi chạy local. Deploy công khai PHẢI đặt <c>Seed:AdminPassword</c>
    /// trong appsettings.Production.json (file này bị .gitignore nên mật khẩu thật không vào git).</summary>
    private const string DefaultAdminPassword = "Admin@123";

    /// <summary>
    /// Tài khoản quản trị mặc định: <c>admin</c> / <see cref="DefaultAdminPassword"/>, ghi đè được
    /// bằng cấu hình <c>Seed:AdminPassword</c>.
    /// ⚠️ CHỈ tạo khi user chưa tồn tại (idempotent) — đổi cấu hình SAU khi DB đã seed thì KHÔNG
    /// có tác dụng, phải đặt mật khẩu TRƯỚC lần chạy đầu tiên trên database mới.
    /// </summary>
    private static async Task SeedIdentityAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        // Role phải tồn tại TRƯỚC khi có ai đăng ký — AuthController gán role Customer ngay
        // lúc tạo tài khoản, thiếu role thì AddToRoleAsync trả lỗi và người đầu tiên đăng ký hỏng.
        foreach (var role in new[] { AppRoles.Admin, AppRoles.Customer })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        const string adminUserName = "admin";
        if (await userManager.FindByNameAsync(adminUserName) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminUserName,
                Email = "admin@applestoreuyenntt.local",
                FullName = "Quản trị viên",
                EmailConfirmed = true
            };

            var password = configuration["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(password))
            {
                password = DefaultAdminPassword;
            }

            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Seed tài khoản admin thất bại: " + string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }

        await BackfillUserCreatedAtAsync(services);
    }

    /// <summary>
    /// Cột <c>AspNetUsers.CreatedAt</c> thêm ở Giai đoạn 9 nên tài khoản tạo TRƯỚC đó mang giá trị
    /// mặc định của SQL Server (<c>0001-01-01</c>) — màn admin Khách hàng sẽ hiện "ngày tham gia"
    /// vô nghĩa. Gán tạm về thời điểm chạy để dữ liệu đọc được; chỉ đụng đúng những dòng còn sai
    /// nên chạy bao nhiêu lần cũng vô hại (idempotent tự nhiên, giống <c>BackfillRebrandAsync</c>).
    /// </summary>
    private static async Task BackfillUserCreatedAtAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var epoch = new DateTime(1, 1, 1);

        await db.Users
            .Where(u => u.CreatedAt <= epoch)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.CreatedAt, DateTime.Now));
    }
}
