# CLAUDE.md — Dự án RenewZone Clone (ASP.NET Core MVC)

> **Mục đích file này:** Là "bộ nhớ" của dự án. Session sau đọc file này là nắm được toàn bộ bối cảnh, không cần phân tích lại từ đầu.
> **Quy tắc bắt buộc:** Sau khi hoàn thành bất kỳ task nào, PHẢI cập nhật lại file này (tick checkbox ở mục 5, ghi thêm dòng vào Nhật ký tiến độ ở mục 8, và bổ sung ghi chú phát sinh nếu có).

---

## 1. Bối cảnh dự án

- **Mục tiêu:** Sao chép (clone) lại website **https://renewzone.vn/** bằng ASP.NET — đây là dự án học tập (thư mục `d:\Learn\lam_viec\renew_zone`).
- **Website gốc:** Cửa hàng bán lẻ sản phẩm Apple (mới & đã qua sử dụng) tại TP.HCM: iPhone, MacBook, iPad, Apple Watch, AirPods, hàng thanh lý. Kèm dịch vụ **thu cũ đổi mới (trade-in)**, **sửa chữa**, trả góp 0%, bảo hành dài hạn.
- **Trạng thái hiện tại:** Đã xong **Giai đoạn 0 + 1 + 2 + 3** — database `RenewZoneClone` đầy đủ 13 bảng + Identity; trang chủ 7 section; **SlugController + fallback routing chạy tốt**: trang danh mục (tab series, phân trang), chi tiết sản phẩm (gallery, variant đổi giá, tabs, liên quan), tìm kiếm `/tim-kiem`, `/san-pham`, trang tĩnh + tin chi tiết render qua slug. Tiếp theo: **Giai đoạn 4** (Giỏ hàng & Đặt hàng — nút PDP hiện mới là placeholder). Link còn 404 (đúng kế hoạch): `/tin-tuc` (listing, Giai đoạn 5), `/sua-chua` + `/sua-chua-phan-cung` (chưa seed StaticPage), `/gio-hang` + `/lien-he` (Giai đoạn 4/5).
- Đã là git repository — đã commit Giai đoạn 0-2 (1 commit) + Giai đoạn 3.
- **Tài khoản admin seed sẵn:** username `admin` / mật khẩu `Admin@123` (email `admin@renewzone-demo.local`).

## 2. Stack công nghệ (ĐÃ CHỐT với user — không hỏi lại)

| Hạng mục | Lựa chọn |
|---|---|
| Framework | **ASP.NET Core MVC**, .NET 9 (máy có SDK 9.0.309; SDK 7.0.410 cũng có nhưng không dùng) |
| Phạm vi | **Web khách hàng + Trang Admin** (Area `Admin`) |
| Database | **SQL Server + EF Core** (code-first, migrations) |
| SQL Server trên máy | Service `MSSQLSERVER` đang chạy (full SQL Server, dùng `Server=localhost`) + LocalDB `MSSQLLocalDB` dự phòng |
| Connection string gợi ý | `Server=localhost;Database=RenewZoneClone;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true` |
| CSS | Bootstrap 5 + custom CSS để bám sát giao diện gốc |
| JS | jQuery + thư viện carousel (Swiper hoặc Owl Carousel) — site gốc dạng truyền thống, không SPA |
| Auth Admin | ASP.NET Core Identity (role `Admin`), chỉ dùng cho quản trị; khách mua hàng KHÔNG cần tài khoản (guest checkout) |
| Giỏ hàng | Session-based (nhớ bật `AddSession` + `UseSession`) |
| Môi trường | Windows 11, PowerShell, VS Code |

## 3. Phân tích website gốc (đã khảo sát ngày 2026-07-17)

### 3.1 Sơ đồ trang & cấu trúc URL

Site gốc dùng **slug phẳng ngay tại root** (không có prefix `/san-pham/` hay `/tin-tuc/` cho trang con):

| Loại trang | URL mẫu |
|---|---|
| Trang chủ | `/` |
| Giới thiệu (trang tĩnh) | `/gioi-thieu` |
| Danh mục sản phẩm | `/san-pham`, `/iphone`, `/macbook`, `/ipad`, `/apple-watch`, `/airpods`, `/hang-thanh-ly` |
| Chi tiết sản phẩm | `/iphone-16-pro-max-256-gb`, `/macbook-air-m3-15-inch-16gb-256gb-753` (một số slug có hậu tố id số) |
| Sửa chữa | `/sua-chua`, `/sua-chua-phan-cung` |
| Chính sách (trang tĩnh) | `/chinh-sach`, `/chinh-sach-thanh-toan`, `/chinh-sach-kiem-hang`, `/chinh-sach-mua-hang1`, `/chinh-sach-bao-hanh`, `/chinh-sach-van-chuyen`, `/chinh-sach-bao-mat`, `/chinh-sach-tra-gop`, `/bang-gia-thu-cu-doi-moi-thang-6`, `/may-moi`, `/may-da-qua-su-dung`, `/bao-hanh-20-nam-tai-rewzone` |
| Tin tức (listing) | `/tin-tuc` |
| Chi tiết bài viết | slug phẳng, vd `/apple-de-lo-ngay-ra-mat-iphone-17` |
| Giỏ hàng | `/gio-hang` |
| Liên hệ | `/lien-he` |

