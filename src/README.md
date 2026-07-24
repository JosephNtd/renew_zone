# RenewZone Clone

Website thương mại điện tử bán lẻ sản phẩm Apple (mới & đã qua sử dụng) — clone lại **https://renewzone.vn/** phục vụ mục đích **học tập**. Xây bằng **ASP.NET Core MVC (.NET 9)** theo kiến trúc 3 tầng, gồm **web khách hàng đầy đủ** + **trang quản trị (Admin) 22 màn** cho chủ shop tự vận hành.

>  **Dự án học tập** — clone cấu trúc/bố cục/tính năng, KHÔNG dùng ảnh, logo, nội dung hay thông tin doanh nghiệp thật. Toàn bộ dữ liệu mẫu là giả lập (hotline `0900.000.000`, "RenewZone Demo"...).

---

## Tính năng chính

### Web khách hàng
- **Trang chủ** đầy đủ: banner slider, khối danh mục, section sản phẩm theo danh mục, ưu đãi đặc biệt, bán chạy, đánh giá khách hàng, tin công nghệ.
- **Danh mục sản phẩm** 2 cấp (danh mục cha → series con) với tab lọc theo dòng máy, phân trang.
- **Chi tiết sản phẩm**: gallery ảnh, bộ chọn biến thể (dung lượng/màu/tình trạng/SIM) đổi giá realtime, thông số kỹ thuật, sản phẩm liên quan.
- **Giỏ hàng** dựa trên Session + **đặt hàng khách (guest checkout)** không cần đăng nhập, luồng PRG chống đặt trùng.
- **Tin tức** (listing + chi tiết), **trang tĩnh** (giới thiệu, chính sách), **liên hệ** (form + upload file an toàn + chống spam).
- **URL slug phẳng** ngay tại root (bám sát site gốc): `/iphone`, `/gioi-thieu`, `/iphone-16-pro-max-256-gb`...

### Trang quản trị (`/admin`)
Dashboard · Đơn hàng · Danh mục · Sản phẩm (+ ảnh + biến thể) · Bài viết · Chuyên mục tin · Trang tĩnh · Banner · Đánh giá · Menu · Khối nội dung · Hộp thư liên hệ · Cấu hình site · Thùng rác. Có soft-delete, rich text editor, kéo-thả sắp xếp, upload ảnh, cache tự động vô hiệu hóa khi lưu.

Tài khoản seed sẵn: **`admin` / `Admin@123`**.

---

## Stack công nghệ

| Hạng mục | Công nghệ |
|---|---|
| Framework | **ASP.NET Core MVC**, **.NET 9** (`net9.0`) |
| Ngôn ngữ | C# (nullable + implicit usings bật) |
| Database | **SQL Server** + **EF Core 9.0.13** (code-first, migrations) |
| Auth | **ASP.NET Core Identity** (role `Admin`) — chỉ cho quản trị; khách mua hàng không cần tài khoản |
| Giỏ hàng | Session-based (không lưu DB) |
| UI | **Bootstrap 5** + custom CSS (design tokens) |
| JS | jQuery + jQuery Validation |
| Carousel | **Swiper** (local, không CDN) |
| Rich text editor | **TinyMCE 7.6.1** (local) |
| Kéo-thả | **SortableJS** |
| Icon | Bootstrap Icons |
| IDE / Môi trường | Windows 11, PowerShell, VS Code |

> Mọi package Microsoft.* **pin cứng version `9.0.13`** (bản 10.x chỉ hỗ trợ net10.0). Thư viện front-end tải về `wwwroot/lib/`, **không dùng CDN**.

---

## Kiến trúc — 3 tầng

Quy tắc phụ thuộc 1 chiều: **Web → Infrastructure → Domain** (không được ngược lại).

