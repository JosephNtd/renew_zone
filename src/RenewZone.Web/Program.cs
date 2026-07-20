using RenewZone.Infrastructure;
using RenewZone.Infrastructure.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

// ===================== Services =====================

// DbContext + Identity + services nghiệp vụ (xem RenewZone.Infrastructure/DependencyInjection.cs)
builder.Services.AddInfrastructure(builder.Configuration);

// Cookie đăng nhập Admin — đường dẫn thuộc về tầng Web nên cấu hình tại đây
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/login";
    options.AccessDeniedPath = "/admin/login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Session — giỏ hàng của khách (guest checkout, không tài khoản)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // giỏ hàng là chức năng thiết yếu
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Áp migrations + seed dữ liệu mẫu khi khởi động (idempotent — đủ dùng cho môi trường học tập)
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}

// ===================== Pipeline =====================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();

// ===================== Routes =====================
// Thứ tự QUAN TRỌNG: route tĩnh đăng ký trước, fallback slug (Giai đoạn 3) đăng ký CUỐI CÙNG.

// Khu vực quản trị: /admin/...
app.MapControllerRoute(
        name: "admin",
        pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}",
        defaults: new { area = "Admin" })
    .WithStaticAssets();

// Web khách hàng
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// TODO (Giai đoạn 3): fallback route "{slug}" → SlugController.Resolve
// (tra Category → Product → NewsArticle → StaticPage, không khớp thì 404)

app.Run();
