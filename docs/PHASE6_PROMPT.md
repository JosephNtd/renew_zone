# PROMPT — Giai đoạn 6: Admin Panel (RenewZone Clone)

> Copy toàn bộ file này gửi cho session Claude Code mới (đang mở tại `d:\Learn\lam_viec\renew_zone`).
>
> **Mục tiêu:** Dựng trang quản trị `/admin` **hoàn chỉnh, đẹp, chặt chẽ, scalable** — nơi chủ shop tự vận hành toàn bộ website mà không cần lập trình viên.
>
> **Bối cảnh:** Giai đoạn 5 đã đưa toàn bộ nội dung web khách xuống DB (`MenuItems`, `ContentBlocks`, `SiteSettings`, `NewsCategories`) và mọi service cache đều đã có sẵn `Invalidate()`. Giai đoạn 6 là phần **"đầu kia của sợi dây"** — làm cho những bảng đó thật sự sửa được.
>
> **Câu hỏi phải trả lời được khi xong:** *"Chủ shop mở `/admin`, đổi giá một sản phẩm, đổi tên một mục menu, viết một bài mới, xác nhận một đơn hàng — web khách đổi theo ngay, không cần deploy, không cần chạm vào code."*

---

## 0. Cách làm việc (đọc trước khi code)

1. **Đọc `CLAUDE.md` trước tiên** — đặc biệt mục **4.1** (kiến trúc 3 tầng), **4.2** (schema + delete behaviors), **4.3** (routing slug), **4.7** (quy tắc nội dung + bảng nguồn dữ liệu), và **toàn bộ Lưu ý mục 6**. Đọc `docs/DESIGN.md` mục **2** (tokens), **3.19** (admin theme), **4** (animation), **7** (a11y).
2. **KHÔNG chế token/màu/font mới.** Admin dùng lại bảng màu của design system nhưng **ngôn ngữ thị giác khác** (đặc hơn, dày dữ liệu hơn, ít trang trí hơn) — xem mục 4 file này.
3. Máy: **Windows 11 + PowerShell** (KHÔNG dùng `&&`). NuGet Microsoft.* **pin `9.0.13`** (Lưu ý 6.7 — bản 10.x không hỗ trợ net9.0). SQL Server `localhost`, DB `RenewZoneClone`.
4. **Giai đoạn này CÓ migration** (soft delete). Lệnh:
   `dotnet ef migrations add Phase6SoftDelete -p src/RenewZone.Infrastructure -s src/RenewZone.Web -o Data/Migrations`
   `dotnet ef database update -p src/RenewZone.Infrastructure -s src/RenewZone.Web`
   **TUYỆT ĐỐI KHÔNG drop DB** — trong đó có đơn hàng test của Giai đoạn 4/5 dùng để làm màn quản lý đơn.
