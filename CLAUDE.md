# CLAUDE.md — Dự án RenewZone Clone (ASP.NET Core MVC)

> **Mục đích file này:** Là "bộ nhớ" của dự án. Session sau đọc file này là nắm được toàn bộ bối cảnh, không cần phân tích lại từ đầu.
> **Quy tắc bắt buộc:** Sau khi hoàn thành bất kỳ task nào, PHẢI cập nhật lại file này (tick checkbox ở mục 5, ghi thêm dòng vào Nhật ký tiến độ ở mục 8, và bổ sung ghi chú phát sinh nếu có).

---

## 1. Bối cảnh dự án

- **Mục tiêu:** Sao chép (clone) lại website **https://renewzone.vn/** bằng ASP.NET — đây là dự án học tập (thư mục `d:\Learn\lam_viec\renew_zone`).
- **Website gốc:** Cửa hàng bán lẻ sản phẩm Apple (mới & đã qua sử dụng) tại TP.HCM: iPhone, MacBook, iPad, Apple Watch, AirPods, hàng thanh lý. Kèm dịch vụ **thu cũ đổi mới (trade-in)**, **sửa chữa**, trả góp 0%, bảo hành dài hạn.
- **Trạng thái hiện tại:** Đã xong **Giai đoạn 0 → 5** — database `RenewZoneClone` đầy đủ **16 bảng** + Identity; trang chủ 7 section; **SlugController + fallback routing chạy tốt**: trang danh mục (tab series, phân trang), chi tiết sản phẩm (gallery, variant đổi giá, tabs, liên quan), tìm kiếm `/tim-kiem`, `/san-pham`, trang tĩnh + tin chi tiết render qua slug; **luồng mua hàng chạy end-to-end**: thêm giỏ AJAX từ PDP (fly-to-cart + toast + badge), `/gio-hang` (stepper/xóa AJAX, giỏ trống), mini-cart offcanvas, `/dat-hang` (guest checkout, COD/chuyển khoản) → ghi Order + OrderItems → `/dat-hang-thanh-cong`; **web khách đã KHỬ SẠCH nội dung hardcode** (menu/footer từ `MenuItems`, khối cam kết/trấn an từ `ContentBlocks`, bank + chuỗi rời rạc từ `SiteSettings`) — xem mục 4.7; `/tin-tuc` (nổi bật + lọc chuyên mục + tìm + phân trang), `/lien-he` (form + upload + map), 2 trang sửa chữa đã seed. **KHÔNG còn link 404 trên menu/footer.** Tiếp theo: **Giai đoạn 6** (Admin panel).
- Đã là git repository — đã commit Giai đoạn 0-2 (1 commit) + Giai đoạn 3 + Giai đoạn 4 + Giai đoạn 5.
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
| `Category` | Id, Name, Slug, ParentId?, ImageUrl, **IconClass?**, DisplayOrder, IsActive | 2 cấp: cha (iPhone) → con (iPhone 16 Series). GĐ5 thêm `IconClass` (icon menu, thay hàm `CatIcon()` hardcode) |
| `Product` | Id, CategoryId, Name, Slug, OriginalPrice, SalePrice?, IsContactPrice, ShortDesc, DescriptionHtml, SpecsHtml, IsInstallment0, IsFeatured, IsBestSeller, IsActive, ViewCount, CreatedAt | `IsContactPrice=true` ⇒ hiện "Liên hệ" |
| `ProductImage` | Id, ProductId, Url, DisplayOrder, IsMain | |
| `ProductOptionGroup` | Id, ProductId, Name, DisplayOrder | vd "Dung lượng", "Màu sắc", "Tình trạng", "Loại SIM" |
| `ProductOptionValue` | Id, GroupId, Value, PriceAdjustment | cộng/trừ vào giá khi chọn |
| `Order` | Id, Code, CustomerName, Phone, Email?, Address, Note?, PaymentMethod, Status, TotalAmount, CreatedAt | Status: New/Confirmed/Shipping/Completed/Cancelled |
| `OrderItem` | Id, OrderId, ProductId?, ProductName, VariantDesc, UnitPrice, Quantity | snapshot tên/giá tại thời điểm đặt |
| `NewsArticle` | Id, Title, Slug, ThumbnailUrl, Excerpt, ContentHtml, **CategoryId?**, IsFeatured, PublishedAt, ViewCount | GĐ5: bỏ `CategoryTag` (string tự do) → FK sang `NewsCategory`, **SetNull** khi xóa chuyên mục |
| `NewsCategory` | Id, Name, Slug (unique), DisplayOrder, IsActive | GĐ5. Slug **KHÔNG** vào `ISlugResolver` — lọc bằng querystring `?chuyenMuc=` |
| `MenuItem` | Id, Group, Type, Text, Url?, Icon?, ParentId?, DisplayOrder, IsActive, OpenInNewTab | GĐ5. `Group`: MainMenu/FooterPolicy/FooterSupport; `Type`: Link/ProductMega; `Parent` **Restrict** |
| `ContentBlock` | Id, GroupKey, Icon?, Title, Subtitle?, LinkUrl?, ImageUrl?, DisplayOrder, IsActive | GĐ5. Index (GroupKey, DisplayOrder); hằng `ContentBlockGroups` |
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