**Menu điều hướng chính:** Trang chủ · Giới thiệu · Sản phẩm (dropdown: iPhone, MacBook, iPad, Apple Watch, AirPods, Hàng thanh lý) · Sửa chữa (dropdown) · Chính sách (dropdown: Bảng giá thu cũ đổi mới, Máy mới, Máy đã qua sử dụng, Bảo hành 20 năm) · Tin công nghệ · Liên hệ.

### 3.2 Trang chủ — thứ tự các section

1. **Header:** logo + hotline `0983.468.860` + menu ngang (có dropdown) + ô tìm kiếm + icon giỏ hàng. Tagline: "Thu Đổi Thiết Bị - Lên Đời Đơn Giản".
2. **Banner slider** khuyến mãi (đang feature iPhone 17 Pro Max).
3. **Khối danh mục** (ảnh + link tới từng danh mục).
4. **Các section sản phẩm theo danh mục** (iPhone, MacBook, iPad...) — mỗi section là grid/carousel card sản phẩm.
5. **Khối ưu đãi đặc biệt** (special offers gallery).
6. **Sản phẩm bán chạy** (carousel).
7. **Đánh giá khách hàng** (testimonials carousel).
8. **Tin công nghệ** (bài viết mới).
9. **Footer:** địa chỉ, giờ mở cửa 8h30–21h30, link chính sách, social (Zalo/Facebook/Maps), thông tin đăng ký kinh doanh (MST).
10. **Nút nổi (floating):** gọi điện, Zalo chat, Facebook Messenger.

### 3.3 Card sản phẩm (dùng chung toàn site)

Ảnh · badge **"Trả góp 0%"** · tên sản phẩm · giá gốc (gạch ngang) · giá khuyến mãi (nổi bật, đỏ) · hoặc chữ **"Liên hệ"** nếu không công khai giá.

### 3.4 Trang danh mục (vd `/iphone`)

- Breadcrumb: `Trang chủ > Sản phẩm mới > IPHONE`.
- **Tab lọc theo dòng (series)** phía trên: IPHONE 11 SERIES → IPHONE 17 SERIES (⇒ danh mục có 2 cấp: danh mục cha → series con).
- Grid sản phẩm 3–4 cột, **phân trang số** (`Trang 1/5`, nút Next/Last).
- Không có sidebar filter.

### 3.5 Trang chi tiết sản phẩm

- Gallery ảnh + thumbnails.
- Tên, giá gốc/giá sale.
- **Bộ chọn biến thể (variants) — điểm phức tạp nhất:** Dung lượng (256GB/512GB/1TB) · Màu sắc (Titan Đen/Trắng/Tự nhiên/Sa mạc) · Tình trạng máy (98% / 99%) · Loại SIM (LL/A hay chính hãng VN).
- Nút hành động: **Thêm giỏ hàng**, **Mua ngay**, nút gọi hotline, khối trả góp 0%.
- Khối cam kết (hàng chính hãng, thu cũ đổi mới, bảo hành).
- **Tab nội dung:** thông số kỹ thuật (camera & màn hình, pin & sạc, thiết kế & chất liệu) · mô tả dài (rich text) · bình luận/đánh giá.
- Carousel **sản phẩm liên quan** cuối trang.

### 3.6 Tin tức

- Listing `/tin-tuc`: khối bài **nổi bật dạng grid** trên cùng (thumbnail lớn ~500x250) + **danh sách dọc** bên dưới (thumbnail trái ~320x240, tiêu đề, ngày giờ đăng, đoạn trích 2–3 câu, tag chuyên mục vd "Khoa học công nghệ", "Thiết bị"). Không sidebar.
- Chi tiết bài viết: slug phẳng ở root.

### 3.7 Liên hệ `/lien-he`

- Thông tin cửa hàng + **form liên hệ**: Họ tên, SĐT, Địa chỉ, Email, Chủ đề, Nội dung, **đính kèm file**.
- Bản đồ (Google Maps embed/link).

### 3.8 Giỏ hàng & đặt hàng `/gio-hang`

- Giỏ trống hiển thị: "Không tồn tại sản phẩm nào trong giỏ hàng !" + link "Về trang chủ".
- Có giỏ hàng chức năng ở header. Checkout dạng khách (không login). Thanh toán: tham chiếu trang Chính Sách Thanh Toán (COD/chuyển khoản — xác nhận lại khi làm Giai đoạn 4).

### 3.9 Thông tin cửa hàng (site gốc — chỉ để tham khảo, xem Lưu ý 6.2)