5. App đang chạy sẽ **khóa DLL** → `taskkill /F /IM RenewZone.Web.exe` trước khi `dotnet build`. Xong phải **thật sự `dotnet run` + smoke test bằng curl/sqlcmd**, không chỉ build. Kết thúc mỗi đợt: **0 warning / 0 error**.
6. Tài khoản seed sẵn: **`admin` / `Admin@123`** (email `admin@renewzone-demo.local`, role `Admin`).
7. Xong mỗi đợt → tick trong `CLAUDE.md` mục 5, cập nhật mục 1 + mục 4 (thêm 4.9), thêm dòng nhật ký mục 8, rồi commit tiếng Việt bằng heredoc `git commit -F - <<'EOF'` (**KHÔNG dùng here-string PowerShell `@'...'@`** — lọt ký tự `@`). Kết `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

## 1. Nguyên tắc CỨNG (đọc kỹ — đây là phần quyết định chất lượng kiến trúc)

### 1.1 Quy tắc phân tầng — không được phá

Giai đoạn 4 đã chốt: **tầng Web KHÔNG bao giờ đụng `AppDbContext`.** Admin cũng vậy, không có ngoại lệ.

```
Areas/Admin/Controllers  →  I*AdminService (Domain/Interfaces)  →  *AdminService (Infrastructure)  →  AppDbContext
        ↓                            ↑
   ViewModel (DataAnnotations)   Domain/Models/Admin/*  (DTO thuần, không EF, không MVC)
```

- Controller **chỉ** làm: nhận input → validate ModelState → gọi service → PRG/JSON → set TempData.
- Service chứa **toàn bộ nghiệp vụ**: kiểm slug trùng, kiểm ràng buộc xóa, sinh mã, chuyển trạng thái hợp lệ.
- **KHÔNG bind thẳng entity vào form** (mass-assignment). Luôn có ViewModel riêng cho từng form, map tay sang entity trong service. Đây là lỗi bảo mật kinh điển — một field `IsDeleted` hay `ViewCount` lọt vào form là hỏng.

### 1.2 Luồng dữ liệu Admin → Web khách (phần user quan tâm nhất)

```
┌─────────────┐  POST form   ┌──────────────┐         ┌──────────────┐
│ Admin form  │ ───────────► │  Controller  │────────►│ AdminService │
└─────────────┘              │ validate VM  │         │  nghiệp vụ   │
                             └──────────────┘         └──────┬───────┘
                                    ▲                        │ SaveChangesAsync()
                          PRG + TempData toast               ▼
                                    │              ┌────────────────────┐
                                    └──────────────│   AppDbContext     │
                                                   │ • gán audit        │
                                                   │ • soft delete      │
                                                   │ • GOM entity type  │
                                                   └─────────┬──────────┘
                                                             │ sau khi save OK
                                                             ▼
                                                   ┌────────────────────┐
                                                   │ ICacheInvalidator  │  ◄── "bộ não" cache
                                                   │ tra bảng phụ thuộc │
                                                   └─────────┬──────────┘
                                    ┌────────────────────────┼───────────────┬──────────────┐
                                    ▼                        ▼               ▼              ▼
                            ISiteContentService    INavigationService  IContentBlock   ISlugResolver
                                                                        Service        IHomeQueryService
                                                             │
                                                             ▼
                                             Web khách request kế tiếp đọc dữ liệu MỚI
```

**Bảng phụ thuộc cache — implementation phải khớp bảng này:**

| Admin sửa bảng | Xóa cache của | Ảnh hưởng trang khách |
|---|---|---|
| `Category` | `ISiteContentService`, `IHomeQueryService`, `ISlugResolver` | menu dropdown, 6 tile trang chủ, `/iphone`, breadcrumb |
| `Product`, `ProductImage`, `ProductOptionGroup/Value` | `IHomeQueryService`, `ISlugResolver` | section trang chủ, trang danh mục, PDP, giỏ hàng |
| `NewsArticle`, `NewsCategory` | `IHomeQueryService`, `ISlugResolver` | khối tin trang chủ, `/tin-tuc`, chi tiết bài |
| `StaticPage` | `ISlugResolver` | `/gioi-thieu`, các trang chính sách, `/sua-chua` |
| `Banner`, `Testimonial` | `IHomeQueryService` | hero slider, khối đánh giá |
| `MenuItem` | `INavigationService` | menu chính, offcanvas mobile, 2 cột footer |
| `ContentBlock` | `IContentBlockService` | cam kết PDP, trấn an giỏ/checkout, tile `/lien-he` |
| `SiteSetting` | `ISiteContentService` | topbar, header, footer, bank checkout, `/tin-tuc`, `/lien-he` |
| `Order`, `OrderItem`, `ContactMessage` | *(không cache)* | — |

### 1.3 Cache invalidation phải là TỰ ĐỘNG, không phải "nhớ mà gọi"

**Đây là quyết định kiến trúc quan trọng nhất của giai đoạn này.**

Cách ngây thơ là mỗi action sau khi lưu thì gọi tay `_siteContent.Invalidate(); _slugResolver.Invalidate();`. Với ~15 màn hình × 3 action, chỉ cần **một chỗ quên là sinh bug "sửa xong không thấy đổi"** — loại bug rất khó truy vì phải đợi 5 phút mới hết triệu chứng.

**Bắt buộc làm theo hướng tự động:**

```csharp
// Infrastructure/Data/AppDbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    ApplyAuditInfo();
    ApplySoftDelete();

    // Gom loại entity bị đụng TRƯỚC khi save (sau khi save ChangeTracker đã reset state)
    var touched = ChangeTracker.Entries()
        .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
        .Select(e => e.Entity.GetType())
        .Distinct()
        .ToList();

    var result = await base.SaveChangesAsync(ct);

    // Chỉ xóa cache khi ghi THÀNH CÔNG — save lỗi thì cache cũ vẫn đúng
    _cacheInvalidator.OnEntitiesChanged(touched);

    return result;
}
```

- `ICacheInvalidator` (Domain/Interfaces) + `CacheInvalidator` (Infrastructure) giữ **bảng tra `Type → cache scope`** đúng như mục 1.2. Thêm entity mới ở giai đoạn sau chỉ cần thêm 1 dòng vào bảng này.
- Nhận `IServiceProvider` (hoặc các service dạng `Lazy<T>`) để tránh vòng phụ thuộc DI: `AppDbContext → CacheInvalidator → SiteContentService → AppDbContext`. **Đây là bẫy thật, phải xử lý** — dùng `IServiceScopeFactory` hoặc đăng ký `CacheInvalidator` không phụ thuộc ngược vào DbContext.
- Ghi rõ trong CLAUDE.md: **thêm entity có cache ⇒ thêm dòng vào `CacheInvalidator`**, không cần sửa controller.
- Vẫn giữ `Invalidate()` public trên từng service (để test tay và để gọi cưỡng bức nếu cần).

> ⚠️ Lưu ý phụ: `ExecuteDelete()`/`ExecuteUpdate()` **KHÔNG đi qua `SaveChanges`** ⇒ không tự xóa cache. Chỗ nào dùng (xóa vĩnh viễn ở thùng rác) phải gọi invalidate tay và **ghi comment giải thích**.

### 1.4 Soft delete — đã chốt

Mọi entity **nội dung** đều xóa mềm. Entity **giao dịch** thì không (đơn hàng không được biến mất khỏi sổ sách — muốn hủy thì đổi `Status = Cancelled`).

| Xóa mềm (`ISoftDeletable`) | KHÔNG xóa mềm |
|---|---|
| `Category`, `Product`, `NewsArticle`, `NewsCategory`, `StaticPage`, `Banner`, `Testimonial`, `MenuItem`, `ContentBlock`, `ContactMessage` | `Order`, `OrderItem`, `SiteSetting`, `ProductImage`, `ProductOptionGroup`, `ProductOptionValue` (xóa cứng theo cha qua Cascade), bảng Identity |

```csharp
// Domain/Common/ISoftDeletable.cs
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
```

**Cách triển khai (3 mảnh, thiếu mảnh nào là hở):**

1. **Chặn ở `SaveChanges`** — biến `Deleted` thành `Modified`:
   ```csharp
   private void ApplySoftDelete()
   {
       foreach (var entry in ChangeTracker.Entries<ISoftDeletable>().Where(e => e.State == EntityState.Deleted))
       {
           entry.State = EntityState.Modified;
           entry.Entity.IsDeleted = true;
           entry.Entity.DeletedAt = DateTime.Now;
       }
   }
   ```
   ⇒ Code nghiệp vụ cứ gọi `db.Products.Remove(p)` như bình thường, **không ai có thể lỡ tay xóa cứng**.

2. **Global Query Filter** — web khách và mọi query admin thường đều tự động bỏ qua:
   ```csharp
   // OnModelCreating — áp tự động cho MỌI entity implement ISoftDeletable (đừng gõ tay từng bảng)
   foreach (var entityType in builder.Model.GetEntityTypes()
                .Where(t => typeof(ISoftDeletable).IsAssignableFrom(t.ClrType)))
   {
       var param = Expression.Parameter(entityType.ClrType, "e");
       var body = Expression.Equal(
           Expression.Property(param, nameof(ISoftDeletable.IsDeleted)),
           Expression.Constant(false));
       builder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, param));
   }
   ```

3. **Xóa vĩnh viễn** ở màn Thùng rác dùng `ExecuteDeleteAsync()` (bypass change tracker nên không bị chặn ở bước 1) — nhớ invalidate cache tay (xem 1.3).

**Bẫy phải xử lý:**
- EF cảnh báo khi entity **có filter** được navigate từ entity **không có filter** qua quan hệ **required**. Ở đây: `Product.Category` là required và cả hai đều có filter ⇒ OK. `OrderItem.Product` là optional ⇒ OK (sản phẩm bị xóa mềm thì `Include` trả null, `OrderItem` vẫn sống bằng snapshot — đúng ý đồ từ Giai đoạn 1).
- Màn Thùng rác **bắt buộc** `IgnoreQueryFilters()`, nếu không sẽ luôn rỗng.
- Đếm số liệu ở Dashboard cũng đi qua filter ⇒ tự động không đếm bản ghi đã xóa. Đúng mong muốn.
- Slug của bản ghi đã xóa mềm **vẫn chiếm unique index** ⇒ tạo mới trùng slug sẽ lỗi DB. Service kiểm slug phải `IgnoreQueryFilters()` và báo lỗi tử tế: *"Slug này đang thuộc về một mục trong Thùng rác."*

### 1.5 Slug — nợ kỹ thuật từ Giai đoạn 1, giai đoạn này PHẢI trả

`CLAUDE.md` Lưu ý 6.4 ghi rõ: slug phải **unique xuyên 4 bảng** (`Category`/`Product`/`NewsArticle`/`StaticPage`) vì fallback route `{slug}` tra cả 4. DB chỉ có unique index **trong từng bảng** ⇒ hiện tại không gì ngăn admin đặt slug sản phẩm trùng slug danh mục, và khi đó `SlugResolver` sẽ trả về danh mục (thứ tự ưu tiên) — sản phẩm biến mất một cách âm thầm.

```csharp
// Domain/Interfaces/ISlugService.cs
public interface ISlugService
{
    /// Chuẩn hóa tiếng Việt: "iPhone 16 Pro Max" → "iphone-16-pro-max"
    string Slugify(string input);

    /// Kiểm tra XUYÊN 4 BẢNG. excludeType+excludeId để tự loại chính nó khi sửa.
    Task<SlugCheckResult> CheckAsync(string slug, SlugTargetType? excludeType = null, int? excludeId = null);

    /// Trả slug rảnh: "iphone-16" đã có ⇒ "iphone-16-2"
    Task<string> MakeUniqueAsync(string baseSlug, SlugTargetType? excludeType = null, int? excludeId = null);
}

public record SlugCheckResult(bool IsAvailable, SlugTargetType? TakenBy, string? TakenByName, bool IsInTrash);
```

- `Slugify` phải xử lý tiếng Việt: `Normalize(NormalizationForm.FormD)` + loại `NonSpacingMark` + map `đ/Đ → d`, lowercase, thay ký tự không phải `[a-z0-9]` bằng `-`, gộp `-` liên tiếp, trim `-`.
- Kiểm **cả bản ghi đã xóa mềm** (`IgnoreQueryFilters`) và trả `IsInTrash = true` để UI báo đúng nguyên nhân.
- **Validate ở server là bắt buộc.** Ngoài ra thêm kiểm tra realtime cho UX: `GET /admin/api/slug/kiem-tra?slug=x&type=Product&id=5` → JSON, ô slug hiện dấu ✓ xanh / ✗ đỏ kèm lý do (xem mục 5.4).
- Slug đổi ⇒ **URL cũ chết**. Cảnh báo trong form khi sửa slug của bản ghi đã tồn tại: *"Đổi slug sẽ làm link cũ không còn truy cập được."* (Redirect 301 để dành Giai đoạn 7.)

### 1.6 Bảo mật — checklist không được bỏ sót

| Việc | Cách làm |
|---|---|
| Chặn toàn Area | `BaseAdminController` gắn `[Area("Admin")] [Authorize(Roles = AppRoles.Admin)] [AutoValidateAntiforgeryToken]`, **mọi** controller admin kế thừa. Dùng hằng `AppRoles.Admin`, không gõ chuỗi "Admin". |
| Anti-forgery | `[AutoValidateAntiforgeryToken]` ở base ⇒ tự bảo vệ **mọi** POST/PUT/DELETE kể cả quên gắn. AJAX gửi token qua header `RequestVerificationToken` (đã có sẵn cơ chế từ cart.js Giai đoạn 4). |
| Open redirect | `returnUrl` sau đăng nhập **bắt buộc** `Url.IsLocalUrl(returnUrl)` hoặc `LocalRedirect()`. Không bao giờ `Redirect(returnUrl)` trần. |
| Mass assignment | ViewModel riêng cho mọi form (mục 1.1). Không có `IsDeleted`, `ViewCount`, `CreatedAt` trong form. |
| Upload | Đi qua `IMediaService` duy nhất (mục 3.5) — đổi tên GUID, whitelist đuôi + ContentType, giới hạn dung lượng. |
| Brute force | Identity đã cấu hình lockout 5 lần / 10 phút (Giai đoạn 0). Trang login hiển thị thông báo bị khóa cho tử tế. |
| Rò rỉ thông tin đăng nhập | Sai user hay sai mật khẩu đều báo **cùng một câu** "Tài khoản hoặc mật khẩu không đúng". |
| `noindex` | `_AdminLayout` đã có `<meta name="robots" content="noindex, nofollow">` — giữ nguyên. |
| Log | `ILogger` ghi lại thao tác nhạy cảm: đăng nhập thất bại, xóa, xóa vĩnh viễn, đổi trạng thái đơn. |

**XSS — rủi ro đã biết, phải ghi nhận rõ ràng:** admin nhập HTML qua TinyMCE, web khách render bằng `@Html.Raw`. Trong phạm vi dự án học tập (1 tài khoản admin tin cậy) đây là **rủi ro chấp nhận được**, nhưng:
- Cấu hình TinyMCE `valid_elements` whitelist (không cho `<script>`, `<iframe>`, thuộc tính `on*`).
- Ghi vào CLAUDE.md: *nếu sau này mở thêm role Editor thì BẮT BUỘC thêm thư viện sanitize HTML (`HtmlSanitizer`) ở tầng service trước khi lưu.*
- **Không** dựa vào việc "TinyMCE đã lọc rồi" — client-side filter không phải biện pháp bảo mật.

---

## 2. Phạm vi — chia 3 đợt

> Làm xong đợt nào **build sạch + smoke test + commit đợt đó**, rồi mới sang đợt sau. Đừng gộp 3 đợt vào một commit.

### ĐỢT A — Nền tảng + Đơn hàng *(sau đợt này admin đã dùng được thật)*

| # | Việc |
|---|---|
| A1 | `ISoftDeletable` + global query filter + migration `Phase6SoftDelete` |
| A2 | `ICacheInvalidator` + móc vào `SaveChangesAsync` (mục 1.3) |
| A3 | `ISlugService` (mục 1.5) + endpoint kiểm tra realtime |
| A4 | `IMediaService` — gom logic upload, **refactor `ContactController` dùng lại** (mục 3.5) |
| A5 | `BaseAdminController` + `AccountController` (đăng nhập/đăng xuất) + trang login |
| A6 | `_AdminLayout` hoàn chỉnh: sidebar + topbar + toast + breadcrumb (mục 4–5) |
| A7 | `admin.css` + `admin.js` — toàn bộ design system admin |
| A8 | Dashboard: 4 stat card + biểu đồ doanh thu SVG + đơn mới + liên hệ mới |
| A9 | Quản lý đơn hàng: danh sách (lọc/tìm/phân trang) + chi tiết + đổi trạng thái (AJAX, có kiểm luồng hợp lệ) |

### ĐỢT B — Catalog *(phần phức tạp nhất)*

| # | Việc |
|---|---|
| B1 | CRUD Danh mục 2 cấp + kéo-thả sắp xếp + chọn icon |
| B2 | CRUD Sản phẩm — form nhiều tab, TinyMCE, upload nhiều ảnh, kéo-thả thứ tự ảnh, đặt ảnh chính |
| B3 | Quản lý biến thể: repeater nhóm option → giá trị, `PriceAdjustment`, `IsDefault` |
| B4 | Bảng sản phẩm: lọc theo danh mục/trạng thái, tìm, sắp xếp cột, thao tác hàng loạt |

### ĐỢT C — Nội dung + Hệ thống

| # | Việc |
|---|---|
| C1 | CRUD Bài viết (chọn `NewsCategory`, TinyMCE, ảnh thumbnail) + CRUD Chuyên mục tin |
| C2 | CRUD Trang tĩnh · Banner (theo `Position`) · Testimonial |
| C3 | CRUD Menu (`MenuItem` 2 cấp, kéo-thả, 3 nhóm) + Khối nội dung (`ContentBlock` theo `GroupKey`) |
| C4 | Hộp thư liên hệ: danh sách, đánh dấu đã đọc, xem file đính kèm |
| C5 | Cấu hình site: form nhóm theo chủ đề, sửa `SiteSettings` |
| C6 | **Thùng rác**: liệt kê theo loại, khôi phục, xóa vĩnh viễn |

---

## 3. Kiến trúc Backend

### 3.1 Danh sách service (Domain/Interfaces + Infrastructure/Services)

> **Đã cân nhắc và LOẠI BỎ** phương án `IAdminCrudService<TEntity>` generic: mỗi aggregate có nghiệp vụ riêng thật sự (sản phẩm có biến thể + ảnh, danh mục có ràng buộc cha-con, đơn hàng có luồng trạng thái), nhét vào generic sẽ phải "thoát hiểm" liên tục bằng override. Viết tường minh từng service, chỉ **dùng chung phần phân trang/lọc**.

```csharp
// Dùng chung cho MỌI màn danh sách admin — Domain/Models/Admin/AdminListRequest.cs
public class AdminListRequest
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDesc { get; init; }
}
```

| Service | Trách nhiệm chính |
|---|---|
| `IDashboardService` | `GetSummaryAsync()` → `DashboardSummary` (đếm đơn theo trạng thái, doanh thu 30 ngày theo ngày, đơn mới nhất, liên hệ chưa đọc, cảnh báo dữ liệu thiếu) |
| `IOrderAdminService` | `GetListAsync(OrderListRequest)`, `GetDetailAsync(id)`, `ChangeStatusAsync(id, newStatus)` — **kiểm luồng hợp lệ**, `GetStatusCountsAsync()` |
| `ICategoryAdminService` | CRUD + `ReorderAsync(int[] ids)` + `CanDeleteAsync(id)` (còn sản phẩm / còn danh mục con ⇒ chặn, đúng ràng buộc Restrict) |
| `IProductAdminService` | CRUD + `SaveImagesAsync` + `ReorderImagesAsync` + `SetMainImageAsync` + `SaveOptionGroupsAsync` (đồng bộ thêm/sửa/xóa) + `ToggleActiveAsync` |
| `INewsAdminService` | CRUD bài viết + CRUD chuyên mục |
| `IPageAdminService` | CRUD trang tĩnh, banner, testimonial |
| `INavigationAdminService` | CRUD `MenuItem` 2 cấp + `ReorderAsync` (**kiểm không cho cha thành con của chính nó**) |
| `IContentBlockAdminService` | CRUD `ContentBlock` theo `GroupKey` + reorder |
| `ISettingAdminService` | `GetAllAsync()`, `SaveAsync(Dictionary<string,string?>)` — chỉ ghi key nằm trong `SettingKeys` (whitelist, chống chèn key rác) |
| `IContactAdminService` | Danh sách + `MarkReadAsync` + `GetUnreadCountAsync` |
| `ITrashService` | `GetTrashAsync(type)`, `RestoreAsync(type, id)`, `PurgeAsync(type, id)` |
| `ISlugService` | Mục 1.5 |
| `ICacheInvalidator` | Mục 1.3 |
| `IMediaService` | Mục 3.5 (đặt ở **tầng Web** vì phụ thuộc `IWebHostEnvironment` + `IFormFile`, giống `ICartService`) |

Đăng ký tất cả trong `Infrastructure/DependencyInjection.cs` (trừ `IMediaService` → `Program.cs`).

### 3.2 Routes (attribute routing, tiếng Việt như web khách)

```
AccountController      GET|POST /admin/login          [AllowAnonymous]
                       POST     /admin/logout
DashboardController    GET      /admin
OrderController        GET      /admin/don-hang            ?trangThai=&q=&tuNgay=&denNgay=&page=
                       GET      /admin/don-hang/{id}
                       POST     /admin/don-hang/{id}/trang-thai        (AJAX JSON)
CategoryController     GET      /admin/danh-muc
                       GET|POST /admin/danh-muc/them
                       GET|POST /admin/danh-muc/{id}/sua
                       POST     /admin/danh-muc/{id}/xoa
                       POST     /admin/danh-muc/sap-xep                (AJAX)
ProductController      GET      /admin/san-pham            ?danhMuc=&trangThai=&q=&sort=&page=
                       GET|POST /admin/san-pham/them
                       GET|POST /admin/san-pham/{id}/sua
                       POST     /admin/san-pham/{id}/xoa
                       POST     /admin/san-pham/{id}/toggle            (AJAX)
                       POST     /admin/san-pham/{id}/anh                (AJAX upload)
                       POST     /admin/san-pham/{id}/anh/sap-xep        (AJAX)
                       POST     /admin/san-pham/{id}/anh/{imageId}/xoa  (AJAX)
NewsController         /admin/bai-viet, /admin/chuyen-muc-tin
PageController         /admin/trang-tinh, /admin/banner, /admin/danh-gia
MenuController         /admin/menu            + /admin/menu/sap-xep     (AJAX)
ContentBlockController /admin/khoi-noi-dung   ?nhom=product.commitments
ContactController      /admin/lien-he, /admin/lien-he/{id}, POST .../da-doc
SettingController      GET|POST /admin/cau-hinh
TrashController        GET /admin/thung-rac  ?loai= , POST .../khoi-phuc , POST .../xoa-vinh-vien
MediaController        POST /admin/media/upload-editor    (TinyMCE)
SlugApiController      GET  /admin/api/slug/kiem-tra
```

> ⚠️ **Route admin nằm dưới prefix `/admin/` nên KHÔNG đụng fallback `{slug}`** (fallback chỉ khớp 1 segment). Nhưng vẫn phải dùng attribute routing để có URL tiếng Việt đẹp — route conventional `admin/{controller}/{action}` hiện có trong `Program.cs` **giữ lại** làm dự phòng, đăng ký trước fallback như cũ.

### 3.3 Luồng trạng thái đơn hàng — kiểm ở SERVER

```
        ┌──────────────────────── Cancelled ◄──────┐
        │                                          │
   New ─┴─► Confirmed ──► Shipping ──► Completed   │
    │           │                                  │
    └───────────┴──────────────────────────────────┘
```

- Cho phép: `New → Confirmed | Cancelled`, `Confirmed → Shipping | Cancelled`, `Shipping → Completed | Cancelled`, `Completed → ∅` (chốt sổ), `Cancelled → ∅`.
- `ChangeStatusAsync` **phải kiểm bảng chuyển hợp lệ và từ chối** bước nhảy sai — không được tin dropdown ở client (client chỉ hiện các lựa chọn hợp lệ để UX tốt, nhưng đó không phải biện pháp bảo vệ).
- Trả JSON `{ ok, message, status, statusText, statusClass, allowedNext: [...] }` để JS vẽ lại badge + dropdown mà không reload.
- Ghi `ILogger` mỗi lần đổi trạng thái.
- Ghi chú cho tương lai: hệ thống thật cần bảng `OrderStatusHistory` (ai đổi, lúc nào). Giai đoạn này dùng tạm `Order.UpdatedAt` — **ghi vào CLAUDE.md như nợ kỹ thuật đã biết**.

### 3.4 Ràng buộc xóa — báo lỗi tử tế, không để văng exception

DB đã cấu hình sẵn (CLAUDE.md mục 4.2). Service phải **kiểm trước** và trả thông báo tiếng Việt:

| Xóa | Kiểm trước | Thông báo khi bị chặn |
|---|---|---|
| Danh mục | còn sản phẩm? còn danh mục con? | "Không thể xóa — danh mục còn N sản phẩm. Hãy chuyển sản phẩm sang danh mục khác trước." |
| Mục menu cha | còn mục con? | "Không thể xóa — mục này còn N mục con." |
| Chuyên mục tin | *(SetNull — xóa được)* | Cảnh báo: "N bài viết sẽ chuyển sang trạng thái chưa phân loại." |
| Sản phẩm | *(soft delete — luôn được)* | Đơn hàng cũ vẫn giữ snapshot, không mất lịch sử. |

### 3.5 `IMediaService` — gom toàn bộ upload về một chỗ

Hiện logic upload nằm trong `ContactController.TryStoreAttachmentAsync`. **Đợt A phải trích ra thành service dùng chung** rồi cho `ContactController` gọi lại — không được copy-paste sang admin.

```csharp
// Web/Services/IMediaService.cs
public interface IMediaService
{
    Task<MediaResult> SaveAsync(IFormFile file, UploadProfile profile);
    Task<IReadOnlyList<MediaResult>> SaveManyAsync(IEnumerable<IFormFile> files, UploadProfile profile);
    void Delete(string? relativePath);   // xóa file rác khi gỡ ảnh sản phẩm
}

public record MediaResult(bool Ok, string? RelativePath, string? Error);
public enum UploadProfile { Contact, Product, News, Banner, Editor }
```

`appsettings.json` — **mỗi profile một cấu hình**, thay thế section `ContactUpload` hiện tại:

```json
"Uploads": {
  "Contact": { "MaxFileSizeMb": 5, "AllowedExtensions": [".jpg",".jpeg",".png",".webp",".pdf"], "FolderPath": "uploads/contact" },
  "Product": { "MaxFileSizeMb": 4, "AllowedExtensions": [".jpg",".jpeg",".png",".webp"],        "FolderPath": "uploads/products" },
  "News":    { "MaxFileSizeMb": 4, "AllowedExtensions": [".jpg",".jpeg",".png",".webp"],        "FolderPath": "uploads/news" },
  "Banner":  { "MaxFileSizeMb": 6, "AllowedExtensions": [".jpg",".jpeg",".png",".webp"],        "FolderPath": "uploads/banners" },
  "Editor":  { "MaxFileSizeMb": 4, "AllowedExtensions": [".jpg",".jpeg",".png",".webp"],        "FolderPath": "uploads/editor" }
}
```

**Bắt buộc giữ nguyên các quy tắc bảo mật đã có ở Giai đoạn 5:**
- Tên file **luôn** `Guid.NewGuid().ToString("N") + ext` — không bao giờ dùng tên gốc.
- Whitelist **cả đuôi lẫn ContentType**.
- Lưu vào `wwwroot/<FolderPath>/yyyy-MM/`.
- ⚠️ **`AllowedExtensions` mặc định trong C# phải để `[]`** — binder của `IConfiguration` *nối thêm* vào collection có sẵn chứ không thay thế (Lưu ý 6.14, đã dính một lần ở Giai đoạn 5). Giá trị mặc định đặt ở property `EffectiveExtensions`.
- `.gitignore` thêm toàn bộ `src/RenewZone.Web/wwwroot/uploads/` (hiện mới chặn `uploads/contact/`).

**Resize ảnh:** *KHÔNG* làm ở Giai đoạn 6. Thêm `SixLabors.ImageSharp` là thêm phụ thuộc + rủi ro pin version, mà kế hoạch đã xếp việc này vào Giai đoạn 7. Nhưng **thiết kế `IMediaService` sao cho thêm resize sau này chỉ sửa 1 file, không đụng chỗ gọi**. Ghi `// TODO Giai đoạn 7` ngay tại điểm chèn.

### 3.6 TinyMCE

- Tải về `wwwroot/lib/tinymce/` (**không CDN** — nhất quán với Swiper, Bootstrap Icons, jQuery đang làm).
- Khởi tạo trong `admin.js` cho mọi `textarea.js-editor`, tiếng Việt, plugin: `lists link image table code fullscreen paste`.
- `images_upload_handler` → `POST /admin/media/upload-editor` → trả `{ location: "/uploads/editor/2026-07/xxx.jpg" }`.
- `valid_elements` whitelist (mục 1.6), `paste_as_text: false` nhưng bật `paste_data_images: false` (ép người dùng upload đàng hoàng thay vì nhúng base64 phình DB).
- `content_style` nạp lại vài token của `.prose` để **nội dung trong editor trông giống hệt lúc lên web khách** — chi tiết nhỏ nhưng tạo cảm giác chuyên nghiệp rõ rệt.

---

## 4. Design System Admin

> **Kim chỉ nam:** Web khách bán hàng — cần *quyến rũ*. Admin là công cụ làm việc — cần **rõ ràng, đặc dữ liệu, thao tác nhanh, không mỏi mắt sau 2 tiếng dùng**. Đẹp ở admin = *trật tự, nhất quán, phản hồi tức thì*, chứ không phải nhiều hiệu ứng.

### 4.1 Chiến lược token — tách `tokens.css` dùng chung

Hiện toàn bộ `:root { --brand-*, --ink-*, --r-*, --sh-*, ... }` nằm trong `site.css` (mục 1). Admin **không nạp `site.css`** (theme khác hẳn) nên sẽ không có token.

**Cách làm (đúng và scalable):**
1. Cắt khối `:root` từ `site.css` ra file mới `wwwroot/css/tokens.css`.
2. `_Layout.cshtml` nạp `tokens.css` **trước** `site.css`.
3. `_AdminLayout.cshtml` nạp `tokens.css` **trước** `admin.css`.
4. `admin.css` khai thêm lớp token riêng của admin (xem dưới).

> Đây là thao tác **thuần di chuyển**, không đổi giá trị nào. Làm ở **đầu Đợt A** rồi smoke test lại `/`, `/iphone`, PDP, `/gio-hang`, `/tin-tuc`, `/lien-he` để chắc chắn web khách không đổi một pixel nào. Nếu vì lý do gì đó không làm được thì phương án dự phòng là copy khối `:root` sang `admin.css` — **nhưng phải ghi chú rõ là bản sao, sửa màu phải sửa 2 nơi**.

```css
/* admin.css — lớp token riêng của admin, đặt ngay đầu file */
:root {
  --adm-sidebar-w:        260px;
  --adm-sidebar-w-mini:   72px;
  --adm-topbar-h:         64px;

  --adm-bg:               #F4F2F5;   /* nền vùng nội dung (DESIGN 3.19) */
  --adm-surface:          #FFFFFF;   /* card, bảng */
  --adm-surface-2:        #FAF9FB;   /* hàng chẵn, vùng chìm */
  --adm-sidebar-bg:       #17141A;   /* = --ink-900 */
  --adm-sidebar-bg-2:     #221D28;   /* nhóm/hover */
  --adm-sidebar-text:     #B9B2C0;
  --adm-sidebar-text-on:  #FFFFFF;

  --adm-border:           #E6E1E8;
  --adm-border-strong:    #D5CEDA;

  /* Màu ngữ nghĩa trạng thái đơn — dùng nhất quán toàn admin */
  --adm-new:      #0D6EFD;   --adm-new-bg:      #E7F0FF;
  --adm-confirm:  #CE5F9A;   --adm-confirm-bg:  #FDF2F8;
  --adm-shipping: #F59E0B;   --adm-shipping-bg: #FEF5E7;
  --adm-done:     #28A745;   --adm-done-bg:     #E8F6EC;
  --adm-cancel:   #EC2D3F;   --adm-cancel-bg:   #FDECEE;

  --adm-r:        10px;      /* bo góc admin nhỏ hơn web khách — cảm giác "công cụ" */
  --adm-r-sm:     6px;
}
```

**Vì sao bo góc admin nhỏ hơn (10px vs 16px):** góc bo lớn tạo cảm giác mềm mại/tiêu dùng; công cụ quản trị cần cảm giác chắc chắn, chính xác. Đây là lựa chọn có chủ đích, không phải tùy hứng.

### 4.2 Thang chữ & mật độ

- Font: `--font-display` (Be Vietnam Pro) cho tiêu đề/số liệu/nút, `--font-body` (Nunito Sans) cho nội dung. **Không dùng Pacifico ở admin.**
- Cỡ chữ nền admin **nhỏ hơn web khách một nấc** (`14px` thay vì `16px`) — bảng dữ liệu cần nhét nhiều cột.
- Số liệu (giá, số lượng, mã đơn) dùng `font-variant-numeric: tabular-nums` để **các chữ số thẳng cột** khi xếp chồng trong bảng. Chi tiết nhỏ, khác biệt lớn.
- Mật độ hàng bảng: `padding: 12px 14px`. Có nút chuyển "chế độ gọn" (`padding: 7px 14px`) lưu vào `localStorage`.

### 4.3 Component — đặc tả

**1. Sidebar** (`--adm-sidebar-w`, nền `--adm-sidebar-bg`, cố định trái, tự cuộn)
- Đầu: logo wordmark `RenewZone` + chữ `Admin` nhỏ màu `--brand-300`.
- Nhóm menu có nhãn viết hoa nhỏ (`.adm-nav-group-label`, `.68rem`, `letter-spacing .08em`, màu `#6E6678`):
  - **TỔNG QUAN** — Dashboard
  - **BÁN HÀNG** — Đơn hàng *(badge số đơn mới)*
  - **CATALOG** — Danh mục · Sản phẩm
  - **NỘI DUNG** — Bài viết · Chuyên mục tin · Trang tĩnh · Banner · Đánh giá
  - **GIAO DIỆN** — Menu · Khối nội dung
  - **HỆ THỐNG** — Hộp thư *(badge chưa đọc)* · Cấu hình · Thùng rác