### 4.6 Giỏ hàng & đặt hàng (Giai đoạn 4 — ĐÃ TRIỂN KHAI)

- **Giỏ nằm trong Session, KHÔNG có bảng DB.** `Cart`/`CartItem` là POCO thuần ở `Domain/Models` (serialize System.Text.Json camelCase, key session `rz_cart`). ⇒ Giai đoạn 4 **không phát sinh migration**; Order/OrderItem đã có từ `InitialCreate`.
- `ICartService`/`CartService` đặt ở **tầng Web** (`Web/Services`) vì phụ thuộc Session — cần `AddHttpContextAccessor()` (đã thêm trong Program.cs). Web KHÔNG đụng `AppDbContext`: muốn lấy sản phẩm thì gọi `ICatalogQueryService.GetProductForCartAsync(id)` (mới thêm, Include ảnh chính + option groups/values, `AsSplitQuery`).
- **Key dòng giỏ** = `productId-<optionValueIds sắp xếp tăng dần>` (vd `8-2-5-9-11`) ⇒ tất định, không phụ thuộc thứ tự khách bấm. Cùng key ⇒ cộng dồn số lượng (clamp 1..10); khác ⇒ dòng mới.
- **Giá luôn tính ở server** (Lưu ý 5c). Id option lạ (không thuộc sản phẩm) ⇒ từ chối; nhóm nào khách chưa chọn ⇒ lấy `IsDefault` (khớp PDP chọn sẵn).
- `IOrderService`/`OrderService` (Infrastructure) tạo Order + OrderItems snapshot, sinh `Code` = `RZ + yyMMdd + 4 số` và **thử lại tối đa 5 lần khi đụng unique index**; sản phẩm đã bị xóa ⇒ `OrderItem.ProductId = null` (tránh vỡ khóa ngoại, đúng ý đồ SetNull).
- Form checkout dùng `CheckoutInput` (ViewModel + DataAnnotations) rồi `ToRequest()` map sang `Domain/Models/OrderRequest` — Domain/Infrastructure không biết gì về MVC.
- **Routes (attribute routing)**: `GET /gio-hang`, `GET /gio-hang/mini` (partial `_MiniCartBody`), `POST /gio-hang/them|cap-nhat|xoa` (JSON), `GET|POST /dat-hang`, `GET /dat-hang-thanh-cong`. POST AJAX gửi anti-forgery qua header `RequestVerificationToken` (layout render sẵn 1 token); checkout là POST form thường theo mô hình **PRG**, dữ liệu trang cảm ơn đi qua TempData (JSON 1 khóa `rz_order_done`) nên F5 không tạo đơn trùng.
- **Hợp đồng JSON** (backend & cart.js phải khớp): `{ ok, message, removedKey, cart:{count, subtotal, subtotalText, isEmpty}, item:{key, name, variant, imageUrl, url, unitPriceText, quantity, lineTotalText} }`.
- Thông tin chuyển khoản là **giả lập** (`DemoBankInfo` trong CheckoutViewModel.cs) đúng Lưu ý 6.2.
- **3 trang này KHÔNG dùng `_Breadcrumb`** mà dùng partial `_CheckoutSteps` (model = bước hiện tại 1/2/3): chấm tròn số + nhãn dưới + nét nối sáng dần (Giỏ hàng → Đặt hàng → Hoàn tất). Bước đã qua hiện dấu tick và bấm quay lại được (trừ khi đang ở bước 3 — đơn đã đặt xong). CSS `.rz-steps` ở cart.css.
- ⚠️ **CSS của partial dùng chung phải để ở `site.css`.** `.rz-breadcrumb` từng nằm trong product.css nên trang giỏ/đặt hàng (chỉ nạp cart.css) render `<ol>` mặc định "1. 2. 3." — đã chuyển sang site.css mục 15. Thêm partial Shared mới thì nhớ quy tắc này.