Địa chỉ: 258L Dương Quảng Hàm, P.5 (P.An Nhơn), Gò Vấp, TP.HCM · Hotline: 0983.468.860 · Giờ: 8h30–21h30 · MST: 079094010343.

## 4. Thiết kế hệ thống

### 4.1 Cấu trúc solution (ĐÃ TẠO — kiến trúc 3 tầng, nâng cấp so với kế hoạch 1-project ban đầu theo yêu cầu user "scalable như senior")

```
renew_zone/
├── CLAUDE.md                    ← file này
├── RenewZone.sln
├── .gitignore                   ← dotnet template
└── src/
    ├── RenewZone.Domain/        ← THUẦN nghiệp vụ, KHÔNG phụ thuộc EF/ASP.NET
    │   ├── Common/              ← BaseEntity (Id), AuditableEntity (CreatedAt/UpdatedAt)
    │   ├── Entities/            ← (Giai đoạn 1) Category, Product, Order...
    │   ├── Enums/               ← (Giai đoạn 1) OrderStatus, PaymentMethod, BannerPosition...
    │   └── Interfaces/          ← abstraction cho services (ISlugService...)
    ├── RenewZone.Infrastructure/ ← EF Core + Identity, có FrameworkReference Microsoft.AspNetCore.App
    │   ├── Data/AppDbContext.cs ← IdentityDbContext<ApplicationUser>, tự gán audit khi SaveChanges,
    │   │                          ApplyConfigurationsFromAssembly (quét Configurations/)
    │   ├── Data/Configurations/ ← (Giai đoạn 1) IEntityTypeConfiguration từng entity
    │   ├── Data/Seed/           ← (Giai đoạn 1) SeedData
    │   ├── Identity/            ← ApplicationUser (IdentityUser + FullName), AppRoles.Admin
    │   ├── Services/            ← implementation các service nghiệp vụ
    │   └── DependencyInjection.cs ← AddInfrastructure(): điểm đăng ký DI duy nhất của tầng này
    └── RenewZone.Web/           ← MVC, phụ thuộc Infrastructure + Domain
        ├── Areas/Admin/         ← DashboardController + _AdminLayout (skeleton, chưa [Authorize])
        ├── Controllers/         ← Home (template), sẽ thêm Product, Cart, News, Contact, Slug
        ├── Models/ViewModels/
        ├── Services/            ← service thuần web (CartService session...)
        ├── ViewComponents/
        ├── Views/
        └── wwwroot/             ← css, js, images/, uploads/, lib (bootstrap+jquery có sẵn)
```

**Quy tắc phụ thuộc:** Web → Infrastructure → Domain (1 chiều, không được ngược lại). Sau này cần API cho mobile ⇒ thêm project `RenewZone.Api` dùng lại 2 tầng dưới.

### 4.2 Database schema (ĐÃ TẠO — migration `InitialCreate`, DB `RenewZoneClone` trên `localhost`)

**Hành vi xóa đã cấu hình (quan trọng khi làm Admin):**
- `Product → Category`: **Restrict** (không xóa được danh mục còn sản phẩm); `Category.Parent`: **Restrict**.
- `ProductImage/OptionGroup/OptionValue → Product`: **Cascade** (xóa sản phẩm kéo theo ảnh + options).
- `OrderItem → Order`: **Cascade**; `OrderItem → Product`: **SetNull** (xóa sản phẩm không mất lịch sử đơn — item sống bằng snapshot ProductName/UnitPrice/VariantDescription).
- Slug unique index TRONG từng bảng (Category/Product/NewsArticle/StaticPage); unique XUYÊN bảng vẫn phải validate bằng service ở Admin (Lưu ý 6.4).
- Giá dùng `decimal(18,0)` (VND không có phần thập phân). `SiteSetting` khóa chính là `Key` (string), có class hằng `SettingKeys`.
- `AuditableEntity` (CreatedAt/UpdatedAt tự gán): Category, Product, Order, NewsArticle, StaticPage, ContactMessage. Còn lại là `BaseEntity` (chỉ Id).
- `ProductOptionValue` có thêm `IsDefault` (option chọn sẵn khi mở trang) — bổ sung so với thiết kế ban đầu.