- Item: icon 18px + nhãn, `padding: 10px 16px`, `border-radius` 0.
  - Hover: nền `--adm-sidebar-bg-2`, chữ trắng, icon dịch phải 2px.
  - **Active**: viền trái 3px `--brand-400`, nền `rgba(255,255,255,.06)`, chữ trắng, icon màu `--brand-300`. Gắn `aria-current="page"`.
- Badge số: pill đỏ `--red-500` bên phải item, có `badge-pop` khi số tăng.
- **Thu gọn**: nút toggle ở topbar → sidebar co còn `--adm-sidebar-w-mini` (chỉ icon), nhãn ẩn, hover hiện tooltip. Trạng thái lưu `localStorage` (`rz_adm_sidebar`). Transition `width var(--t-base) var(--ease-out)`.
- `< 992px`: sidebar thành offcanvas trượt từ trái + lớp phủ tối.

**2. Topbar** (dính trên, cao `--adm-topbar-h`, nền `--adm-surface`, `box-shadow: var(--sh-sm)`)
- Trái: nút thu gọn sidebar (hamburger ở mobile) + **breadcrumb** (Trang chủ / Sản phẩm / Sửa).
- Phải: ô tìm nhanh · nút "Xem website" (mở tab mới, icon `bi-box-arrow-up-right`) · chuông thông báo (liên hệ chưa đọc, dropdown 5 mục gần nhất) · avatar tròn chữ cái đầu + dropdown (Tài khoản / Đăng xuất).