### 4.7 Quy tắc NỘI DUNG (Giai đoạn 5 — "khử hardcode", áp dụng cho mọi giai đoạn sau)

**Yêu cầu cứng của chủ dự án:** *"không phải admin nói 1 kiểu mà web khách nội dung hardcode"*. Trước khi viết bất kỳ chuỗi nào vào view, phân loại theo bảng này:

| Loại | Ví dụ | Chỗ ở đúng |
|---|---|---|
| **Nội dung** — chủ shop muốn đổi mà không cần lập trình viên | tên cửa hàng, hotline, địa chỉ, link menu, danh sách chính sách, khối cam kết, bài viết, thông tin ngân hàng, chủ đề form liên hệ | **DB** (`SiteSetting` / `MenuItem` / `ContentBlock` / bảng nghiệp vụ) |
| **Nhãn chức năng** — thuộc về tính năng, đổi là đổi cả UX | "Thêm vào giỏ", "Đặt hàng ngay", "Tìm kiếm", "Trang 1/5", thông báo lỗi validate, tiêu đề cột footer | **Hardcode trong view — ĐÚNG**, giữ nguyên |
| **Cấu hình kỹ thuật** | dung lượng file tối đa, đuôi file cho phép, TTL cache, page size | **appsettings.json** (Options pattern), KHÔNG vào DB |

**Luật fallback (BẮT BUỘC — thiếu dữ liệu không bao giờ được làm vỡ layout):**
- Chuỗi từ SiteSetting: dùng `settings.Get(key, fallback)` (`Web/Helpers/SettingExtensions.cs`) — value null **hoặc rỗng/whitespace** đều rơi về fallback ghi tại chỗ. Không bao giờ để lọt chuỗi rỗng / "null" ra HTML.
- Khối danh sách (`ContentBlock`, `MenuItem`, social links): rỗng ⇒ **ẩn cả khối**, không render khung trống, không throw. `ContentBlocksViewComponent` tự làm việc này nên view gọi không cần `if`.
- Ảnh: luôn `?? "/images/placeholder-*.svg"`.
- URL (social, map embed): rỗng ⇒ ẩn nút/khối. `href=""` sẽ trỏ về chính trang hiện tại — lỗi thầm lặng.

**Cache:** mọi service layout đọc mỗi request đều `IMemoryCache` TTL 5 phút + có `Invalidate()` — `ISiteContentService`, `INavigationService`, `IContentBlockService`, `ISlugResolver`. **Giai đoạn 6: Admin sửa xong PHẢI gọi `Invalidate()` tương ứng**, nếu không phải đợi 5 phút mới thấy thay đổi.

**Nguồn dữ liệu từng chỗ (tra nhanh khi làm Admin):**

| Chỗ trên web khách | Nguồn |
|---|---|
| Menu chính + offcanvas mobile | `MenuItems` group `MainMenu` (mục `Type=ProductMega` thì dropdown sinh động từ cây `Category`, không khai báo link con) |
| 2 cột link ở footer | `MenuItems` group `FooterPolicy` / `FooterSupport` |
| Icon danh mục trong menu | `Category.IconClass` |
| Khối cam kết ở trang chi tiết SP | `ContentBlocks` group `product.commitments` |
| Dòng trấn an ở giỏ + checkout | `ContentBlocks` group `cart.assurance` |
| 4 tile kênh liên hệ ở `/lien-he` | `ContentBlocks` group `contact.channels` |
| Thông tin chuyển khoản (checkout + trang cảm ơn) | `SiteSettings` `BankName`/`BankAccountNumber`/`BankAccountName`/`BankTransferNoteTemplate` qua partial `_BankInfo` |
| Phí vận chuyển, mô tả footer, disclaimer, tiêu đề trang tin, lời mời liên hệ, chủ đề form | `SiteSettings` `ShippingNote`/`FooterAbout`/`FooterDisclaimer`/`NewsPageTitle`/`NewsPageIntro`/`ContactIntro`/`ContactSubjects` |
| Giới hạn file upload form liên hệ | `appsettings.json` → `ContactUpload` (Options) |