| Entity | Trường chính | Ghi chú |
|---|---|---|
| `Category` | Id, Name, Slug, ParentId?, ImageUrl, DisplayOrder, IsActive | 2 cấp: cha (iPhone) → con (iPhone 16 Series) |
| `Product` | Id, CategoryId, Name, Slug, OriginalPrice, SalePrice?, IsContactPrice, ShortDesc, DescriptionHtml, SpecsHtml, IsInstallment0, IsFeatured, IsBestSeller, IsActive, ViewCount, CreatedAt | `IsContactPrice=true` ⇒ hiện "Liên hệ" |
| `ProductImage` | Id, ProductId, Url, DisplayOrder, IsMain | |
| `ProductOptionGroup` | Id, ProductId, Name, DisplayOrder | vd "Dung lượng", "Màu sắc", "Tình trạng", "Loại SIM" |
| `ProductOptionValue` | Id, GroupId, Value, PriceAdjustment | cộng/trừ vào giá khi chọn |
| `Order` | Id, Code, CustomerName, Phone, Email?, Address, Note?, PaymentMethod, Status, TotalAmount, CreatedAt | Status: New/Confirmed/Shipping/Completed/Cancelled |
| `OrderItem` | Id, OrderId, ProductId?, ProductName, VariantDesc, UnitPrice, Quantity | snapshot tên/giá tại thời điểm đặt |
| `NewsArticle` | Id, Title, Slug, ThumbnailUrl, Excerpt, ContentHtml, CategoryTag, IsFeatured, PublishedAt, ViewCount | CategoryTag string đơn giản là đủ |
| `StaticPage` | Id, Title, Slug, ContentHtml, UpdatedAt | giới thiệu + toàn bộ trang chính sách |
| `Banner` | Id, ImageUrl, LinkUrl, Position, DisplayOrder, IsActive | Position: HomeSlider... |
| `Testimonial` | Id, CustomerName, AvatarUrl, Content, DisplayOrder, IsActive | |
| `ContactMessage` | Id, FullName, Phone, Address?, Email, Subject, Content, AttachmentPath?, IsRead, CreatedAt | |
| `SiteSetting` | Key, Value | hotline, địa chỉ, giờ mở cửa, link social, MST... đọc ra layout |
| Identity tables | | ASP.NET Core Identity cho Admin |

### 4.3 Routing slug phẳng (quyết định kiến trúc quan trọng)

Bám theo site gốc: slug nằm ngay root. **ĐÃ TRIỂN KHAI (Giai đoạn 3):**
1. Route tĩnh 1 segment dùng **attribute routing** để thắng fallback slug nhờ độ ưu tiên (literal > tham số): `/san-pham` + `/tim-kiem` (`ProductController`). Route `/`, `/admin/...` giữ conventional. Các route tĩnh còn lại (`/gio-hang`, `/lien-he`, `/tin-tuc`, `/dat-hang`) sẽ thêm ở giai đoạn sau.
2. **Fallback route `{slug}`** (đăng ký CUỐI trong Program.cs, 1 segment nên không nuốt `/admin/x`) → `SlugController.Resolve(slug, series?, page?)`. Tra qua `ISlugResolver` — nạp toàn bộ map slug→(loại,Id) của 4 bảng vào Dictionary + cache `IMemoryCache` 5 phút (không query 4 bảng mỗi request rác). Khớp loại nào render view loại đó; không khớp → 404. Admin sửa slug phải gọi `ISlugResolver.Invalidate()`.
3. Slug phải **unique xuyên suốt 4 bảng** — validate khi tạo/sửa trong Admin (viết service check chung). `SlugResolver` `TryAdd` theo thứ tự ưu tiên Category→Product→News→StaticPage nên kể cả lỡ trùng vẫn tất định.
4. Query catalog: `ICatalogQueryService` (danh mục/chi tiết/tìm kiếm/tất cả SP, KHÔNG cache) + `IContentQueryService` (StaticPage/NewsDetail). PDP dùng `AsSplitQuery()` khi Include 2 collection (Images + OptionGroups) — tránh cartesian + cảnh báo EF.

### 4.4 Thiết kế UI (ĐÃ CHỐT — xem chi tiết `docs/DESIGN.md`)

Bản design system đầy đủ nằm ở **`docs/DESIGN.md`** — Giai đoạn 2 trở đi PHẢI bám theo file này. Tóm tắt:
- **Giữ DNA site gốc, tinh chỉnh premium:** hồng brand `--brand-500 #CE5F9A` (tinh chỉnh từ `#d78dba` gốc — đã trích từ CSS thật của site), đỏ giá/CTA `#EC2D3F` (giữ nguyên token gốc), nền `--page #FAF7FA`, chữ `#212529`.
- Font: Be Vietnam Pro (heading/giá) + Nunito Sans (body) + Pacifico (chỉ tagline). Icon: Bootstrap Icons. Carousel: **Swiper** duy nhất. Lightbox: GLightbox. Thư viện tải về `wwwroot/lib` (không CDN).
- Tokens đầy đủ (màu/spacing/radius/shadow/motion) viết sẵn dạng CSS variables trong DESIGN.md mục 2 — copy vào `site.css`.
- 19 component đã đặc tả (mục 3 DESIGN.md): topbar, header sticky shrink, mega menu, 5 loại button, 5 loại badge, product card (anatomy + hover), hero slider, series tabs, PDP (variant selector kiểu TGDĐ + giá count-up + sticky buy-bar mobile), giỏ hàng/checkout, tin tức, testimonials, footer, floating buttons (phone-ring), toast, admin theme riêng.
- 10 animation chính (mục 4): reveal-on-scroll (IntersectionObserver tự viết), fly-to-cart, badge-pop, quét sáng CTA... Quy tắc: chỉ animate transform/opacity + tôn trọng `prefers-reduced-motion`.
- Cấu trúc file css/js cho Giai đoạn 2: mục 6 DESIGN.md.