**3. Page header**
- `h1` (1.4rem, 800) + dòng phụ mô tả ngắn bên dưới; cụm nút hành động dính bên phải (`Thêm mới` primary, các nút phụ ghost).
- `< 768px`: nút xuống hàng, full width.

**4. Stat card** (dashboard)
- Card trắng, `--adm-r`, viền `--adm-border`, `padding: 20px`.
- Icon tròn 48px nền gradient (mỗi loại một màu ngữ nghĩa) + số **to 1.9rem, 800, tabular-nums** + nhãn `--ink-500` + dòng so sánh ("+12% so với tháng trước", xanh/đỏ kèm mũi tên).
- **Count-up** khi tải trang (`requestAnimationFrame`, 800ms, ease-out) — tôn trọng `prefers-reduced-motion` thì hiện số cuối luôn.
- Hover: nâng 2px + `--sh-md`. Cả card là link tới màn tương ứng (`.stretched-link`).

**5. Data table** (`.adm-table`)
- Bọc trong `.adm-table-wrap { overflow-x: auto }` — **bắt buộc**, tránh vỡ layout khi nhiều cột.
- `thead` dính (`position: sticky; top: 0`), nền `--adm-surface-2`, chữ `.78rem` viết hoa, `letter-spacing .04em`, màu `--ink-500`.
- Hàng: viền dưới `--adm-border`, hover nền `--brand-50` + transition `background var(--t-fast)`.
- Cột đầu có thể là checkbox chọn hàng loạt → hiện thanh "Đã chọn N mục · [Ẩn] [Xóa]" trượt lên từ đáy.
- Header **sắp xếp được**: click đổi `?sort=ten&desc=true`, hiện mũi tên. Là `<a>` thật (SEO không cần nhưng để bookmark/mở tab mới được).
- Cột thao tác cuối: nút icon-only (`bi-pencil`, `bi-trash`, `bi-eye`), hiện mờ 0.4 → rõ 1 khi hover hàng (giảm nhiễu thị giác).
- **Loading**: khi lọc/phân trang bằng AJAX thì phủ skeleton shimmer lên tbody, **không** làm bảng nhảy chiều cao.
- **Empty state** trong bảng: icon lớn + câu gợi ý + nút hành động chính.

