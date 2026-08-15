using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using RenewZone.Infrastructure;
using RenewZone.Infrastructure.Data.Seed;
using RenewZone.Infrastructure.Identity;
using RenewZone.Web.Hubs;
using RenewZone.Web.Models.Options;
using RenewZone.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// DbContext + Identity + services nghiệp vụ (xem RenewZone.Infrastructure/DependencyInjection.cs)
builder.Services.AddInfrastructure(builder.Configuration);

// ─────────────────── XÁC THỰC: HAI PHIÊN ĐỘC LẬP (quyết định 2026-07-28) ───────────────────
// Khu quản trị và web khách là hai VÙNG TIN CẬY khác nhau nên có cookie riêng — xem lý do đầy
// đủ ở AppAuthSchemes. Nhờ đó: đặt được hai thời hạn khác nhau, đăng nhập đồng thời hai vai
// trong cùng trình duyệt, và cookie quản trị không bao giờ được gửi cho trang khách.

// 1) KHÁCH HÀNG — scheme MẶC ĐỊNH của Identity. Mọi chỗ đọc `User` ở web khách vẫn như cũ.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = AppAuthSchemes.CustomerCookie;
    options.LoginPath = "/dang-nhap";
    options.AccessDeniedPath = "/loi/403";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;

    options.Events.OnRedirectToLogin = context =>
    {
        // Request AJAX (fetch trong cart.js/auth.js...) mà bị đá về trang đăng nhập sẽ nhận
        // HTML 200 rồi JSON.parse vỡ — trả 401 để client biết đường mở hộp thoại đăng nhập.
        if (IsApiRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(WithReturnUrl("/dang-nhap", context.Request));
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (IsApiRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect("/loi/403");
        return Task.CompletedTask;
    };
});

// 2) QUẢN TRỊ — scheme riêng, cookie giới hạn đường dẫn /admin, hạn ngắn hơn hẳn.
builder.Services.AddAuthentication().AddCookie(AppAuthSchemes.Admin, options =>
{
    options.Cookie.Name = AppAuthSchemes.AdminCookie;

    // Điểm bảo mật chính: trình duyệt CHỈ gửi cookie này cho URL dưới /admin. Trang khách không
    // bao giờ nhận được nó ⇒ lỗ hổng phía web khách cũng không chạm tới phiên quản trị.
    options.Cookie.Path = AppAuthSchemes.AdminCookiePath;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    // 8 giờ — bằng một ca làm việc. Phiên khách 24 giờ là tiện lợi; phiên quản trị mà dài như
    // vậy thì máy bị mượn là mất quyền quản trị cả ngày.
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    options.LoginPath = "/admin/login";
    options.AccessDeniedPath = "/loi/403";

    options.Events.OnRedirectToLogin = context =>
    {
        if (IsApiRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(WithReturnUrl("/admin/login", context.Request));
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = IsApiRequest(context.Request)
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status302Found;

        if (!IsApiRequest(context.Request))
        {
            context.Response.Redirect("/loi/403");
        }

        return Task.CompletedTask;
    };

    // Thu hồi quyền phải có tác dụng NGAY.
    //
    // Identity có sẵn SecurityStampValidator, nhưng nó chỉ kiểm lại mỗi 30 phút
    // (SecurityStampValidatorOptions.ValidationInterval) và cấu hình đó là TOÀN CỤC — hạ xuống 0
    // sẽ bắt cả web khách truy vấn DB mỗi request. Với khu quản trị (rất ít người dùng, quyền rất
    // lớn) thì kiểm từng request là đánh đổi đúng, nên tự viết ở đây thay vì dùng validator chung.
    //
    // Kiểm 4 điều kiện: user còn tồn tại · security stamp còn khớp (đổi mật khẩu / đặt lại mật
    // khẩu đều làm stamp đổi) · không bị khóa · vẫn còn role Admin.
    options.Events.OnValidatePrincipal = async context =>
    {
        var services = context.HttpContext.RequestServices;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var stampClaimType = services.GetRequiredService<IOptions<IdentityOptions>>()
            .Value.ClaimsIdentity.SecurityStampClaimType;

        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = context.Principal?.FindFirstValue(stampClaimType);

        var user = userId is null ? null : await userManager.FindByIdAsync(userId);

        var stillValid = user is not null
                         && stamp is not null
                         && stamp == await userManager.GetSecurityStampAsync(user)
                         && !await userManager.IsLockedOutAsync(user)
                         && await userManager.IsInRoleAsync(user, AppRoles.Admin);

        if (!stillValid)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(AppAuthSchemes.Admin);
        }
    };
});