```
renew_zone/
├── CLAUDE.md                     # "Bộ nhớ" dự án — bối cảnh, quyết định kiến trúc, nhật ký tiến độ
├── README.md                     # File này
├── RenewZone.sln
├── docs/                         # DESIGN.md (design system) + PHASE*_PROMPT.md (kế hoạch từng giai đoạn)
└── src/
    ├── RenewZone.Domain/         # Nghiệp vụ THUẦN — KHÔNG phụ thuộc EF/ASP.NET
    │   ├── Common/               # BaseEntity, AuditableEntity
    │   ├── Entities/             # 16 entity (Category, Product, Order, NewsArticle...)
    │   ├── Enums/                # OrderStatus, PaymentMethod, BannerPosition...
    │   ├── Interfaces/           # Abstraction cho services
    │   └── Models/               # POCO (Cart, CartItem, OrderRequest, HomeContent...)
    ├── RenewZone.Infrastructure/ # EF Core + Identity (FrameworkReference AspNetCore.App)
    │   ├── Data/                 # AppDbContext, Configurations/, Migrations/, Seed/
    │   ├── Identity/             # ApplicationUser, AppRoles
    │   ├── Services/             # Implementation service nghiệp vụ
    │   └── DependencyInjection.cs# AddInfrastructure() — điểm đăng ký DI tầng này
    └── RenewZone.Web/            # MVC
        ├── Areas/Admin/          # Trang quản trị (controllers + views + layout riêng)
        ├── Controllers/          # Home, Product, Cart, Checkout, News, Contact, Slug
        ├── Services/             # Service thuần web (CartService session, MediaService)
        ├── ViewComponents/
        ├── Views/
        └── wwwroot/              # css, js, images, uploads, lib (bootstrap/jquery/swiper/tinymce...)
```

### Điểm kiến trúc
- **Routing slug phẳng**: route tĩnh (attribute routing) thắng fallback `{slug}` nhờ độ ưu tiên; `SlugResolver` cache map slug→loại, tra Category → Product → News → StaticPage. Slug unique **xuyên 4 bảng**, validate ở Admin.
- **Cache tự động vô hiệu hóa**: `AppDbContext.SaveChanges` gom loại entity vừa ghi rồi gọi `ICacheInvalidator` — controller Admin **không cần** nhớ gọi `Invalidate()`.
- **Soft delete + Thùng rác**: `ISoftDeletable` + global query filter dựng bằng reflection; `Remove()` biến thành UPDATE. Order/OrderItem/SiteSetting không xóa mềm (sổ sách không được biến mất).
- **Bảo mật Admin**: `BaseAdminController` gắn sẵn `[Authorize(Roles=Admin)]` + `[AutoValidateAntiforgeryToken]` — mọi controller admin mặc định an toàn.
- **Khử hardcode nội dung**: chuỗi "nội dung" nằm ở DB (`SiteSetting`/`MenuItem`/`ContentBlock`), "nhãn chức năng" hardcode trong view, "cấu hình kỹ thuật" ở `appsettings.json`. Có luật fallback bắt buộc (thiếu dữ liệu không được vỡ layout).

---

## Yêu cầu môi trường

- **.NET SDK 9** (`9.0.309`)
- **SQL Server** 
- `dotnet-ef` (global tool) nếu cần chạy migration thủ công

Connection string ở [src/RenewZone.Web/appsettings.json](src/RenewZone.Web/appsettings.json):

```
Server=localhost;Database=RenewZoneClone;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

---

## Chạy dự án

```powershell
# 1. Khôi phục package
dotnet restore

# 2. Chạy web (startup project là RenewZone.Web)
dotnet run --project src/RenewZone.Web
```

- Database **tự tạo + tự seed** khi app khởi động (`DbInitializer` — idempotent, chạy migrate + seed dữ liệu mẫu).
- Web khách: `https://localhost:<port>/` · Trang quản trị: `https://localhost:<port>/admin` (đăng nhập `admin` / `Admin@123`).

### Migrations

DbContext nằm ở `Infrastructure`, startup project là `Web` — phải chỉ rõ cả hai:

```powershell
# Thêm migration mới
dotnet ef migrations add <Ten> -p src/RenewZone.Infrastructure -s src/RenewZone.Web -o Data/Migrations

# Cập nhật database
dotnet ef database update -p src/RenewZone.Infrastructure -s src/RenewZone.Web

# Seed lại từ đầu (xóa DB rồi chạy lại app)
dotnet ef database drop -p src/RenewZone.Infrastructure -s src/RenewZone.Web
```

---

## Dữ liệu mẫu (seed)

18 danh mục · 22 sản phẩm (có biến thể + sản phẩm "Liên hệ") · 8 bài viết · 12 trang tĩnh · 5 banner · 5 đánh giá · SiteSettings · tài khoản admin. Seed **idempotent theo từng key/bảng** nên thêm dữ liệu mới ở giai đoạn sau không cần drop DB.

---

## Trạng thái dự án

Đã hoàn thành **Giai đoạn 0 → 6**: web khách đầy đủ + trang quản trị 22 màn.

Đang tới **Giai đoạn 7 — Hoàn thiện**: SEO (meta, sitemap), trang lỗi 404/500 tùy chỉnh, tối ưu ảnh (resize khi upload), kiểm thử end-to-end.