**6. Filter bar** (`.adm-filters`)
- Hàng ngang: ô tìm (icon kính lúp, debounce 400ms) · các `<select>` lọc · khoảng ngày · nút "Lọc" · nút "Xóa lọc".
- Đang lọc thì hiện hàng **chip** bên dưới ghi rõ điều kiện (`Trạng thái: Đang giao ✕`), bấm ✕ gỡ từng điều kiện. Dùng lại `.rz-chip` từ `site.css`… **nhưng admin không nạp `site.css`** ⇒ định nghĩa `.adm-chip` riêng trong `admin.css` (đừng import chéo).
- Bộ lọc **nằm trên querystring** — F5 giữ nguyên, gửi link cho người khác được, nút back hoạt động đúng.

**7. Form**
- `.adm-form-card` trắng + `.adm-form-grid` 12 cột (`grid-template-columns: repeat(12, 1fr)`), field khai `--col-span`.
- Field: nhãn trên, input dưới, dòng hướng dẫn nhỏ màu `--ink-400`, lỗi đỏ + `shake` 300ms.
- Trường bắt buộc gắn dấu `*` đỏ.
- **Input tiền**: căn phải, có hậu tố `đ`, tự chèn dấu chấm ngăn nghìn khi gõ, submit thì gửi số thuần (JS lọc lại trước khi submit; **server vẫn phải parse phòng thủ**).
- **Input slug**: tự sinh từ ô Tên (chỉ khi người dùng chưa tự gõ slug), có nút "Tạo lại", và **chỉ báo trạng thái realtime** ở cuối ô: xoay tròn khi đang kiểm → ✓ xanh "Slug hợp lệ" / ✗ đỏ "Đã được dùng bởi sản phẩm «iPhone 16»" / ⚠ vàng "Đang nằm trong Thùng rác".
- **Toggle switch** cho các cờ bool (`IsActive`, `IsFeatured`, `IsBestSeller`, `IsContactPrice`): 44×24px, núm trượt `--ease-spring`, bật thì nền `--grad-brand`.
- **Sticky action bar**: thanh dính đáy form (`position: sticky; bottom: 0`), nền trắng, viền trên, chứa `Lưu` + `Lưu & tiếp tục` + `Hủy`. Form dài không phải cuộn xuống cuối mới lưu được.
- **Cảnh báo rời trang khi chưa lưu**: `beforeunload` nếu form đã đổi (`dirty` flag).

**8. Tabs trong form sản phẩm**
`Thông tin chung · Mô tả · Thông số · Hình ảnh · Biến thể` — tab có lỗi validate thì hiện chấm đỏ trên nhãn (**cực kỳ quan trọng**: lỗi nằm ở tab đang ẩn mà không báo thì người dùng bấm Lưu mãi không hiểu vì sao không được).

**9. Image uploader** (`.adm-uploader`)
- Dropzone viền đứt + grid thumbnail 120px, mỗi ảnh có: nút xóa (góc trên phải), nhãn "Ảnh chính" (nếu `IsMain`), tay cầm kéo.
- **Kéo-thả sắp xếp** → gửi mảng id mới về server (AJAX). Ảnh đầu tiên mặc định thành ảnh chính, hoặc bấm sao để đặt.
- Upload nhiều file cùng lúc, mỗi file có thanh tiến trình riêng (`XMLHttpRequest.upload.onprogress`).
- Lỗi từng file hiện ngay trên thumbnail đó, không làm hỏng các file khác.

**10. Repeater biến thể** (`.adm-repeater`)
- Mỗi **nhóm option** là một card: ô Tên nhóm + tay cầm kéo + nút xóa nhóm.
- Trong card là danh sách **giá trị**: ô Giá trị · ô Chênh lệch giá (cho phép số âm) · radio "Mặc định" · nút xóa.
- Nút "Thêm nhóm" / "Thêm giá trị" nhân bản từ `<template>`, **đánh lại index tên field** (`OptionGroups[0].Values[2].Value`) để model binder nhận đúng.
- Hàng mới trượt vào (`translateY(-6px) → 0` + fade 200ms), hàng xóa gập lại 200ms rồi mới bỏ khỏi DOM.
- Preview nhỏ: "Giá hiển thị khi chọn mặc định: **30.490.000đ**" — tính lại bằng JS mỗi khi đổi, giúp admin thấy ngay hậu quả.

**11. Badge trạng thái** (`.adm-badge`)
Nền nhạt + chữ đậm cùng tông, `border-radius: var(--r-pill)`, `.72rem`, 700. Dùng đúng cặp biến `--adm-{status}` / `--adm-{status}-bg` ở mục 4.1. **Không dùng badge nền đặc** — bảng nhiều hàng sẽ rất chói.

**12. Modal xác nhận** (`.adm-modal`)
- Xóa thường: tiêu đề + tên đối tượng in đậm + nút `Xóa` đỏ / `Hủy`.
- **Xóa vĩnh viễn** (thùng rác): thêm bước **gõ đúng tên** để bật nút — hành động không hoàn tác được thì phải khó bấm nhầm.
- Vào: overlay fade 150ms + hộp `scale(.96) → 1` 200ms `--ease-spring`. Bẫy focus trong modal, `Esc` đóng, trả focus về nút vừa bấm.

**13. Toast** — dùng lại API `RZ.toast(msg, type)` nhưng **định nghĩa riêng trong `admin.js`** (admin không nạp `site.js`). Vị trí góc phải dưới, tự tắt 4s, có nút đóng, xếp chồng. Sau PRG thì đọc `TempData["AdminToast"]` để hiện.

**14. Pagination admin** — số trang + dòng "Hiển thị 21–40 trên 137 mục" + chọn số dòng/trang (20/50/100, lưu `localStorage`).

**15. Trang đăng nhập** — bố cục riêng (`_LoginLayout.cshtml`, không sidebar):
- Nền `--grad-hero` phủ toàn màn + hoa văn chấm mờ.
- Card trắng 400px canh giữa, `--r-lg`, `--sh-lg`: logo, "Đăng nhập quản trị", 2 ô, checkbox "Ghi nhớ", nút full-width gradient.
- Sai mật khẩu: card `shake` 300ms + thông báo đỏ. Bị khóa: thông báo riêng có đếm ngược.
- Ô mật khẩu có nút hiện/ẩn (`bi-eye` / `bi-eye-slash`).
- Autofocus ô tên đăng nhập; Enter submit.

### 4.4 Animation admin — kỷ luật hơn web khách

| Chỗ | Hiệu ứng | Thông số |
|---|---|---|
| Hàng bảng khi tải | fade-up stagger | 20ms/hàng, **tối đa 12 hàng** rồi thôi (stagger 50 hàng thấy như trang bị lag) |
| Stat card | count-up số | 800ms ease-out |
| Sidebar thu gọn | width + opacity nhãn | `--t-base` |
| Item sidebar hover | icon `translateX(2px)` | `--t-fast` |
| Toggle switch | núm trượt | 200ms `--ease-spring` |
| Kéo-thả | item nhấc lên `scale(1.02)` + `--sh-lg`, chỗ trống hiện đường kẻ brand | — |
| Lưu thành công | nút → spinner → ✓ 600ms → về trạng thái thường | — |
| Modal | overlay fade + hộp scale-in | 150/200ms |
| Toast | trượt vào từ phải + fade | 250ms |
| Skeleton khi AJAX | shimmer `--grad-shine` | 1.2s linear infinite |
| Chuyển tab form | fade nội dung | 120ms (nhanh — người dùng bấm tab liên tục) |

**Quy tắc cứng (kế thừa DESIGN.md mục 4):** chỉ animate `transform`/`opacity`; mọi hiệu ứng bọc `@media (prefers-reduced-motion: reduce)`; hover chỉ trong `@media (hover: hover)`.

> **Nguyên tắc riêng cho admin:** không có hiệu ứng nào **chặn thao tác**. Người quản trị nhập liệu 100 sản phẩm sẽ ghét animation 400ms lặp lại 100 lần. Admin nhanh hơn web khách: `--t-fast`/`--t-base`, hầu như không dùng `--t-slow`.

---

## 5. Đặc tả từng màn hình