**Quyết định kiến trúc:** `_Layout` dùng `@inject INavigationService` thay vì ViewComponent — cùng một tập dữ liệu render 3 kiểu markup rất khác nhau (nav desktop có flyout, offcanvas có collapse id riêng, footer là 2 cột link) và **cả 3 chỉ tồn tại trong `_Layout`**, tách thành 3 ViewComponent + 3 view chỉ thêm file chứ không tái dùng được. Ngược lại `ContentBlocks` **là** ViewComponent vì được gọi từ 3 view khác nhau (PDP, giỏ, checkout).

### 4.8 Trang tin tức & liên hệ (Giai đoạn 5)

- **`/tin-tuc`** (`NewsController`, attribute routing, 9 bài/trang): `INewsQueryService` (KHÔNG cache). Khối 3 bài nổi bật **chỉ hiện ở trang 1 khi không lọc và không tìm**; bài nổi bật bị **loại khỏi query TRƯỚC khi đếm** nên `TotalItems`/số trang khớp đúng số bài thật sự liệt kê. Lọc bằng querystring `?chuyenMuc=<slug>` + `?q=`; `NewsListViewModel.CategoryUrl()/PageUrl()` giữ nguyên bộ lọc khi đổi trang/đổi chuyên mục. Slug chuyên mục sai ⇒ trả trang rỗng, **không 500**.
- **`/lien-he`** (`ContactController`): GET/POST + **PRG** (`?sent=1` → khối cảm ơn thay form, F5 không tạo bản ghi trùng). Form bind `[Bind(Prefix = "Input")]` cho khớp `name="Input.X"` do `asp-for` sinh ra.
- **Upload an toàn** (`TryStoreAttachmentAsync`): tên file **luôn** đổi thành `Guid.NewGuid().ToString("N") + ext` (chống path traversal / ghi đè), whitelist **cả đuôi lẫn ContentType**, kiểm dung lượng ở client **và** server, lưu vào `wwwroot/uploads/contact/yyyy-MM/`. Sai định dạng ⇒ `ModelState.AddModelError` chứ không throw. `.gitignore` đã chặn thư mục này.
- **Chống spam:** honeypot `Input.Website` (ẩn bằng CSS `.hp-field`, không dùng `type=hidden`) — có giá trị ⇒ **giả vờ thành công nhưng KHÔNG ghi DB**; chặn tần suất bằng Session (< 30 giây ⇒ báo lỗi lịch sự).
- Chấm "Đang mở cửa / Đã đóng cửa" ở cột thông tin: JS parse setting `OpenHours` bằng regex — **parse không được thì để nguyên `hidden`** (fallback an toàn, không đoán bừa).

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

### Giai đoạn 4 — Giỏ hàng & Đặt hàng ✅ (xong 2026-07-22)
- [x] `CartService` (session JSON, `Web/Services`): thêm/sửa số lượng/xóa/clear; item phân biệt bằng `Key = productId-<các optionValueId sắp xếp>` ⇒ cùng biến thể cộng dồn, khác biến thể tách dòng. Giá + mô tả biến thể TÍNH LẠI Ở SERVER, số lượng clamp 1..10, chặn sản phẩm "Liên hệ"
- [x] Trang `/gio-hang`: line item (thumb/tên/variant/đơn giá/stepper/thành tiền/xóa), tóm tắt sticky, CTA dính đáy mobile, trạng thái giỏ trống đúng thông điệp gốc + SVG túi giấy tự vẽ; stepper & xóa chạy AJAX (cập nhật lạc quan, xóa = gập dòng 300ms), badge header + tổng tiền đồng bộ không reload
- [x] Mini-cart offcanvas trong layout (mở từ icon giỏ, nạp lại qua `GET /gio-hang/mini`) + `CartBadgeViewComponent` (badge đúng ngay lần tải đầu)
- [x] Checkout `/dat-hang`: form khách (họ tên, SĐT regex, email, địa chỉ, ghi chú) + radio-card COD/chuyển khoản (panel bank demo slide-down), tóm tắt đơn sticky (mobile thành accordion), validate client + server, chống double-submit → `OrderService` tạo Order + OrderItems snapshot → clear giỏ → PRG sang `/dat-hang-thanh-cong` (SVG check vẽ nét, copy mã đơn, guard TempData)
- [x] Nút PDP "Thêm vào giỏ" + "Mua ngay" nối thật (stepper số lượng, `data-value-id`), kèm fly-to-cart 600ms + toast giàu (`RZ.addToCart/cartToast/flyToCart`)