static string WithReturnUrl(string path, HttpRequest request) =>
    $"{path}?returnUrl={Uri.EscapeDataString(request.Path + request.QueryString)}";

// Nhận diện request do JavaScript gửi (fetch/XHR) chứ không phải trình duyệt điều hướng trang.
// Kiểm cả 3 dấu hiệu vì mỗi thứ có thể vắng mặt: fetch không tự thêm X-Requested-With, còn
// Accept thì tùy client đặt. Trình duyệt điều hướng KHÔNG bao giờ có Content-Type: application/json,
// nên dấu hiệu thứ 3 là cái chắc chắn nhất cho các endpoint JSON của dự án này.
static bool IsApiRequest(HttpRequest request) =>
    string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
    || request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)
    || (request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false);

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

// Công tắc cho phép/chặn công cụ tìm kiếm index site (bản demo công khai phải chặn)
builder.Services.Configure<SeoOptions>(
    builder.Configuration.GetSection(SeoOptions.SectionName));

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

// Realtime báo đơn hàng mới cho trang admin (Giai đoạn 8). Server đẩy sự kiện qua IHubContext
// khi khách đặt đơn ⇒ admin không phải F5. Hub được bảo vệ [Authorize(Roles = Admin)].
builder.Services.AddSignalR();

var app = builder.Build();

// Áp migrations + seed dữ liệu mẫu khi khởi động (idempotent — đủ dùng cho môi trường học tập)
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    // Exception CHƯA bắt ⇒ trang 500 tùy chỉnh. CHỈ gắn ở Production: Development giữ
    // Developer Exception Page (chủ đích — hữu ích khi dev). Test trang 500 phải chạy
    // ASPNETCORE_ENVIRONMENT=Production (xem docs/PHASE7_PROMPT.md mục 0.6).
    app.UseExceptionHandler("/loi/500");
    app.UseHsts();
}

app.UseHttpsRedirection();

// File người dùng/admin tải lên (MediaService ghi vào wwwroot/uploads/...) KHÔNG tồn tại lúc
// build/publish nên không nằm trong manifest của MapStaticAssets() bên dưới — bản publish thật
// sẽ 404 nếu thiếu dòng này (Development có fallback quét đĩa nên trước đây không lộ ra).
// Scope riêng /uploads để không chồng lấn với MapStaticAssets (css/js/lib vẫn hưởng nén+fingerprint).
var uploadsRoot = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsRoot); // wwwroot/uploads bị .gitignore — clone mới chưa có thư mục này
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads"
});

// 404/403... ⇒ re-execute sang ErrorController, GIỮ NGUYÊN status code + URL gốc (KHÔNG redirect —
// redirect trả 302 làm Google tưởng trang tồn tại). Đặt NGOÀI if để test được ở cả Development.
app.UseStatusCodePagesWithReExecute("/loi/{0}");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();

// Hub realtime của admin. Đặt SAU UseAuthentication/UseAuthorization để [Authorize] trên hub
// có hiệu lực; là endpoint riêng nên không lọt fallback route "{slug}" bên dưới.
app.MapHub<AdminNotifyHub>("/admin/hub/thong-bao");

// Hub realtime của KHÁCH (Giai đoạn 9 — báo khi đơn của họ đổi trạng thái).
// Route 2 đoạn nên không lọt fallback "{slug}"; [Authorize] trên hub có hiệu lực vì đặt sau
// UseAuthentication/UseAuthorization giống hub admin.
app.MapHub<CustomerNotifyHub>("/hub/thong-bao");

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