### 5.1 `/admin/login`
Xem component 15. Xử lý: `PasswordSignInAsync(user, pass, remember, lockoutOnFailure: true)`; đã đăng nhập mà vào lại thì redirect thẳng `/admin`; `returnUrl` phải qua `Url.IsLocalUrl`.

### 5.2 `/admin` — Dashboard
1. **Hàng 4 stat card**: Đơn mới (Status=New) · Doanh thu 30 ngày (chỉ tính `Completed`) · Sản phẩm đang bán · Liên hệ chưa đọc.
2. **Biểu đồ doanh thu 30 ngày** — **SVG tự vẽ, KHÔNG thêm Chart.js**: cột bo góc trên, gradient brand, hover hiện tooltip ngày + số tiền, trục Y 4 mốc, đường lưới mảnh. Không có dữ liệu ⇒ khối rỗng nhã nhặn.
   *Lý do không dùng Chart.js: thêm ~200KB và một phụ thuộc nữa cho đúng một biểu đồ; SVG tự vẽ ~60 dòng, kiểm soát hoàn toàn thẩm mỹ, khớp tuyệt đối với design system.*
3. **Vòng tròn tỉ lệ trạng thái đơn** (donut SVG) + chú giải.
4. **Bảng 5 đơn mới nhất** — mã, khách, tiền, trạng thái, thời gian tương đối ("2 giờ trước").
5. **5 liên hệ chưa đọc** — dạng list, bấm vào mở chi tiết.
6. **Khối "Cần chú ý"** *(điểm nhấn thể hiện sự chu đáo)*: cảnh báo dữ liệu thiếu — "3 sản phẩm chưa có ảnh", "2 danh mục đang trống", "5 sản phẩm chưa có mô tả". Mỗi dòng là link tới danh sách đã lọc sẵn. Không có vấn đề gì ⇒ hiện trạng thái "Mọi thứ đều ổn" kèm icon xanh.

### 5.3 Đơn hàng
**Danh sách** `/admin/don-hang`
- Tab nhanh theo trạng thái, **kèm số đếm** lấy từ DB: `Tất cả (137) · Mới (8) · Đã xác nhận (12) · Đang giao (5) · Hoàn thành (109) · Đã hủy (3)`.
- Cột: checkbox · Mã đơn (đậm, brand) · Khách hàng (tên + SĐT) · Số món · Tổng tiền (tabular-nums, phải) · Thanh toán (icon COD/CK) · Trạng thái (badge) · Ngày đặt · Thao tác.
- Lọc: tìm theo mã/tên/SĐT · khoảng ngày · phương thức thanh toán.
- **Đổi trạng thái ngay trên hàng**: bấm badge → dropdown chỉ chứa trạng thái hợp lệ kế tiếp → AJAX → badge đổi màu tại chỗ + toast, **không reload**.

**Chi tiết** `/admin/don-hang/{id}`
- 2 cột: trái là bảng sản phẩm (ảnh, tên, biến thể, đơn giá, SL, thành tiền) + tổng cộng; phải là card thông tin khách (tên, SĐT bấm gọi được, email, địa chỉ, ghi chú) + card thanh toán + card trạng thái.
- **Thanh timeline trạng thái** ngang: các chặng đã qua tô brand, chặng hiện tại có vòng sáng, chặng sau xám. Dùng lại ý tưởng `_CheckoutSteps` của web khách nhưng phiên bản admin.
- Nút hành động theo trạng thái hiện tại (không hiện nút vô nghĩa).
- Nút "In đơn" → `window.print()` + `@media print` ẩn sidebar/topbar/nút, chỉ in nội dung đơn. *(CSS in ấn ~20 dòng, tạo cảm giác sản phẩm hoàn chỉnh.)*

### 5.4 Danh mục `/admin/danh-muc`
- **Hiển thị dạng cây 2 cấp** (không phải bảng phẳng): danh mục cha có thể gập/mở, con thụt vào 32px.
- Mỗi hàng: tay cầm kéo · ảnh/icon · Tên · slug (chữ nhỏ xám) · số sản phẩm · toggle IsActive · nút sửa/xóa.
- **Kéo-thả sắp xếp trong cùng cấp** → AJAX lưu `DisplayOrder`. *(Không cho kéo đổi cấp cha-con ở phiên bản này — phức tạp và dễ sai; đổi cấp thì vào form sửa chọn danh mục cha. Ghi rõ quyết định này vào CLAUDE.md.)*
- Form: Tên · Slug (auto + kiểm) · Danh mục cha (select, **loại chính nó và toàn bộ con cháu** khỏi danh sách để tránh vòng lặp) · Ảnh · **Icon** (bảng chọn Bootstrap Icons có ô tìm, xem trước) · Thứ tự · IsActive.

### 5.5 Sản phẩm `/admin/san-pham`
**Danh sách**: cột Ảnh (thumb 44px) · Tên + slug · Danh mục · Giá (gốc gạch ngang + giá bán đỏ, hoặc chữ "Liên hệ") · các cờ (chip nhỏ: Nổi bật / Bán chạy / Trả góp) · Lượt xem · Toggle · Thao tác.
Lọc: danh mục (có cả cấp con) · trạng thái · có/không ảnh · tìm theo tên. Sắp xếp: tên, giá, lượt xem, ngày tạo.

**Form** — 5 tab (component 8):
1. **Thông tin chung** — Tên · Slug · Danh mục (select 2 cấp thụt lề) · Giá gốc · Giá KM · toggle "Giá liên hệ" (bật thì **khóa 2 ô giá** và hiện ghi chú "Sản phẩm giá liên hệ không thêm vào giỏ được" — đúng Lưu ý 6.5) · Mô tả ngắn · các toggle cờ.
2. **Mô tả** — TinyMCE (`DescriptionHtml`).
3. **Thông số** — TinyMCE (`SpecsHtml`), có nút "Chèn bảng thông số mẫu" tạo sẵn khung `<table>` 2 cột.
4. **Hình ảnh** — uploader (component 9).
5. **Biến thể** — repeater (component 10) + ghi chú giải thích công thức giá: *giá hiển thị = Giá KM + tổng chênh lệch các option đã chọn* (Lưu ý 6.3).

**Nút "Xem trên web"** ở đầu form (mở `/{slug}` tab mới) — cách nhanh nhất để admin tự kiểm chứng.

### 5.6 Bài viết · Chuyên mục tin
- Bài viết: bảng có thumbnail, tiêu đề, chuyên mục (badge), Nổi bật, lượt xem, ngày đăng. Form: Tiêu đề · Slug · Chuyên mục · Ảnh · Trích dẫn (đếm ký tự, max 500) · Nội dung TinyMCE · toggle Nổi bật · Ngày đăng (cho phép đặt lịch trong tương lai — nếu làm thì query web khách phải lọc `PublishedAt <= now`; **không làm thì đừng để lộ ô này**).
- Chuyên mục: bảng đơn giản + kéo-thả. Xóa thì cảnh báo số bài sẽ mất phân loại (SetNull).

### 5.7 Trang tĩnh · Banner · Đánh giá
- Trang tĩnh: bảng (Tiêu đề, slug, cập nhật lần cuối) + form TinyMCE toàn chiều rộng. Cảnh báo khi sửa slug của trang đang được menu trỏ tới.
- Banner: **nhóm theo `Position`** (HomeSlider / HomeMiddle), mỗi nhóm là lưới thẻ có ảnh xem trước tỉ lệ đúng (1400×450), kéo-thả sắp xếp, toggle.
- Đánh giá: bảng + form ngắn (tên, avatar, nội dung, thứ tự, toggle).

### 5.8 Menu `/admin/menu`
- 3 tab theo nhóm: **Menu chính · Footer – Chính sách · Footer – Hỗ trợ**.
- Cây 2 cấp kéo-thả (chỉ trong cùng nhóm và cùng cấp).
- Form: Tên hiển thị · URL · Icon · Mục cha · Kiểu (`Link` / `ProductMega`) · Mở tab mới · Thứ tự · IsActive.
- **Hiện cảnh báo nếu URL trỏ tới slug không tồn tại** — gọi `ISlugResolver`, báo "URL này hiện đang 404". Đây chính là loại lỗi mà Giai đoạn 2–4 từng mắc (menu trỏ `/sua-chua` khi chưa seed trang).
- Mục `ProductMega`: hiện ghi chú "Danh sách con của mục này sinh tự động từ Danh mục sản phẩm — không cần khai báo tay."

### 5.9 Khối nội dung `/admin/khoi-noi-dung`
- Chọn nhóm bằng tab: `product.commitments` · `cart.assurance` · `contact.channels` — **hiển thị bằng tên tiếng Việt dễ hiểu** kèm mô tả *chỗ nào trên web khách đang dùng nhóm này*, và **ảnh/khối xem trước** mô phỏng. Đây là điểm khiến admin thật sự dùng được thay vì đoán mò.
- Danh sách trong nhóm: kéo-thả, icon xem trước, tiêu đề, phụ đề, link, toggle.

### 5.10 Hộp thư `/admin/lien-he`
- Bảng: chấm chưa đọc · Họ tên · SĐT · Chủ đề · trích nội dung 1 dòng · kẹp giấy nếu có file · thời gian.
- **Hàng chưa đọc in đậm + nền `--brand-50` nhạt** (quy ước hộp thư quen thuộc).
- Chi tiết mở bằng **drawer trượt từ phải** (nhanh hơn chuyển trang): đầy đủ nội dung, link `tel:`/`mailto:`, ảnh đính kèm xem được (`<img>` nếu là ảnh, nút tải nếu PDF), tự đánh dấu đã đọc khi mở.
- Lọc: chưa đọc / đã đọc / theo chủ đề.