### 4.5 Định dạng & ngôn ngữ

- Toàn bộ UI tiếng Việt. Giá tiền định dạng `29.990.000đ` — dùng `CultureInfo("vi-VN")`, ví dụ `price.ToString("N0", viVN) + "đ"`.

## 5. Kế hoạch công việc (tick ✅ khi xong + ghi nhật ký mục 8)

### Giai đoạn 0 — Khởi tạo dự án ✅ (xong 2026-07-20)
- [x] `git init` + tạo `.gitignore` (dotnet template)
- [x] Tạo solution 3 tầng: `RenewZone.Domain` + `RenewZone.Infrastructure` + `RenewZone.Web` (net9.0, xem 4.1)
- [x] Cài packages **pin version 9.0.13**: EF Core SqlServer + Identity.EFCore (Infrastructure); EF Design + Tools (Web)
- [x] Cấu hình connection string, Session, Identity, route Area Admin (`Program.cs` + `DependencyInjection.cs`)
- [x] Skeleton Area Admin: Dashboard + `_AdminLayout` riêng; cấu trúc thư mục đầy đủ các tầng
- [x] Build 0 lỗi; chạy thử OK: `/` → 200, `/admin` → 200

### Giai đoạn 1 — Database & dữ liệu mẫu ✅ (xong 2026-07-20)
- [x] 13 entities + 3 enums (Domain) + 13 file `IEntityTypeConfiguration` (Infrastructure/Data/Configurations) + DbSets trong `AppDbContext`
- [x] Migration `InitialCreate` (Data/Migrations) + DB `RenewZoneClone` tạo tự động khi app start (`DbInitializer.InitializeAsync` trong Program.cs — idempotent)
- [x] SeedData: 18 danh mục (6 cha + 7 iPhone series + 2 MacBook + 3 iPad con), 22 sản phẩm (2 SP có đủ option groups demo variants, 1 SP "Liên hệ"), 8 bài viết, 12 trang tĩnh, 5 banner, 5 testimonial, 12 SiteSettings, admin `admin`/`Admin@123`
- [x] 4 ảnh placeholder SVG tự tạo trong `wwwroot/images/` (product/banner/news/avatar) — không copy ảnh site gốc
- [x] Verify bằng sqlcmd: đủ số dòng các bảng, admin gắn đúng role, iPhone 16 Pro Max có 4 nhóm option (3+4+2+2 giá trị)

### Giai đoạn 2 — Layout & Trang chủ ✅ (xong 2026-07-21)
- [x] `_Layout.cshtml` HOÀN CHỈNH theo DESIGN.md: topbar gradient · header sticky tự thu nhỏ (logo wordmark HTML/CSS, search pill, khối hotline, cart badge) · mainnav gradient hồng + dropdown 2 cấp (panel + flyout series, thuần CSS hover/focus-within) · offcanvas mobile (collapse 2 cấp + khối hotline) · footer tối 4 cột đọc SiteSettings · 3 nút nổi (phone-ring animation, Zalo, Messenger) · back-to-top · toast stack. Responsive đủ 3 breakpoint (search tụt hàng 2 + hamburger < lg, ẩn hotline < xl, thu nhỏ nút nổi < sm)
- [x] `ISiteContentService` (Domain) + `SiteContentService` (Infrastructure, IMemoryCache TTL 5 phút, có `Invalidate()` cho Admin gọi sau) — layout `@inject`, KHÔNG query DB trực tiếp mỗi request
- [x] `site.css` (tokens DESIGN.md mục 2 + 14 nhóm component) · `site.js` (header shrink, back-to-top, reveal-on-scroll IntersectionObserver + stagger, `window.RZ.toast()` + `RZ.cartBadge()`) · Bootstrap Icons tải về `wwwroot/lib/bootstrap-icons/`
- [x] Trang chủ đủ 7 section theo mục 3.2: hero (Swiper fade + 2 banner phụ) · khối 6 tile danh mục · section sản phẩm theo từng danh mục cha · ưu đãi đặc biệt (IsFeatured, nền `--brand-50`) · bán chạy (Swiper carousel) · testimonials (Swiper) · tin công nghệ (4 bài mới). Dữ liệu qua `IHomeQueryService`/`HomeContent` → `HomeIndexViewModel`
- [x] Partial `_ProductCard.cshtml` (dùng chung, CSS card trong site.css mục 14 — reuse ở Giai đoạn 3) + `_SectionHeader.cshtml`; card dùng Bootstrap `.stretched-link` (tránh lồng `<a>`), sản phẩm "Liên hệ" chỉ có nút gọi hotline, không giá/không thêm giỏ