### Giai đoạn 5 — Tin tức, liên hệ, khử hardcode nội dung ✅ (xong 2026-07-22)
- [x] **Khử hardcode (phần A)**: 3 entity mới `MenuItem`/`ContentBlock`/`NewsCategory` + 2 enum + cột `Category.IconClass` + 15 `SiteSettings` mới (migration `Phase5Content`); 4 service `INavigationService`/`IContentBlockService`/`INewsQueryService`/`IContactService`. `_Layout` bỏ sạch mảng `policyLinks`, hàm `CatIcon()`, 2 link sửa chữa, 11 link footer; PDP/giỏ/checkout đọc `ContentBlocks`; **xóa hẳn class `DemoBankInfo`** → partial `_BankInfo` đọc SiteSettings. Bảng phân loại nội dung + luật fallback ở mục 4.7
- [x] `/tin-tuc`: hero + 3 bài nổi bật (không lặp ở danh sách) + chip lọc chuyên mục kèm số bài + tìm kiếm + phân trang 9 bài/trang + trạng thái rỗng + CTA hotline (mục 4.8)
- [x] Render StaticPage theo slug (giới thiệu + các trang chính sách) — xong ở Giai đoạn 3 (`StaticPage.cshtml`). Đã seed thêm `/sua-chua` + `/sua-chua-phan-cung` nội dung phong phú (h2/h3, danh sách, quy trình, bảng giá cuộn ngang được trên mobile)
- [x] `/lien-he`: 4 tile kênh liên hệ (ContentBlock), form đầy đủ + dropzone kéo-thả + đếm ký tự, upload an toàn (đổi tên GUID, whitelist đuôi + ContentType, giới hạn 2 tầng), honeypot + chặn tần suất 30s, PRG → khối cảm ơn, cột thông tin sticky + chấm mở cửa, map embed (setting rỗng ⇒ ẩn)
- [x] **Dọn nợ CSS (phần C)**: `.news-card` từng định nghĩa TRÙNG ở home.css + product.css → gom về site.css (mục 19) + partial `_NewsCard`; `.series-tab` → component chung `.rz-chip` (mục 18); `.rz-input`/`.co-label`/`.co-error` + `@keyframes shake` (mục 17), `.done-check` + `draw-stroke` (mục 20), `.bank-row` (mục 21) chuyển từ cart.css sang site.css

### Giai đoạn 6 — Admin panel (Area `Admin`, route `/admin`)

> **Kế hoạch chi tiết: `docs/PHASE6_PROMPT.md`** (design system admin, đặc tả 15 màn, luồng dữ liệu, bẫy kỹ thuật, tiêu chí nghiệm thu).
> **4 quyết định đã CHỐT với user (2026-07-22 — không hỏi lại):**
> 1. Chia **3 đợt**: A (nền tảng: soft delete + cache invalidator + slug service + media service + auth + layout + dashboard + đơn hàng) · B (catalog: danh mục + sản phẩm + ảnh + biến thể) · C (nội dung: bài viết, trang tĩnh, banner, menu, content block, hộp thư, cấu hình, thùng rác). Mỗi đợt build sạch + smoke test + commit riêng.
> 2. Editor rich text: **TinyMCE** (tải local `wwwroot/lib/tinymce/`, KHÔNG CDN) — chọn vì có sẵn công cụ chèn `<table>`, dự án nhiều bảng giá.
> 3. Tương tác: **server-rendered + AJAX có chọn lọc** — form là POST/PRG như web khách; AJAX chỉ dùng cho kéo-thả sắp xếp, toggle IsActive, đổi trạng thái đơn, upload ảnh, xóa.
> 4. Xóa dữ liệu: **soft delete + thùng rác** (`ISoftDeletable` + EF global query filter + chặn ở `SaveChanges`). Order/OrderItem/SiteSetting KHÔNG xóa mềm (đơn hủy thì đổi `Status`).
>
> **2 nợ kỹ thuật GĐ trước phải trả trong đợt A:** (a) validate slug **unique xuyên 4 bảng** (Lưu ý 6.4 — hiện chưa có gì kiểm); (b) trích logic upload từ `ContactController.TryStoreAttachmentAsync` thành `IMediaService` dùng chung thay vì copy sang admin.