### 5.11 Cấu hình `/admin/cau-hinh`
- **Không phải một bảng key-value trần** — phải nhóm theo chủ đề với nhãn tiếng Việt + mô tả:
  - **Thông tin cửa hàng**: tên, tagline, hotline, email, địa chỉ, giờ mở cửa, MST
  - **Mạng xã hội & bản đồ**: Facebook, Messenger, Zalo, Map URL, Map Embed
  - **Thanh toán**: tên NH, số TK, chủ TK, mẫu nội dung CK *(kèm chú thích `{0}` = mã đơn)*
  - **Nội dung trang**: FooterAbout, FooterDisclaimer, ShippingNote, NewsPageTitle/Intro, ContactIntro, ContactSubjects *(nhập nhiều dòng, lưu nối bằng `|`)*
- Mỗi ô có **placeholder chính là giá trị fallback trong code** ⇒ admin biết ngay "để trống thì web hiện gì".
- Ô nào là URL thì có nút mở thử tab mới; ô hotline hiện xem trước dạng `tel:`.
- Lưu → toast + gọi invalidate (tự động qua mục 1.3).

### 5.12 Thùng rác `/admin/thung-rac`
- Tab theo loại (Sản phẩm, Danh mục, Bài viết, Trang tĩnh, Banner, Đánh giá, Menu, Khối nội dung, Liên hệ) kèm số đếm.
- Cột: Tên · Loại · Xóa lúc nào · nút **Khôi phục** / **Xóa vĩnh viễn**.
- Khôi phục danh mục con mà cha vẫn đang trong thùng rác ⇒ **chặn** và báo "Hãy khôi phục danh mục cha trước".
- Xóa vĩnh viễn: modal gõ tên xác nhận (component 12) + cảnh báo không hoàn tác.
- Trạng thái rỗng: "Thùng rác trống" + icon.

---

## 6. Responsive

Admin **ưu tiên desktop** (thực tế chủ shop quản trị trên máy tính) nhưng phải dùng được trên tablet và không vỡ trên điện thoại.

- `≥ 1200px`: bố cục đầy đủ, sidebar mở.
- `992–1199px`: sidebar tự thu gọn về chế độ icon.
- `< 992px`: sidebar thành offcanvas; topbar hiện hamburger; stat card 4 cột → 2 cột.
- `< 768px`: stat card 1 cột; **bảng chuyển sang dạng thẻ** (mỗi hàng thành một card, nhãn cột hiện bằng `::before` từ `data-label`) — cách này dùng được hơn hẳn cuộn ngang trên điện thoại. Áp dụng cho bảng đơn hàng và sản phẩm; các bảng đơn giản thì cuộn ngang là đủ.
- Form: lưới 12 cột → 1 cột; sticky action bar giữ nguyên (càng hữu ích trên màn nhỏ).
- Kiểm ở **375 / 768 / 1280 / 1920px**.

---

## 7. Accessibility

- Mọi input có `<label for>`; lỗi gắn `aria-describedby` + `aria-invalid="true"`.
- Sidebar là `<nav>`, item đang mở gắn `aria-current="page"`.
- Bảng có `<caption>` (ẩn bằng `.visually-hidden` nếu không muốn hiện) và `<th scope="col">`.
- Modal: `role="dialog"` + `aria-modal="true"`, **bẫy focus**, `Esc` đóng, trả focus về nút đã mở.
- Kết quả AJAX (lọc, đổi trạng thái, lưu) thông báo trong vùng `aria-live="polite"` — người dùng đọc màn hình phải biết thao tác đã xong.
- Kéo-thả **bắt buộc có phương án bàn phím thay thế**: mỗi item có nút ▲▼ (có thể ẩn hiện khi focus) hoặc ô nhập số thứ tự. Kéo-thả thuần chuột là rào cản a11y điển hình.
- Tương phản: chữ trên sidebar tối và badge trạng thái đều phải đạt **AA (4.5:1)**. `--adm-sidebar-text: #B9B2C0` trên `#17141A` đạt ~7:1 — giữ nguyên, đừng làm nhạt hơn.
- Focus ring nhìn thấy rõ trên **mọi** thành phần tương tác: `outline: 2px solid var(--brand-400); outline-offset: 2px`.

---

## 8. Danh sách file (tạo/sửa)

**Domain**
- `Common/ISoftDeletable.cs`
- `Interfaces/`: `ICacheInvalidator.cs`, `ISlugService.cs`, `IDashboardService.cs`, `IOrderAdminService.cs`, `ICategoryAdminService.cs`, `IProductAdminService.cs`, `INewsAdminService.cs`, `IPageAdminService.cs`, `INavigationAdminService.cs`, `IContentBlockAdminService.cs`, `ISettingAdminService.cs`, `IContactAdminService.cs`, `ITrashService.cs`
- `Models/Admin/`: `AdminListRequest.cs`, `DashboardSummary.cs`, `OrderListItem.cs`, `ProductListItem.cs`, `SlugCheckResult.cs`, `TrashItem.cs`…
- **Sửa**: 10 entity thêm `: ISoftDeletable`

**Infrastructure**
- `Services/`: `CacheInvalidator.cs`, `SlugService.cs`, `DashboardService.cs`, `OrderAdminService.cs`, `CategoryAdminService.cs`, `ProductAdminService.cs`, `NewsAdminService.cs`, `PageAdminService.cs`, `NavigationAdminService.cs`, `ContentBlockAdminService.cs`, `SettingAdminService.cs`, `ContactAdminService.cs`, `TrashService.cs`
- **Sửa**: `AppDbContext.cs` (soft delete + query filter tự động + hook cache), `DependencyInjection.cs`
- `Data/Migrations/*_Phase6SoftDelete.cs`

**Web — Areas/Admin**
- `Controllers/`: `BaseAdminController.cs`, `AccountController.cs`, `DashboardController.cs` *(sửa)*, `OrderController.cs`, `CategoryController.cs`, `ProductController.cs`, `NewsController.cs`, `NewsCategoryController.cs`, `PageController.cs`, `BannerController.cs`, `TestimonialController.cs`, `MenuController.cs`, `ContentBlockController.cs`, `ContactController.cs`, `SettingController.cs`, `TrashController.cs`, `MediaController.cs`, `SlugApiController.cs`
- `Models/`: ViewModel cho từng màn (list + form), `AdminToast.cs`
- `Views/`: `Shared/_AdminLayout.cshtml` *(viết lại)*, `_LoginLayout.cshtml`, `_AdminSidebar.cshtml`, `_AdminTopbar.cshtml`, `_AdminPageHeader.cshtml`, `_AdminPagination.cshtml`, `_AdminTable.cshtml`, `_ConfirmModal.cshtml`, `_StatusBadge.cshtml`, `_EmptyState.cshtml`, `_ToggleSwitch.cshtml`, `_SlugInput.cshtml`, `_ImageUploader.cshtml` + view từng màn
- `Views/_ViewImports.cshtml` *(sửa: thêm `@using` cho VM admin)*

**Web — chung**
- `Services/`: `IMediaService.cs`, `MediaService.cs`
- `Models/Options/UploadOptions.cs` *(thay `ContactUploadOptions`)*
- `wwwroot/css/`: **`tokens.css` (mới, cắt từ site.css)**, `admin.css`, `admin-print.css`
- `wwwroot/js/`: `admin.js`, `admin-form.js`, `admin-uploader.js`, `admin-sortable.js`, `admin-chart.js`
- `wwwroot/lib/tinymce/` *(tải về)*
- **Sửa**: `site.css` (bỏ khối `:root`), `_Layout.cshtml` (nạp `tokens.css`), `Controllers/ContactController.cs` (dùng `IMediaService`), `Program.cs`, `appsettings.json`, `.gitignore`, `CLAUDE.md`

> **Kéo-thả:** dùng **SortableJS** (~40KB, không phụ thuộc, tải về `wwwroot/lib/sortablejs/`). Tự viết drag-drop cho cây 2 cấp là cái bẫy thời gian kinh điển — đây là chỗ đáng dùng thư viện. Vẫn phải kèm phương án bàn phím (mục 7).

---

## 9. Bẫy kỹ thuật (đọc kỹ — phần lớn là lỗi đã gặp thật trong dự án này)

1. **Vòng phụ thuộc DI**: `AppDbContext → ICacheInvalidator → SiteContentService → AppDbContext`. Phải cắt bằng `IServiceScopeFactory`/`Lazy<T>` trong `CacheInvalidator`, nếu không app sẽ chết ngay lúc khởi động với `InvalidOperationException: A circular dependency was detected`.
2. **Global query filter + `Include`**: entity có filter được Include từ entity không filter qua quan hệ **required** sẽ sinh warning và có thể mất dữ liệu. Rà lại toàn bộ query hiện có sau khi bật soft delete — **đặc biệt `OrderItem → Product`**.
3. **`ExecuteDelete`/`ExecuteUpdate` không đi qua `SaveChanges`** ⇒ không có audit, không có soft delete, **không tự xóa cache**. Chỉ dùng ở xóa vĩnh viễn và phải invalidate tay.
4. **Slug của bản ghi trong thùng rác vẫn giữ unique index** ⇒ kiểm slug phải `IgnoreQueryFilters()`, báo lỗi rõ nguyên nhân.
5. **`IConfiguration` binder NỐI THÊM vào collection có sẵn** (Lưu ý 6.14 — đã dính ở Giai đoạn 5): mọi mảng trong Options phải mặc định `[]`.
6. **Repeater phải đánh lại index field liên tục** (`OptionGroups[0].Values[1].Value`). Xóa hàng giữa mà không đánh lại ⇒ model binder **dừng ở chỗ đứt index**, các dòng sau biến mất âm thầm.
7. **AJAX POST phải gửi anti-forgery token** qua header `RequestVerificationToken` (cơ chế đã có sẵn từ `cart.js` Giai đoạn 4). Thiếu ⇒ 400 khó hiểu.
8. **Listener submit của mình chạy TRƯỚC jquery-validation** ⇒ phải `if (window.jQuery && jQuery(form).data("validator") && !jQuery(form).valid()) return;` rồi mới disable nút (bẫy đã gặp 2 lần: `checkout.js`, `contact.js`).
9. **Form có `action` viết tay thì FormTagHelper không tự thêm token** ⇒ dùng `asp-antiforgery="true"`, và **đừng thêm cả `@Html.AntiForgeryToken()`** (2 token trùng tên làm hỏng validate).
10. **`asp-for="Input.X"` sinh `name="Input.X"`** ⇒ action phải `[Bind(Prefix = "Input")]`, không thì ModelState key lệch và lỗi server không hiện đúng ô (bẫy Giai đoạn 5).
11. **Mọi `[Required]`/`[Range]` phải có ErrorMessage tiếng Việt**, không thì lọt chuỗi `"The X field is required."` vào HTML.
12. **Upload cần `enctype="multipart/form-data"`**, thiếu thì `IFormFile` luôn null.
13. **CSS của partial dùng chung phải ở file CSS mà MỌI trang dùng nó đều nạp** (Giai đoạn 4 đã vỡ trang giỏ hàng vì `.rz-breadcrumb` để nhầm trong `product.css`). Với admin: partial ở `Areas/Admin/Views/Shared` ⇒ CSS phải ở `admin.css`.
14. **Dừng `RenewZone.Web.exe` trước khi build** (khóa DLL). Chạy migration khi app đã tắt.
15. **Đổi `SiteSettings` từ Admin nhưng seed vẫn chạy mỗi lần khởi động** — seed hiện idempotent **theo từng key** nên **không ghi đè** giá trị admin đã sửa (chỉ thêm key còn thiếu). Xác nhận lại điều này khi test, đừng để admin sửa xong restart lại bị mất (Lưu ý 6.13).
16. **`[Authorize]` phải ở `BaseAdminController`, không phải từng controller.** Quên gắn một controller là hở toàn bộ màn đó. Sau khi làm xong, **test bằng curl không cookie: mọi URL `/admin/*` phải trả 302 về `/admin/login`.**
17. **`DashboardController` hiện KHÔNG có `[Authorize]`** (chủ đích từ Giai đoạn 0) — nhớ bỏ dòng TODO và cho kế thừa base.