### Giai đoạn 3 — Trang sản phẩm ✅ (xong 2026-07-21)
- [x] SlugController + fallback routing (mục 4.3): `ISlugResolver` (cache map slug→loại 5 phút) → route `{slug}` cuối cùng trong Program.cs; tra Category→Product→News→StaticPage, không khớp 404
- [x] Trang danh mục (`Views/Slug/Category.cshtml`): breadcrumb, tab series (lọc `?series=`), grid tái dùng `_ProductCard`, phân trang số (`_Pagination`, 12 SP/trang)
- [x] Trang chi tiết (`Views/Slug/Product.cshtml`): gallery + thumbnails, bộ chọn variants đổi giá realtime (product.js), tabs thông số/mô tả, carousel liên quan (Swiper), khối cam kết. SP "Liên hệ" chỉ có nút gọi hotline
- [x] Tìm kiếm theo tên (`/tim-kiem?q=`, `ProductController.Search`) + `/san-pham` (tất cả SP) — attribute routing, khớp trước fallback slug. Ô search header đã trỏ đúng
- [x] Trang tĩnh + tin chi tiết render qua slug (`StaticPage.cshtml` + `NewsDetail.cshtml`) — làm sẵn vì SlugController cần (chồng sang Giai đoạn 5)

### Giai đoạn 4 — Giỏ hàng & Đặt hàng
- [ ] `CartService` (session JSON): thêm/sửa số lượng/xóa; item phân biệt theo product + variants đã chọn
- [ ] Trang `/gio-hang`: bảng items, cập nhật, trạng thái giỏ trống (đúng thông điệp gốc), badge số lượng trên header (cập nhật AJAX)
- [ ] Checkout: form khách (họ tên, SĐT, địa chỉ, email, ghi chú, phương thức thanh toán COD/chuyển khoản) → tạo Order + OrderItems → trang cảm ơn kèm mã đơn
- [ ] Nút "Mua ngay" ở trang chi tiết (thêm giỏ + chuyển thẳng giỏ hàng)

### Giai đoạn 5 — Tin tức, trang tĩnh, liên hệ
- [ ] `/tin-tuc`: khối nổi bật + danh sách (theo mục 3.6) — CÒN LẠI (listing chưa có, link header đang 404). Chi tiết bài viết + bài liên quan ĐÃ xong ở Giai đoạn 3 (`NewsDetail.cshtml`)
- [x] Render StaticPage theo slug (giới thiệu + các trang chính sách) — xong ở Giai đoạn 3 (`StaticPage.cshtml`). CÒN THIẾU 2 trang seed `/sua-chua` + `/sua-chua-phan-cung` (menu đang trỏ tới → 404)
- [ ] `/lien-he`: thông tin cửa hàng, form liên hệ (đủ field, upload file vào `wwwroot/uploads`, validate), map embed

### Giai đoạn 6 — Admin panel (Area `Admin`, route `/admin`)
- [ ] Đăng nhập Identity, chặn toàn Area bằng `[Authorize(Roles="Admin")]`
- [ ] Dashboard: đếm đơn mới, sản phẩm, bài viết, liên hệ chưa đọc
- [ ] CRUD Danh mục (2 cấp) · CRUD Sản phẩm (kèm upload nhiều ảnh, quản lý option groups/values, editor rich text — TinyMCE/Quill)
- [ ] Quản lý Đơn hàng: danh sách, lọc theo trạng thái, xem chi tiết, đổi trạng thái
- [ ] CRUD Bài viết, Trang tĩnh, Banner, Testimonial
- [ ] Xem ContactMessage (đánh dấu đã đọc), sửa SiteSettings

### Giai đoạn 7 — Hoàn thiện
- [ ] SEO: meta title/description theo trang, sitemap.xml đơn giản
- [ ] Trang lỗi 404/500 tùy chỉnh
- [ ] Rà soát responsive toàn site, tối ưu ảnh (resize khi upload)
- [ ] Kiểm thử end-to-end luồng chính: xem sản phẩm → chọn variant → thêm giỏ → đặt hàng → admin xử lý đơn

## 6. Lưu ý quan trọng

