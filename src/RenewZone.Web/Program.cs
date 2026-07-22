using System.Text.Json.Serialization;
using RenewZone.Infrastructure;
using RenewZone.Infrastructure.Data.Seed;
using RenewZone.Web.Models.Options;
using RenewZone.Web.Services;

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

// Giỏ hàng đọc/ghi Session ngoài phạm vi controller (ViewComponent, service) ⇒ cần accessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICartService, CartService>();

// Cấu hình KỸ THUẬT (dung lượng/đuôi file cho phép) — thuộc appsettings.json, không vào DB
builder.Services.Configure<UploadOptions>(
    builder.Configuration.GetSection(UploadOptions.SectionName));

// Nơi DUY NHẤT ghi file người dùng tải lên (form liên hệ + 4 chỗ upload của admin).
// Ở tầng Web vì phụ thuộc IFormFile + IWebHostEnvironment, giống ICartService phụ thuộc Session.
builder.Services.AddScoped<IMediaService, MediaService>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Enum đi/về dưới dạng TÊN ("Shipping") thay vì số thứ tự. Không có dòng này thì
        // [FromBody] gặp {"status":"Shipping"} sẽ bind ra null và action ném NullReference —
        // mặt khác số thứ tự trong JSON làm request/response gần như không đọc được khi debug.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

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

// Slug phẳng ở root (CLAUDE.md mục 4.3) — PHẢI đăng ký CUỐI CÙNG để mọi route tĩnh
// phía trên được ưu tiên. Chỉ khớp 1 segment nên "/admin/..." không lọt vào đây.
app.MapControllerRoute(
        name: "slug",
        pattern: "{slug}",
        defaults: new { controller = "Slug", action = "Resolve" })
    .WithStaticAssets();

app.Run();