- [ ] Đăng nhập Identity, chặn toàn Area bằng `[Authorize(Roles="Admin")]`
- [ ] Dashboard: đếm đơn mới, sản phẩm, bài viết, liên hệ chưa đọc
- [ ] CRUD Danh mục (2 cấp) · CRUD Sản phẩm (kèm upload nhiều ảnh, quản lý option groups/values, editor rich text — TinyMCE/Quill)
- [ ] Quản lý Đơn hàng: danh sách, lọc theo trạng thái, xem chi tiết, đổi trạng thái
- [ ] CRUD Bài viết (chọn `NewsCategory`), Chuyên mục tin, Trang tĩnh, Banner, Testimonial
- [ ] **CRUD Menu (`MenuItem` 2 cấp) + Khối nội dung (`ContentBlock` theo GroupKey)** — hạ tầng đã có từ Giai đoạn 5, xem mục 4.7
- [ ] Xem ContactMessage (đánh dấu đã đọc, mở file đính kèm), sửa SiteSettings
- [ ] ⚠️ Sau MỌI thao tác sửa: gọi `Invalidate()` của service tương ứng (`ISiteContentService`, `INavigationService`, `IContentBlockService`, `ISlugResolver`) — không thì phải đợi hết cache 5 phút mới thấy đổi

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
5b. **Nút "Thêm giỏ"/"Mua ngay" ở PDP đã nối vào `CartService` thật** (Giai đoạn 4): product.js gom `productId` + `optionValueIds` (từ `.pdp-opt.is-active[data-value-id]`) + số lượng rồi gọi `RZ.addToCart()` trong cart.js; "Mua ngay" thêm giỏ xong chuyển `/gio-hang`.
5c. **KHÔNG BAO GIỜ tin giá do client gửi.** Client chỉ gửi `productId` + `optionValueIds` + `quantity`; server nạp lại Product/OptionValue từ DB để tính `UnitPrice` và dựng `VariantDescription`. Mọi chuỗi tiền trong JSON trả về đều do server format sẵn (`ToVnd()`) — client không tự tính để khỏi lệch làm tròn.
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
13. **Seed `SiteSettings` idempotent ở mức TỪNG KEY** (đổi ở Giai đoạn 5), không phải "bảng trống mới chạy" — nếu không thì thêm key mới ở giai đoạn sau sẽ không bao giờ vào được DB đang chạy. Hệ quả cần biết: **xóa tay 1 setting rồi restart thì nó tự mọc lại**; muốn test fallback thì để `Value` RỖNG (helper `Get()` coi rỗng/whitespace là thiếu). `BackfillPhase5Async` làm việc tương tự cho cột/bảng mới trên DB đã seed từ trước (gán `Category.IconClass`, `NewsArticle.CategoryId`, thêm 2 trang sửa chữa) nên **không phải drop DB** — giữ được đơn hàng test cho Giai đoạn 6.
14. **`IConfiguration` binder NỐI THÊM vào collection sẵn có, không thay thế.** `ContactUploadOptions.AllowedExtensions` từng khai giá trị mặc định trong C# ⇒ khi appsettings.json cũng khai thì danh sách bị lặp đôi (thông báo lỗi hiện "…, .pdf, .jpg, …"). Đã sửa: property mặc định `[]`, giá trị mặc định nằm ở `EffectiveExtensions`. Áp dụng cho mọi Options có mảng/list sau này.
15. **Đổi `CategoryTag` (string) → `CategoryId` (FK) là breaking change** — nơi nào render tag bài viết đều phải `Include(a => a.Category)`, nếu quên thì `Category` null và tag biến mất âm thầm (`ContentQueryService`, `HomeQueryService`, `NewsQueryService` đều đã Include).

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
| 2026-07-22 | **Xong Giai đoạn 4 (giỏ hàng & đặt hàng).** Quyết định kiến trúc (chi tiết mục 4.6): giỏ ở **Session** (POCO `Cart`/`CartItem` trong Domain, JSON camelCase, key `rz_cart`) ⇒ **KHÔNG migration mới**; `ICartService` đặt ở tầng Web (cần `AddHttpContextAccessor()` — trước đó chưa đăng ký), lấy sản phẩm qua `ICatalogQueryService.GetProductForCartAsync()` để Web không đụng `AppDbContext`; **Key dòng giỏ tất định** `productId-<optionValueIds sắp xếp>` (cộng dồn/tách dòng); giá + `VariantDescription` tính lại ở SERVER, mọi chuỗi tiền trong JSON do server format (Lưu ý 5c); `OrderService` sinh `Code` RZ+yyMMdd+4 số, retry 5 lần khi trùng unique index, product đã xóa ⇒ `OrderItem.ProductId = null`. Thêm `Domain/Models/OrderRequest` để `CheckoutInput` (DataAnnotations, tầng Web) không lọt xuống Domain. Views: `Cart/Index` (line item + tóm tắt sticky + CTA đáy mobile + giỏ trống SVG tự vẽ), `Checkout/Index` (radio-card + panel bank demo + tóm tắt accordion mobile), `Checkout/Complete` (SVG check stroke-dashoffset + copy mã), `_MiniCart`/`_MiniCartBody`, `CartBadgeViewComponent`. `cart.css` + `cart.js` (nạp global, mở rộng `RZ.addToCart/cartToast/flyToCart/refreshMiniCart/syncCart`) + `checkout.js`; phần global (mini-cart, toast giàu, fly-clone, `.rz-stepper`) đặt ở site.css mục 15. PDP bỏ placeholder, thêm `data-value-id` + stepper số lượng. **Bẫy gặp phải:** (1) cart.js phải nạp **không `defer`** ngay sau site.js, nếu để defer thì product.js (trong `@section Scripts`) chạy trước và chưa có `RZ.addToCart`; (2) form checkout viết `action` tay nên FormTagHelper không tự thêm token ⇒ dùng `asp-antiforgery="true"` thay vì `@Html.AntiForgeryToken()` để chắc chắn chỉ có ĐÚNG 1 token trong form (2 token trùng tên sẽ làm validate hỏng); (3) listener submit của ta chạy TRƯỚC jquery-validation nên phải tự hỏi `$(form).valid()` rồi mới bật spinner, không thì form sai vẫn bị khóa nút; (4) `PaymentMethod` cần `[Required]` có message tiếng Việt, không thì lọt chuỗi "The … field is required" của framework vào HTML. Smoke test thật (curl + cookie jar): giá biến thể đúng (30.490.000 + 3.5M − 1.5M − 0.8M = 31.690.000đ), cộng dồn/tách dòng đúng, SP "Liên hệ" + option id lạ bị chặn, POST thiếu token = 400, clamp qty 99→10, xóa dòng, `/dat-hang` chặn giỏ trống, validate server hiện đủ lỗi, đặt hàng → **verify sqlcmd: Orders + OrderItems khớp snapshot, TotalAmount = tổng dòng**, giỏ clear, badge về 0, TempData tiêu thụ 1 lần (F5 → `/`). Không hồi quy: `/` `/iphone` `/san-pham` `/tim-kiem` `/gioi-thieu` slug SP `/admin` = 200, URL rác = 404. Build 0 lỗi/0 cảnh báo. Ghi chú: DB đang có 2 đơn hàng test (giữ lại làm dữ liệu mẫu cho Giai đoạn 6). |
| 2026-07-22 | **Xong Giai đoạn 5 (tin tức · liên hệ · khử hardcode).** Migration `Phase5Content`: 3 bảng mới (`MenuItems`, `ContentBlocks`, `NewsCategories`), `NewsArticles.CategoryTag` → `CategoryId` (FK **SetNull**), `Categories.IconClass`, +15 SiteSettings. **Khử hardcode (phần A)** — nguồn dữ liệu từng chỗ ghi ở mục 4.7: `_Layout` xóa sạch `policyLinks`/`CatIcon()`/2 link sửa chữa/11 link footer, giờ đọc `MenuItems` 3 nhóm (mục `ProductMega` vẫn sinh dropdown động từ cây `Category` để admin thêm danh mục là menu tự có); khối cam kết PDP + dòng trấn an giỏ/checkout đọc `ContentBlocks` qua ViewComponent (nhóm rỗng ⇒ **tự ẩn**); **xóa hẳn class `DemoBankInfo`**, thay bằng partial `_BankInfo` đọc SiteSettings (template nội dung CK có `{0}` = mã đơn). **Phần B**: `/tin-tuc` (hero + 3 bài nổi bật không lặp ở list + chip lọc kèm số bài + tìm + phân trang 9 bài + rỗng + CTA), `/lien-he` (4 tile kênh, dropzone kéo-thả + thumbnail + đếm ký tự, cột info sticky + chấm mở cửa, map ẩn khi setting rỗng), seed 2 trang sửa chữa nội dung dài + bảng giá. **Phần C**: `.news-card` hết trùng (home.css + product.css → site.css mục 19 + partial `_NewsCard`), `.series-tab` → `.rz-chip` chung, `.rz-input`/`shake`/`.done-check`/`draw-stroke`/`.bank-row` từ cart.css → site.css. **Quyết định:** `_Layout` dùng `@inject INavigationService` (3 kiểu markup khác nhau, chỉ dùng trong 1 file) còn `ContentBlocks` là ViewComponent (gọi từ 3 view) — lý do ở 4.7. **Bẫy gặp phải:** (1) `IConfiguration` binder **nối thêm** vào mảng có sẵn ⇒ `AllowedExtensions` bị lặp đôi, phải để mặc định `[]` + `EffectiveExtensions` (Lưu ý 6.14); (2) seed `SiteSettings` phải idempotent **theo từng key** chứ không phải "bảng trống mới chạy", nếu không key mới không vào được DB cũ — kéo theo `BackfillPhase5Async` cho `IconClass`/`CategoryId`/2 trang sửa chữa nên **không phải drop DB** (Lưu ý 6.13); (3) `asp-for` sinh `name="Input.X"` nên action phải `[Bind(Prefix = "Input")]`, không thì ModelState key lệch và lỗi server không hiện lại đúng ô; (4) `.sum-assure` từ `<p>` thành `<ul>` (ContentBlock) nên phải reset list-style. **Smoke test thật (curl + cookie jar + sqlcmd):** 23 route đúng mã (URL rác vẫn 404), 0 exception trong log; `?chuyenMuc=` + `?q=` + kết hợp + phân trang lọc đúng, chuyên mục sai ⇒ rỗng chứ không 500, 3 bài nổi bật KHÔNG lặp ở danh sách; **đổi DB → web đổi theo** (sửa `MenuItems.Text` → menu đổi, `BankName` → checkout + trang cảm ơn đổi, tắt `ContentBlocks` → khối cam kết ẩn hẳn, để rỗng `FooterDisclaimer` → hiện fallback chứ không rỗng); liên hệ: validate tiếng Việt + giữ dữ liệu đã nhập, `.exe` và file 6MB bị chặn **ở server** (curl bỏ qua JS), thiếu token = 400, honeypot ⇒ 302 nhưng **0 dòng DB**, gửi lại trong 30s bị chặn, gửi hợp lệ → verify sqlcmd `AttachmentPath` trỏ file thật **tên đã đổi thành GUID**; luồng mua hàng GĐ4 còn nguyên (thêm giỏ → đặt hàng → Order + OrderItem đúng, TotalAmount = 8.990.000 × 2). Build 0 lỗi/0 cảnh báo. Đã dọn dữ liệu test và trả các giá trị DB về như seed. |
| 2026-07-22 | **Sửa breadcrumb vỡ + thay bằng thanh tiến trình mua hàng.** User báo `/dat-hang` hiện "1. Trang chủ 2. Giỏ hàng 3. Đặt hàng" dạng list thô — nguyên nhân: CSS `.rz-breadcrumb` của partial DÙNG CHUNG lại nằm trong `product.css`, mà trang giỏ/đặt hàng chỉ nạp `cart.css` ⇒ `<ol>` rơi về mặc định trình duyệt. Đã **chuyển `.rz-breadcrumb` sang site.css (mục 15 mới, dồn cart-global thành 16, responsive thành 17)** — quy tắc: partial ở `Views/Shared` thì CSS phải ở site.css. Theo yêu cầu user, 3 trang luồng mua hàng đổi từ breadcrumb sang partial mới `_CheckoutSteps` (model = bước 1/2/3): chấm tròn 40px chứa số, nhãn dưới chấm, nét nối 2 lớp (::before xám nền + ::after gradient hồng `scaleX(0→1)` animate 520ms, stagger 160ms theo `--step-index`), bước đã qua đổi thành dấu tick + bấm quay lại được bằng `.stretched-link` (chặn ở bước 3 vì đơn đã đặt xong), bước hiện tại có ring `--brand-100` + `step-pop`. Verify HTML cả 3 trạng thái đúng class `is-done`/`is-current`, breadcrumb các trang cũ (`/iphone` `/san-pham` PDP `/gioi-thieu`) vẫn render bình thường. Build 0 lỗi/0 cảnh báo. |