1. **Máy user:** Windows 11 + PowerShell 5.1 — không dùng `&&` khi gợi ý lệnh; SDK .NET 9; SQL Server full đang chạy tại `localhost`.
2. **Bản quyền & đạo đức (QUAN TRỌNG):** Đây là dự án học tập — clone **cấu trúc, bố cục, tính năng**. KHÔNG copy nguyên si ảnh, logo, bài viết của site gốc vào repo; dùng ảnh placeholder/nguồn tự do và nội dung mẫu tự viết. KHÔNG dùng thông tin thật của doanh nghiệp (địa chỉ, MST, chủ sở hữu) trong sản phẩm demo — thay bằng thông tin giả lập trong SeedData (vd hotline `0900.000.000`, "RenewZone Demo"). Không deploy công khai dưới danh nghĩa thương hiệu thật.
3. **Variants quyết định giá:** giá hiển thị = SalePrice + tổng PriceAdjustment của các option đã chọn; OrderItem lưu snapshot mô tả variant dạng text.
4. **Slug unique toàn cục** (4 bảng: Category/Product/NewsArticle/StaticPage) — bắt buộc validate ở Admin, nếu không fallback route sẽ nhập nhằng.
5. **Sản phẩm "Liên hệ"**: không cho thêm vào giỏ, chỉ hiện nút gọi hotline.
5b. **Nút "Thêm giỏ"/"Mua ngay" ở PDP hiện là placeholder** (product.js chỉ báo toast "Giỏ hàng đang được hoàn thiện") — nối vào `CartService` thật khi làm Giai đoạn 4.
6. Migrations (DbContext nằm ở Infrastructure, startup project là Web — phải chỉ rõ cả 2):
   `dotnet ef migrations add <Name> -p src/RenewZone.Infrastructure -s src/RenewZone.Web -o Data/Migrations`
   `dotnet ef database update -p src/RenewZone.Infrastructure -s src/RenewZone.Web`
   (máy ĐÃ có dotnet-ef 10.0.8 global — chạy tốt với EF Core 9 của project, không cần cài lại).
7. **NuGet phải pin version `9.0.13`** cho mọi package Microsoft.* — bản mới nhất trên nuget.org là 10.x chỉ hỗ trợ net10.0, cài không pin sẽ lỗi NU1202.
8. Session/Identity đã bật sẵn trong `Program.cs` từ Giai đoạn 0 (`UseAuthentication` → `UseAuthorization` → `UseSession`); cookie login Admin trỏ `/admin/login` (trang này Giai đoạn 6 mới làm).
9. `DashboardController` hiện CHƯA có `[Authorize]` (chủ đích — chưa có trang đăng nhập/seed admin); bắt buộc bật ở Giai đoạn 6, dùng hằng `AppRoles.Admin`.
10. Audit tự động: entity kế thừa `AuditableEntity` sẽ được `AppDbContext.SaveChanges` tự gán `CreatedAt`/`UpdatedAt` (giờ local) — không set tay trong code nghiệp vụ.
11. Seed chạy MỖI Lần app start (`DbInitializer` trong Program.cs) nhưng idempotent (mỗi hàm seed check bảng trống trước). Muốn seed lại từ đầu: xóa DB (`dotnet ef database drop -p src/RenewZone.Infrastructure -s src/RenewZone.Web`) rồi chạy lại app.
12. Dữ liệu seed dùng thông tin GIẢ LẬP đúng Lưu ý 6.2 (hotline `0900.000.000`, "RenewZone Demo", MST `0000000000`, mô tả/bài viết tự viết, ảnh SVG placeholder).

## 7. Quy ước làm việc cho các session sau

- Đọc file này trước khi làm bất cứ gì; làm theo đúng thứ tự giai đoạn ở mục 5 (task trong cùng giai đoạn có thể linh hoạt).
- Xong task nào → tick `[x]` task đó + thêm 1 dòng vào mục 8 (ngày, việc đã làm, quyết định/vấn đề phát sinh).
- Có quyết định kiến trúc mới hoặc thay đổi so với kế hoạch → cập nhật ngay vào mục 4/6, đừng để file lỗi thời.
- Ưu tiên chạy được từng giai đoạn (`dotnet build` + `dotnet run` kiểm tra tay) trước khi chuyển giai đoạn kế.

## 8. Nhật ký tiến độ