---

## 10. Tiêu chí nghiệm thu

### Chung (mỗi đợt đều phải đạt)
- [ ] `dotnet build` **0 warning / 0 error**; `dotnet run` không exception trong log.
- [ ] Web khách **không hồi quy**: `/`, `/iphone`, `/san-pham`, `/tim-kiem?q=`, slug SP, `/gio-hang`, `/dat-hang`, `/tin-tuc`, `/lien-he`, `/gioi-thieu`, `/sua-chua` đều 200; URL rác vẫn 404.
- [ ] Luồng mua hàng end-to-end còn nguyên: thêm giỏ → đặt hàng → có Order trong DB.

### Đợt A
- [ ] **Chưa đăng nhập: mọi `/admin/*` đều 302 về `/admin/login`** (test bằng curl không cookie — đây là kiểm tra quan trọng nhất của đợt này).
- [ ] Đăng nhập sai 5 lần → bị khóa, thông báo tiếng Việt rõ ràng.
- [ ] `returnUrl=https://evil.com` **không** redirect ra ngoài.
- [ ] Soft delete: xóa 1 sản phẩm → biến mất khỏi web khách **và** khỏi danh sách admin, nhưng `SELECT * FROM Products WHERE IsDeleted=1` vẫn thấy; đơn hàng cũ chứa sản phẩm đó **vẫn hiển thị đúng tên/giá snapshot**.
- [ ] **Cache tự động**: sửa 1 sản phẩm trong admin → mở web khách **ngay lập tức** (không đợi 5 phút, không restart) thấy giá mới. Làm tương tự với `SiteSetting`, `MenuItem`, `ContentBlock`.
- [ ] Slug: tạo sản phẩm có slug trùng slug một danh mục → **bị chặn**, báo rõ "đang được dùng bởi danh mục «iPhone»".
- [ ] Đổi trạng thái đơn: `New → Shipping` (bước nhảy sai) bị **server** từ chối kể cả khi gọi thẳng bằng curl.
- [ ] Dashboard: số liệu khớp `sqlcmd`; biểu đồ vẽ đúng; không có dữ liệu vẫn không vỡ.

### Đợt B
- [ ] Tạo sản phẩm mới đủ 5 tab → **kiểm bằng sqlcmd**: Product + ProductImages + OptionGroups + OptionValues đều đúng; mở `/{slug}` trên web khách thấy đầy đủ.
- [ ] Upload 3 ảnh cùng lúc, kéo đổi thứ tự, đặt ảnh chính → F5 vẫn đúng thứ tự; ảnh chính hiện đúng trên card sản phẩm.
- [ ] Sửa biến thể (thêm nhóm, xóa 1 giá trị ở giữa, đổi mặc định) → PDP đổi giá đúng, **thêm vào giỏ tính đúng giá** (Lưu ý 5c).
- [ ] Upload file `.exe` hoặc quá dung lượng → bị chặn **ở server** (test bằng curl bỏ qua JS).
- [ ] Xóa danh mục còn sản phẩm → **bị chặn** với thông báo tiếng Việt, không văng exception.
- [ ] Bật "Giá liên hệ" → PDP chỉ còn nút gọi hotline, không thêm giỏ được (Lưu ý 6.5).

### Đợt C
- [ ] Sửa 1 mục menu → menu web khách đổi ngay; thêm mục mới trỏ slug không tồn tại → admin **cảnh báo 404**.
- [ ] Tắt hết `ContentBlock` một nhóm → khối đó **ẩn hẳn** trên web khách, không để khung trống (luật fallback mục 4.7).
- [ ] Xóa rỗng 1 `SiteSetting` → web khách dùng fallback, **không hiện chuỗi rỗng hay "null"**; restart app **không** làm mất giá trị admin đã sửa.
- [ ] Gửi form `/lien-he` từ web khách → xuất hiện trong hộp thư admin, mở ra tự đánh dấu đã đọc, file đính kèm xem được.
- [ ] Thùng rác: xóa → khôi phục → bản ghi trở lại web khách; xóa vĩnh viễn → mất hẳn khỏi DB.

### Chất lượng giao diện (cả 3 đợt)
- [ ] Kiểm **375 / 768 / 1280 / 1920px**: không tràn ngang, bảng không vỡ, sidebar hoạt động đúng ở mọi mốc.
- [ ] `prefers-reduced-motion`: tắt hiệu ứng, **chức năng vẫn đủ**.
- [ ] Chỉ dùng bàn phím: đăng nhập → điều hướng sidebar → mở form → lưu → đóng modal, **không bị kẹt focus** ở đâu.
- [ ] Không có định nghĩa CSS trùng giữa `tokens.css` / `site.css` / `admin.css`.
- [ ] Tương phản chữ đạt AA ở sidebar và mọi badge trạng thái.

---

## 11. Thứ tự thực hiện gợi ý

**Đợt A**
1. `tokens.css` (cắt từ `site.css`) → smoke test web khách **trước khi làm gì khác**.
2. `ISoftDeletable` + query filter + migration → chạy lại web khách, chắc chắn không vỡ.
3. `ICacheInvalidator` + hook `SaveChanges` → test bằng cách sửa DB qua sqlcmd? Không — test bằng đợt sau. Viết trước, kiểm sau.
4. `ISlugService` + `IMediaService` (refactor `ContactController` dùng lại, **test lại form liên hệ ngay**).
5. `BaseAdminController` + login + `_LoginLayout` → **test chặn truy cập bằng curl ngay tại đây**.
6. `admin.css` + `admin.js` + `_AdminLayout` (sidebar/topbar/toast) — dựng khung trước, có khung rồi mọi màn sau ráp rất nhanh.
7. Dashboard → Đơn hàng. Commit đợt A.

**Đợt B**: Danh mục (đơn giản hơn, làm trước để hoàn thiện các component form/table/sortable) → Sản phẩm (dùng lại toàn bộ component vừa tôi luyện). Commit.

**Đợt C**: Bài viết → Trang tĩnh/Banner/Đánh giá → Menu/Khối nội dung → Hộp thư → Cấu hình → Thùng rác. Commit.

> **Lời khuyên quan trọng:** đừng làm 15 màn hình theo kiểu mỗi màn một cách. Dựng **thư viện partial dùng chung** ở bước A6 (`_AdminTable`, `_AdminPageHeader`, `_ConfirmModal`, `_ToggleSwitch`, `_SlugInput`, `_EmptyState`, `_AdminPagination`) rồi mọi màn sau chỉ là ráp lại. Đó là khác biệt giữa admin "làm cho xong" và admin **nhất quán, dễ mở rộng**.

---

## 12. Kim chỉ nam thẩm mỹ

Admin đẹp **không phải** vì nhiều hiệu ứng, mà vì:

- **Trật tự thị giác**: mọi màn cùng một nhịp — page header giống nhau, bảng giống nhau, form giống nhau. Người dùng học một lần, dùng được mọi nơi.
- **Phản hồi tức thì**: bấm gì cũng thấy có chuyện xảy ra trong 100ms (spinner, skeleton, toast). Không bao giờ để người dùng tự hỏi "nó có nhận không?".
- **Nói cho người dùng biết chuyện gì đang xảy ra**: lỗi phải nói *tại sao* và *phải làm gì*, không phải "Có lỗi xảy ra". Xóa được/không được phải nói rõ vì sao.
- **Khoảng thở**: dữ liệu dày không có nghĩa là chật. Padding rộng rãi, nhóm thông tin rõ ràng, một màn chỉ có **một** hành động chính (nút primary duy nhất).
- **Màu có ý nghĩa**: hồng brand cho hành động chính và điều hướng; đỏ **chỉ** dành cho phá hủy và cảnh báo; xanh cho thành công. Không tô màu để trang trí.

Nếu phải chọn giữa "thêm một animation" và "làm thông báo lỗi rõ ràng hơn" — **luôn chọn thông báo lỗi**.

Sau khi xong, một người chưa từng thấy trang này phải **tự thêm được một sản phẩm mới lên website mà không cần ai hướng dẫn**. Đó là thước đo cuối cùng.