| Ngày | Nội dung |
|---|---|
| 2026-07-17 | Phân tích toàn bộ site gốc (trang chủ, danh mục, chi tiết SP, tin tức, liên hệ, giỏ hàng, giới thiệu). Chốt stack: ASP.NET Core MVC .NET 9 + SQL Server/EF Core + Admin panel. Tạo CLAUDE.md. Chưa có code. |
| 2026-07-20 | **Xong Giai đoạn 0.** Quyết định kiến trúc: nâng từ 1 project lên solution 3 tầng Domain/Infrastructure/Web (yêu cầu scalable của user) — cập nhật mục 4.1. Gặp lỗi NU1202 (NuGet lấy EF Core 10.x không hỗ trợ net9.0) → pin toàn bộ về 9.0.13 (Lưu ý 6.7). Infrastructure cần `FrameworkReference Microsoft.AspNetCore.App` để dùng AddIdentity. Build 0 lỗi, `/` và `/admin` đều 200. Chưa commit git. |
| 2026-07-20 | **Xong task Layout (Giai đoạn 2, phần 1).** `_Layout.cshtml` đầy đủ component + responsive theo DESIGN.md; `ISiteContentService` cached cấp menu/settings cho layout; site.css (tokens + 14 nhóm component + keyframes) + site.js (`window.RZ` API); Bootstrap Icons local. Verify curl: 20/20 component có trong HTML, menu/hotline đọc đúng từ DB. Trang chủ vẫn là placeholder — task kế: dựng section trang chủ + `_ProductCard`. Lưu ý: menu có link `/sua-chua`, `/sua-chua-phan-cung` nhưng CHƯA seed 2 trang tĩnh này (bổ sung ở Giai đoạn 5); mọi link slug sẽ 404 cho tới khi làm SlugController (Giai đoạn 3) — bình thường. |
| 2026-07-20 | **Chốt thiết kế UI** — tạo `docs/DESIGN.md` (theme tokens, 19 components, 10 animations, quy tắc a11y, cấu trúc file css/js). Đã trích xuất token màu/font THẬT từ CSS site gốc làm căn cứ (`--color-main #d78dba`, `--color-red #ec2d3f`, Nunito Sans/Mulish/Pacifico, Owl+Slick+Nivo → ta thay bằng Swiper duy nhất). Cập nhật mục 4.4. |
| 2026-07-20 | **Xong Giai đoạn 1.** 13 entities + 3 enums + 13 configurations (delete behaviors: Restrict/Cascade/SetNull có chủ đích — xem 4.2), migration `InitialCreate`, `DbInitializer` migrate+seed idempotent khi app start. Seed 22 SP/18 danh mục/8 bài viết/12 trang tĩnh/banners/testimonials/settings + admin `admin`/`Admin@123`. Verify sqlcmd OK. Bổ sung `ProductOptionValue.IsDefault`. dotnet-ef 10.0.8 global dùng được với EF 9. |
| 2026-07-21 | **Xong Giai đoạn 2 (trang chủ).** Thêm `IHomeQueryService`/`HomeQueryService` (cache IMemoryCache TTL **2 phút** — ngắn hơn SiteContentService 5 phút vì dữ liệu cấp trang đổi thường xuyên hơn) trả `HomeContent` (Domain/Models mới). Tải toàn bộ SP active 1 query rồi chia nhóm trong bộ nhớ (catalog nhỏ ⇒ tránh N+1) để dựng section theo danh mục + featured + bestseller. `HomeController.Index` async map sang `HomeIndexViewModel`. Dựng đủ 7 section + partial `_ProductCard`/`_SectionHeader`; card dùng `.stretched-link`. Thêm Swiper 11.2.10 (local `wwwroot/lib/swiper`) cho 3 carousel; `home.css` + `home.js` (tắt autoplay khi `prefers-reduced-motion`). CSS card đặt ở site.css (mục 14) vì Giai đoạn 3 dùng lại. Reveal-on-scroll đặt class thẳng trong markup (site.js quét `.reveal` lúc load, TRƯỚC home.js). Build 0 lỗi/0 cảnh báo; curl `/` = 200, đúng thứ tự 7 section, 34 card (SP "Liên hệ" iphone-17-pro-max hiện 2 lần ở iPhone + Ưu đãi, chỉ có nút gọi hotline). Ghi chú: 2 file Domain lúc đầu bị ghi nhầm vào `wwwroot/lib/src/` do dùng đường dẫn tương đối sau khi `cd` — đã chuyển về đúng chỗ. |
| 2026-07-21 | **Commit lần đầu** toàn bộ Giai đoạn 0-2 (161 file, `.gitignore` chặn sạch obj/bin). Lưu ý viết commit message qua Bash tool: KHÔNG dùng here-string PowerShell `@'...'@` (bị lọt ký tự `@` vào message), dùng heredoc `git commit -F - <<'EOF'`. |
| 2026-07-21 | **Xong Giai đoạn 3.** Thêm `ISlugResolver` (cache map slug→loại 5 phút, Dictionary OrdinalIgnoreCase, `TryAdd` theo thứ tự ưu tiên) + `ICatalogQueryService` (danh mục/PDP/search/all — không cache) + `IContentQueryService` (StaticPage/NewsDetail). `SlugController.Resolve(slug, series?, page?)` điều phối 4 loại; route `{slug}` đăng ký cuối Program.cs. `/san-pham` + `/tim-kiem` dùng attribute routing (thắng fallback). Views: `Slug/Category` (breadcrumb + tab series + grid + `_Pagination`), `Slug/Product` (gallery đổi ảnh, variant→giá realtime, tabs, related Swiper, cam kết), `Slug/StaticPage`, `Slug/NewsDetail`, `Product/All`, `Product/Search`. Partial mới `_Pagination`/`_Breadcrumb`. `product.css` + `product.js`. Chuyển `.section-head` + `.carousel-nav` từ home.css → site.css (dùng chung). Nút giỏ PDP mới là placeholder toast (Lưu ý 5b). PDP thêm `AsSplitQuery()` — đã verify hết warning MultipleCollectionInclude. Smoke test: `/iphone` `/macbook` `/hang-thanh-ly` `/san-pham` `/tim-kiem?q=` `/gioi-thieu` `/chinh-sach-bao-hanh` + slug SP/tin = 200; `/iphone?series=` lọc đúng, `/san-pham` 2 trang; URL rác + `/tin-tuc` = 404 đúng; `/admin` vẫn 200. Build 0 lỗi/0 cảnh báo. |
