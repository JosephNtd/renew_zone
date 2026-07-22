# PROMPT — Giai đoạn 5: Tin tức · Liên hệ · Khử hardcode nội dung (RenewZone Clone)

> Copy toàn bộ file này gửi cho session Claude Code mới (đang mở tại `d:\Learn\lam_viec\renew_zone`).
> **Mục tiêu kép:**
> 1. Dựng nốt 3 màn còn thiếu của web khách (`/tin-tuc`, `/lien-he`, 2 trang sửa chữa) — **đẹp, mượt, đúng design system**.
> 2. **KHỬ SẠCH nội dung hardcode** trên web khách để Giai đoạn 6 (Admin) có thứ thật để quản trị. Đây là yêu cầu cứng của chủ dự án: *"không phải admin nói 1 kiểu mà web khách nội dung hardcode"*.

---

## 0. Cách làm việc (đọc trước khi code)

1. **Đọc `CLAUDE.md` trước tiên** — kiến trúc 3 tầng, mục 4.6 (giỏ hàng), toàn bộ Lưu ý mục 6. Đọc `docs/DESIGN.md` mục **3.14 (tin tức), 3.6 (card), 3.11 (breadcrumb/pagination), 4 (animation), 7 (a11y)**.
2. Bám **design system đã có**, KHÔNG chế token/màu/font mới. Tokens + component dùng chung ở `wwwroot/css/site.css`.
3. Máy: **Windows 11 + PowerShell** (KHÔNG dùng `&&`). NuGet Microsoft.* **pin `9.0.13`**. SQL Server `localhost`, DB `RenewZoneClone`.
4. **Giai đoạn này CÓ migration** (khác Giai đoạn 4). Lệnh (DbContext ở Infrastructure, startup ở Web):
   `dotnet ef migrations add Phase5Content -p src/RenewZone.Infrastructure -s src/RenewZone.Web -o Data/Migrations`
   `dotnet ef database update -p src/RenewZone.Infrastructure -s src/RenewZone.Web`
   Nếu seed cũ vướng: `dotnet ef database drop -p src/RenewZone.Infrastructure -s src/RenewZone.Web` rồi chạy lại app (DbInitializer tự migrate + seed).
5. App đang chạy sẽ **khóa DLL** → `taskkill /F /IM RenewZone.Web.exe` trước khi `dotnet build`. Xong phải **thật sự `dotnet run` + smoke test**, không chỉ build. Kết thúc: **0 warning / 0 error**.
6. Xong → tick Giai đoạn 5 trong `CLAUDE.md` mục 5, cập nhật mục 1 (trạng thái) + mục 4 (thêm 4.7), thêm dòng nhật ký mục 8, rồi commit tiếng Việt bằng heredoc `git commit -F - <<'EOF'` (**KHÔNG dùng here-string PowerShell `@'...'@`** — lọt ký tự `@`). Kết `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

## 1. Nguyên tắc CỨNG về nội dung (phần quan trọng nhất của giai đoạn này)

### 1.1 Ranh giới "nội dung" vs "nhãn giao diện"

Đừng nhét mọi chuỗi vào DB — đó là bài toán i18n, không phải CMS. Phân loại rõ:

| Loại | Ví dụ | Chỗ ở đúng |
|---|---|---|
| **Nội dung** — chủ shop muốn đổi mà không cần lập trình viên | tên cửa hàng, hotline, địa chỉ, link menu, danh sách chính sách, khối cam kết, bài viết, thông tin ngân hàng, chủ đề form liên hệ | **DB** (SiteSetting / MenuItem / ContentBlock / các bảng nghiệp vụ) |
| **Nhãn chức năng** — thuộc về tính năng, đổi là đổi cả UX | "Thêm vào giỏ", "Đặt hàng ngay", "Tìm kiếm", "Trang 1/5", thông báo lỗi validate | **Hardcode trong view** — ĐÚNG, giữ nguyên |
| **Cấu hình kỹ thuật** | dung lượng file tối đa, đuôi file cho phép, TTL cache, page size | **appsettings.json** (Options pattern), KHÔNG vào DB |

Ghi nguyên bảng phân loại này vào `CLAUDE.md` mục 4.7 để các giai đoạn sau không lệch chuẩn.

### 1.2 Luật fallback (bắt buộc)

**Thiếu dữ liệu trong DB không bao giờ được làm vỡ layout.**
- Chuỗi đơn đọc từ SiteSetting: dùng helper `S(key, fallback)` — thiếu thì lấy fallback ghi ngay tại view.
- Khối danh sách (ContentBlock, MenuItem): rỗng ⇒ **ẩn khối**, không render khung trống, không throw.
- Ảnh: luôn `?? "/images/placeholder-*.svg"`.

### 1.3 Cache

Mọi thứ layout đọc mỗi request (menu, settings, content block) phải qua service có `IMemoryCache` **TTL 5 phút** + `Invalidate()` như `SiteContentService` đang làm. Giai đoạn 6 admin sửa xong sẽ gọi `Invalidate()`.

---

## 2. Phạm vi Giai đoạn 5

### PHẦN A — Khử hardcode (làm TRƯỚC, là nền cho phần B và cho Admin GĐ6)

| # | Việc | Vì sao |
|---|---|---|
| A1 | Entity **`MenuItem`** + service + refactor `_Layout` (menu chính desktop, offcanvas mobile, 2 cột link ở footer) | `_Layout.cshtml` đang hardcode mảng `policyLinks`, 2 link sửa chữa, 6 link chính sách + 5 link hỗ trợ ở footer |
| A2 | Entity **`ContentBlock`** + ViewComponent + refactor khối cam kết PDP, dòng trấn an giỏ/checkout | `Slug/Product.cshtml` hardcode 4 dòng `pdp-commit`; `Cart/Index` + `Checkout/Index` hardcode "Thu cũ đổi mới · Bảo hành dài hạn" |
| A3 | Bổ sung **SiteSettings** + chuyển `DemoBankInfo` từ C# xuống DB | `Models/ViewModels/CheckoutViewModel.cs` đang hardcode tên ngân hàng/STK/chủ TK trong code |
| A4 | Entity **`NewsCategory`** thay `NewsArticle.CategoryTag` (string tự do) | Cần chuyên mục có slug để lọc ở `/tin-tuc`, và để admin quản lý danh sách chuyên mục thay vì gõ tay dễ sai chính tả |
| A5 | Thêm cột **`Category.IconClass`** | `_Layout` đang hardcode hàm `CatIcon(slug)` map slug → icon Bootstrap |

### PHẦN B — 3 màn hình còn thiếu

| # | Màn | Route |
|---|---|---|
| B1 | Trang tin tức (nổi bật + lọc chuyên mục + tìm + phân trang) | `GET /tin-tuc` |
| B2 | Trang liên hệ (thông tin + form + upload + map) | `GET/POST /lien-he` |
| B3 | 2 trang sửa chữa (seed StaticPage nội dung phong phú) | `/sua-chua`, `/sua-chua-phan-cung` |

### PHẦN C — Dọn nợ kỹ thuật CSS/partial

| # | Việc |
|---|---|
| C1 | `.news-card`/`.news-tag`/`.news-thumb`... **đang bị định nghĩa TRÙNG ở `home.css` và `product.css`** → gom về `site.css`, tạo partial dùng chung `_NewsCard.cshtml` (trang chủ + `/tin-tuc` + bài liên quan cùng dùng) |
| C2 | `.series-tab` (product.css) đổi thành component chung `.rz-chip` ở site.css — `/tin-tuc` dùng lại làm chip lọc chuyên mục. Giữ `.series-tab` như alias để không vỡ trang danh mục, hoặc sửa luôn view danh mục |
| C3 | `.done-check` + keyframe `draw-stroke` (cart.css) chuyển sang site.css — trang liên hệ dùng lại cho trạng thái gửi thành công |

> **Quy tắc đã chốt ở GĐ4 (đừng lặp lại lỗi):** partial nằm ở `Views/Shared` thì CSS **bắt buộc** để ở `site.css`. Trước đây `.rz-breadcrumb` để trong `product.css` làm vỡ trang giỏ hàng.

---

## 3. Kiến trúc Backend

### 3.1 Entities mới (Domain/Entities)

```csharp
// Điều hướng — thay toàn bộ menu/footer hardcode trong _Layout
public class MenuItem : BaseEntity
{
    public MenuGroup Group { get; set; }        // MainMenu | FooterPolicy | FooterSupport
    public MenuItemType Type { get; set; }      // Link | ProductMega
    public string Text { get; set; }
    public string? Url { get; set; }            // null khi Type = ProductMega
    public string? Icon { get; set; }           // "bi-shield-check"
    public int? ParentId { get; set; }          // menu 2 cấp (dropdown)
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool OpenInNewTab { get; set; }
    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = [];
}

// Khối nội dung lặp lại (cam kết, USP, kênh liên hệ...)
public class ContentBlock : BaseEntity
{
    public string GroupKey { get; set; }        // "product.commitments" | "cart.assurance" | "contact.channels"
    public string? Icon { get; set; }
    public string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? LinkUrl { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// Chuyên mục tin tức
public class NewsCategory : BaseEntity
{
    public string Name { get; set; }
    public string Slug { get; set; }            // dùng ở querystring ?chuyen-muc=
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<NewsArticle> Articles { get; set; } = [];
}
```

**Sửa entity cũ:**
- `NewsArticle`: bỏ `CategoryTag` → thêm `int? CategoryId` + `NewsCategory? Category` (**SetNull** khi xóa chuyên mục — không được mất bài viết).
- `Category`: thêm `string? IconClass`.
- `MenuItem.ParentId` self-reference: **Restrict** (không cho xóa cha khi còn con).

**Configurations** (`Infrastructure/Data/Configurations/`): mỗi entity 1 file như 13 file đang có. `MenuItem.Text` max 100, `Url` max 300; `ContentBlock.GroupKey` max 60 + **index trên (GroupKey, DisplayOrder)**; `NewsCategory.Slug` max 200 + **unique index**.

> ⚠️ **Slug của NewsCategory KHÔNG vào `ISlugResolver`** (chỉ 4 bảng Category/Product/News/StaticPage). Lọc chuyên mục đi bằng **querystring** `?chuyen-muc=<slug>` — giống `?series=` ở trang danh mục — nên không đụng namespace slug toàn cục.

### 3.2 SiteSettings bổ sung (thêm hằng vào `SettingKeys`)

| Key | Giá trị seed (GIẢ LẬP — Lưu ý 6.2) |
|---|---|
| `BankName` | `Demo Bank (ngân hàng giả lập)` |
| `BankAccountNumber` | `0000 0000 0000` |
| `BankAccountName` | `CTY RENEWZONE DEMO` |
| `BankTransferNoteTemplate` | `Thanh toan don {0}` (placeholder = mã đơn) |
| `ShippingNote` | `Shop báo khi xác nhận` |
| `ContactSubjects` | `Tư vấn mua hàng\|Thu cũ đổi mới\|Bảo hành\|Sửa chữa\|Khiếu nại\|Khác` (ngăn bằng `\|`) |
| `ContactIntro` | 1–2 câu mời liên hệ |
| `NewsPageTitle` / `NewsPageIntro` | tiêu đề + phụ đề trang `/tin-tuc` |
| `FooterAbout` | đoạn mô tả cửa hàng ở footer (đang hardcode) |
| `FooterDisclaimer` | dòng "Website học tập (clone)..." (đang hardcode) |
| `MapEmbedUrl` | **key đã có nhưng seed rỗng** → seed URL nhúng Google Maps chung chung |

`DemoBankInfo` trong `CheckoutViewModel.cs` **xóa hẳn**, thay bằng đọc SiteSettings ở 2 view checkout + trang cảm ơn (nhớ fallback).

### 3.3 Services

```csharp
// Domain/Interfaces
public interface INavigationService            // cache 5', có Invalidate()
{
    Task<IReadOnlyList<MenuItem>> GetMenuAsync(MenuGroup group);   // đã Include Children, đã lọc IsActive, đã sort
}

public interface IContentBlockService          // cache 5', có Invalidate()
{
    Task<IReadOnlyList<ContentBlock>> GetAsync(string groupKey);
}

public interface INewsQueryService             // KHÔNG cache (giống ICatalogQueryService)
{
    Task<NewsListPage> GetListAsync(string? categorySlug, string? keyword, int page, int pageSize);
    Task<IReadOnlyList<NewsCategory>> GetCategoriesAsync();
}

public interface IContactService               // Infrastructure, ghi DB
{
    Task<ContactMessage> SubmitAsync(ContactRequest request);
}
```

- `NewsListPage` (Domain/Models): `Featured` (list, chỉ có khi trang 1 + không lọc/không tìm), `Articles` (`PagedResult<NewsArticle>`), `Categories`, `ActiveCategory`, `Keyword`.
- **Bài featured KHÔNG được lặp lại trong danh sách bên dưới** — loại theo Id.
- `ContactRequest` (Domain/Models) giống pattern `OrderRequest` ở GĐ4: Domain không biết gì về MVC, ViewModel `ContactInput` (DataAnnotations) map sang bằng `ToRequest()`.
- Đăng ký hết trong `Infrastructure/DependencyInjection.cs`.
- `IContentQueryService.GetNewsDetailAsync` phải `Include(a => a.Category)` sau khi đổi model.

### 3.4 Controllers & Routes (attribute routing — thắng fallback `{slug}`)

```
NewsController
  GET  /tin-tuc                → Index(string? chuyenMuc, string? q, int page = 1)

ContactController
  GET  /lien-he                → Index()
  POST /lien-he                → Index(ContactInput input)   [ValidateAntiForgeryToken] + PRG
```

- POST `/lien-he` hợp lệ → lưu DB → `TempData` → redirect GET `/lien-he?sent=1` → hiện khối cảm ơn thay form (PRG, F5 không gửi trùng).
- Cả 2 route đều là literal 1 segment ⇒ **phải dùng attribute routing** để thắng route fallback `{slug}` (đăng ký cuối `Program.cs`).

### 3.5 Upload file — BẢO MẬT (không được làm ẩu)

Cấu hình vào `appsettings.json` (Options pattern, KHÔNG vào DB):

```json
"ContactUpload": {
  "MaxFileSizeMb": 5,
  "AllowedExtensions": [ ".jpg", ".jpeg", ".png", ".webp", ".pdf" ],
  "FolderPath": "uploads/contact"
}
```

Bắt buộc:
- **Đổi tên file thành `Guid.NewGuid().ToString("N") + ext`** — TUYỆT ĐỐI không dùng tên gốc khách gửi (chống path traversal / ghi đè).
- Whitelist **đuôi file** (lấy bằng `Path.GetExtension`, lowercase) **và** kiểm tra `ContentType`. Đuôi lạ ⇒ `ModelState.AddModelError` chứ không throw.
- Giới hạn dung lượng ở cả client (JS) lẫn server (`[RequestSizeLimit]` + kiểm `file.Length`).
- Lưu vào `wwwroot/uploads/contact/yyyy-MM/` (tạo thư mục nếu chưa có). `wwwroot/uploads/` đã có `.gitkeep`; thêm rule `.gitignore` để **không commit file khách upload**.
- Lưu đường dẫn tương đối vào `ContactMessage.AttachmentPath`.

**Chống spam** (đủ cho demo, không cần captcha):
- **Honeypot**: 1 input ẩn bằng CSS (không dùng `type=hidden`), tên vô hại kiểu `website`. Có giá trị ⇒ giả vờ thành công nhưng KHÔNG lưu DB.
- **Chặn tần suất bằng Session**: lưu timestamp lần gửi cuối, dưới 30 giây ⇒ báo lỗi lịch sự.

---

## 4. Design system tái dùng (KHÔNG chế lại)

- **Tokens** (`site.css`): brand `--brand-50…900` (`--brand-500 #CE5F9A`), `--red-500 #EC2D3F`, `--ink-900/700/500/400`, `--line`, `--paper`, `--page #FAF7FA`; gradient `--grad-brand`, `--grad-cta`, `--grad-shine`; radius `--r-sm/md/lg/xl/pill`; shadow `--sh-sm/md/lg/brand`; motion `--t-fast/base/slow`, `--ease-out`, `--ease-spring`. Font `--font-display` (Be Vietnam Pro) / `--font-body` (Nunito Sans).
- **Có sẵn, dùng lại ngay:** `.btn-rz` + `.btn-buy/.btn-brand/.btn-ghost/.btn-cart-outline`, `.badge-*`, `.rz-input` + `.rz-textarea` + `.co-error` + `.co-label` (đã viết ở `cart.css` cho form checkout — **tách phần input dùng chung sang site.css** để form liên hệ xài lại thay vì copy), `.section-head` (partial `_SectionHeader`), `.rz-breadcrumb` (partial `_Breadcrumb`), `_Pagination`, `.prose` (nội dung rich text), `.reveal` (site.js tự xử lý, stagger 60ms), `RZ.toast()`.
- **Swiper 11** đã tải local ở `wwwroot/lib/swiper` (dùng nếu cần carousel).
- File mới: `wwwroot/css/news.css`, `wwwroot/css/contact.css` (nạp bằng `@section Styles`), `wwwroot/js/news.js`, `wwwroot/js/contact.js` (nạp bằng `@section Scripts`). Phần dùng chung → `site.css`.

---

## 5. Đặc tả UI từng màn (làm cho ĐẸP — phần này quyết định chất lượng)

### B1. `/tin-tuc` — Trang tin công nghệ

**Kim chỉ nam:** cảm giác như một chuyên trang tin thật (VnExpress Số hóa / The Verge) nhưng mang màu hồng RenewZone. Ưu tiên **ảnh lớn + khoảng thở + typography rõ tầng bậc**.

**5.1 Page hero** (dải nền `--brand-50`, bo đáy `--r-xl`, có hoa văn chấm mờ hoặc vệt gradient hồng nhạt góc phải):
- Breadcrumb (partial có sẵn) → h1 `NewsPageTitle` (fallback "Tin công nghệ") + phụ đề `NewsPageIntro`.
- Bên phải: **ô tìm bài viết** dạng pill (giống `.search-pill` header), submit GET về `/tin-tuc?q=`.

**5.2 Khối bài nổi bật** — CHỈ hiện ở trang 1, không lọc, không tìm kiếm:
- Grid desktop `7fr 5fr`:
  - **Bài chính (trái):** ảnh 16:9 full card, overlay `linear-gradient(to top, rgba(23,20,26,.85), transparent 60%)`, nội dung đè lên đáy: tag pill `--brand-500`, tiêu đề trắng `--font-display` 800 ~1.6rem (2 dòng, ellipsis), meta ngày + lượt xem. Hover: ảnh `scale(1.06)` 600ms, overlay đậm thêm.
  - **2 bài phụ (phải):** card ngang gọn — thumb 120×90 bo `--r-sm` bên trái, phải là tag nhỏ + tiêu đề 2 dòng + ngày. Hover: nền `--brand-50`, tiêu đề `--brand-600`, dịch phải 4px.
- Mobile: xếp dọc, bài chính giữ overlay, 2 bài phụ thành list.

**5.3 Thanh lọc chuyên mục:**
- Chip `.rz-chip` cuộn ngang được trên mobile (`overflow-x:auto`, ẩn scrollbar): **"Tất cả"** + từng `NewsCategory` (kèm **số bài** dạng `Thiết bị (3)` — số này lấy từ DB, không hardcode).
- Chip active: nền `--grad-brand`, chữ trắng, `--sh-brand`. Chip thường: viền `--line`, hover viền `--brand-400`.
- Là **link querystring** (`?chuyen-muc=thiet-bi`), không phải AJAX — SEO tốt + đơn giản. Giữ nguyên `q` khi đổi chuyên mục và ngược lại.
- Khi đang lọc/tìm: hiện dòng kết quả *"Tìm thấy N bài viết cho «keyword»"* + nút **"Xóa bộ lọc"** (chip viền đứt có icon `bi-x`).

**5.4 Danh sách bài (khối chính):**
- Card ngang, cách nhau 20px, nền `--paper`, bo `--r-lg`, viền `--line`, `--sh-sm`.
- Grid `320px 1fr` (desktop) — ảnh trái tỉ lệ 16:10 bo `--r-md` (`overflow:hidden`, ảnh `scale(1.05)` khi hover card), phải:
  tag pill nhỏ · h3 tiêu đề (hover `--brand-600`, 2 dòng) · meta hàng ngang (`bi-calendar3` ngày giờ · `bi-eye` lượt xem · `bi-clock` "≈ N phút đọc" tính từ độ dài `ContentHtml`) · excerpt 2 dòng `--ink-500` · dòng **"Đọc tiếp →"** màu brand, mũi tên trượt phải 4px khi hover.
- Cả card là 1 link — dùng **`.stretched-link`** (đúng cách project đang làm ở `_ProductCard`, `_CheckoutSteps`), KHÔNG lồng `<a>` trong `<a>`.
- `< 768px`: ảnh lên trên (16:9 full width), nội dung xuống dưới.
- **Reveal-on-scroll**: thêm class `.reveal` từng card (site.js tự stagger 60ms).

**5.5 Phân trang:** dùng partial `_Pagination` sẵn có, giữ nguyên `chuyenMuc` + `q` trên URL. Page size **9 bài**.

**5.6 Trạng thái rỗng:** icon `bi-newspaper` cỡ lớn `--brand-200` + "Chưa có bài viết nào phù hợp" + gợi ý + nút "Xem tất cả bài viết".

**5.7 Cuối trang:** khối CTA mảnh nền `--grad-brand` bo `--r-lg`: *"Cần tư vấn chọn máy? Gọi ngay {hotline}"* + nút trắng — hotline đọc từ SiteSettings.

### B2. `/lien-he` — Trang liên hệ

**5.8 Head:** breadcrumb + h1 "Liên hệ" + `ContactIntro` (từ setting).

**5.9 Hàng kênh liên hệ nhanh** (4 tile, grid 4 cột → 2 cột `<md`):
- Mỗi tile: icon tròn 52px nền `--brand-50` (hover đổi `--grad-brand` + chữ trắng), tên kênh, giá trị (số điện thoại / "Chat ngay" / "Chỉ đường").
- Nguồn dữ liệu: **ContentBlock group `contact.channels`** (icon + title + subtitle + link) — hoặc dựng từ SiteSettings nếu block rỗng. Hover: nâng `translateY(-4px)` + `--sh-md`.

**5.10 Grid 2 cột `1.2fr 0.8fr`** (dưới `lg` xếp dọc, form lên trước):

**Cột trái — Form (card trắng `--r-lg` `--sh-sm`, padding 24px):**
- Tiêu đề nhỏ "Gửi tin nhắn cho chúng tôi" + dòng phụ "Shop phản hồi trong giờ làm việc".
- Fields (grid 2 cột, `<sm` 1 cột) — **dùng lại `.rz-input` / `.co-label` / `.co-error` của checkout**:
  1. Họ và tên * · 2. Số điện thoại * (regex `^(0|\+84)\d{9,10}$`) · 3. Email (optional, `[EmailAddress]`) · 4. Địa chỉ (optional)
  5. **Chủ đề** — `<select class="rz-input">` đổ từ setting `ContactSubjects` (tách bằng `|`), option đầu "-- Chọn chủ đề --"
  6. **Nội dung** * — textarea 5 dòng + **đếm ký tự** `0/1000` góc phải dưới (đỏ khi vượt)
  7. **Đính kèm** — xem 5.11
  8. **Honeypot** ẩn (`.hp-field { position:absolute; left:-9999px }`, `tabindex="-1"`, `autocomplete="off"`)
- Nút submit `.btn-rz .btn-buy .btn-lg` full width "Gửi liên hệ" + icon `bi-send`; submit → disable + spinner (chống double-submit — **nhớ bẫy ở GĐ4: phải kiểm `$(form).valid()` trước khi khóa nút**, vì listener của mình chạy trước jquery-validation).
- Lỗi: viền `--red-500` + message đỏ + icon + rung `shake` 300ms (CSS đã có ở cart.css → chuyển `@keyframes shake` sang site.css).

**5.11 Vùng đính kèm file (điểm nhấn UI):**
- **Dropzone** viền đứt 2px `--line` bo `--r-md`, nền `--page`, cao ~110px, giữa là icon `bi-cloud-arrow-up` + "Kéo thả file vào đây hoặc **bấm để chọn**" + dòng nhỏ "JPG, PNG, WEBP, PDF · tối đa 5MB".
- `dragover`: viền `--brand-400`, nền `--brand-50`, icon `scale(1.1)`.
- Chọn xong: dropzone thu lại, hiện **chip file** (icon theo loại · tên file ellipsis · dung lượng đọc được kiểu "1,2 MB" · nút `bi-x` xóa). Ảnh thì hiện **thumbnail 40px** đọc bằng `URL.createObjectURL`.
- JS validate ngay: sai đuôi/quá nặng ⇒ `RZ.toast(msg,'error')` + không gán file. **Server vẫn phải validate lại** (client chỉ là trải nghiệm).

**Cột phải — Thông tin cửa hàng (sticky `top: 92px`):**
- Card 1 "Thông tin cửa hàng": các dòng icon + nội dung, TẤT CẢ từ SiteSettings — địa chỉ, giờ mở cửa, hotline (link `tel:`), email (link `mailto:`), MST.
- Card 2 "Kết nối": nút social tròn (Facebook/Messenger/Zalo/Maps) từ settings, hover nảy nhẹ.
- Card 3 (tùy chọn, đẹp): "Giờ làm việc" dạng bảng T2–CN + chấm trạng thái **"Đang mở cửa" / "Đã đóng cửa"** tính bằng JS so giờ hiện tại với `OpenHours` — nếu parse không được thì ẩn chấm (fallback an toàn).

**5.12 Bản đồ:** full-width dưới cùng, `<iframe>` từ `MapEmbedUrl`, bọc div bo `--r-lg` `overflow:hidden`, `aspect-ratio: 21/9` (desktop) / `4/3` (mobile), `loading="lazy"`, `title` rõ ràng. **Setting rỗng ⇒ ẩn hẳn khối bản đồ** (đừng hiện iframe trắng). Góc trên phải overlay nút `.btn-rz .btn-brand .btn-sm` "Chỉ đường" → `MapUrl`.

**5.13 Trạng thái gửi thành công** (sau PRG, `?sent=1`):
- Thay chỗ form bằng khối canh giữa: **SVG check vẽ nét** (tái dùng `.done-check` chuyển sang site.css) + "Đã gửi liên hệ thành công!" + "Shop sẽ phản hồi trong thời gian sớm nhất" + 2 nút "Về trang chủ" / "Gửi liên hệ khác" (nút sau reload `/lien-he` sạch querystring).

### B3. 2 trang sửa chữa (`/sua-chua`, `/sua-chua-phan-cung`)

- **Chỉ cần seed thêm 2 `StaticPage`** — view `Slug/StaticPage.cshtml` + `.prose` đã render sẵn, không phải viết trang mới.
- Nội dung seed phải **phong phú và tự viết** (Lưu ý 6.2 — không copy site gốc): `<h2>` các nhóm dịch vụ, `<ul>` hạng mục, `<table>` bảng giá tham khảo, đoạn quy trình 4 bước, dòng cam kết. Đủ dài để trang không trống trải.
- Kiểm tra `.prose` đã có style cho `table` chưa — chưa thì **bổ sung style bảng vào `.prose`** (header nền `--brand-50`, viền `--line`, bo góc, cuộn ngang `overflow-x:auto` trên mobile).
- 2 slug này phải khớp link menu đang có (`/sua-chua`, `/sua-chua-phan-cung`) và menu giờ đọc từ `MenuItem` nên seed MenuItem phải trỏ đúng.

---

## 6. Animation & Micro-interaction

Chỉ animate `transform`/`opacity` (trừ ngoại lệ đã cho phép), 60fps, **tất cả tôn trọng `prefers-reduced-motion`** (site.css đã có block global, đừng phá).

| Chỗ | Hiệu ứng |
|---|---|
| Card tin (list + featured) | `.reveal` fade-up stagger 60ms; hover: card `translateY(-4px)` + `--sh-md`, ảnh `scale(1.05)` 600ms `--ease-out` |
| Chip chuyên mục | hover viền brand; active có `--sh-brand`; khi đổi trang, chip active `badge-pop` nhẹ |
| Tag pill trên ảnh | vào viewport thì `badge-pop` (keyframe có sẵn) |
| "Đọc tiếp →" | mũi tên `translateX(4px)` khi hover card |
| Dropzone | dragover: viền + nền đổi, icon `scale(1.1)`; thêm file: chip trượt vào `translateY(8px)→0` + fade |
| Input | focus viền `--brand-400` + glow `0 0 0 4px rgba(206,95,154,.14)` (đã có ở `.rz-input`) |
| Lỗi validate | `shake` 300ms |
| Gửi thành công | SVG check `stroke-dashoffset` 600ms (vòng tròn → dấu tick) |
| Tile kênh liên hệ | hover nâng + icon tròn đổi sang `--grad-brand` |
| Khối CTA cuối trang tin | quét sáng `--grad-shine` khi hover nút |

---

## 7. Responsive

- **Tin tức:** `lg+` featured `7fr 5fr`, list card ngang `320px 1fr`; `md` list `240px 1fr`; `<768px` mọi card xếp dọc, ảnh 16:9 full; chip lọc cuộn ngang (ẩn thanh cuộn, thêm gradient fade 2 mép).
- **Liên hệ:** `lg+` 2 cột `1.2fr 0.8fr` (cột phải sticky); `<lg` 1 cột, **form lên trước** thông tin; kênh liên hệ 4 → 2 cột; map `21/9` → `4/3`.
- Kiểm tra ở đủ 3 mốc **375px / 768px / 1440px**.

---

## 8. Accessibility

- Mỗi input có `<label>` liên kết đúng `for`; lỗi gắn `aria-describedby` + `aria-invalid`.
- Dropzone: là `<button type="button">` hoặc `<label for="file">` để bàn phím dùng được; input file thật ẩn nhưng KHÔNG `display:none` (giữ focus được).
- Chip lọc: `<a>` thật (điều hướng được, mở tab mới được), chip đang chọn có `aria-current="page"`.
- Bộ đếm ký tự + thông báo kết quả tìm kiếm đặt trong vùng `aria-live="polite"`.
- Ảnh bài viết: `alt` = tiêu đề bài. Ảnh trang trí: `alt=""`.
- Tương phản chữ trên overlay ảnh phải đạt **AA** — overlay đủ đậm, không để chữ trắng trên ảnh sáng.
- `<iframe>` map có `title`.

---

## 9. Danh sách file (tạo/sửa)

**Tạo mới — Domain:**
- `Entities/MenuItem.cs`, `Entities/ContentBlock.cs`, `Entities/NewsCategory.cs`
- `Enums/MenuGroup.cs`, `Enums/MenuItemType.cs`
- `Models/NewsListPage.cs`, `Models/ContactRequest.cs`
- `Interfaces/INavigationService.cs`, `IContentBlockService.cs`, `INewsQueryService.cs`, `IContactService.cs`

**Tạo mới — Infrastructure:**
- `Data/Configurations/MenuItemConfiguration.cs`, `ContentBlockConfiguration.cs`, `NewsCategoryConfiguration.cs`
- `Services/NavigationService.cs`, `ContentBlockService.cs`, `NewsQueryService.cs`, `ContactService.cs`
- `Data/Migrations/*_Phase5Content.cs` (sinh bằng lệnh, không viết tay)

**Tạo mới — Web:**
- `Controllers/NewsController.cs`, `Controllers/ContactController.cs`
- `Models/ViewModels/NewsListViewModel.cs`, `ContactViewModel.cs` (chứa `ContactInput`)
- `Models/Options/ContactUploadOptions.cs`
- `ViewComponents/ContentBlocksViewComponent.cs` + `Views/Shared/Components/ContentBlocks/Default.cshtml`
- `ViewComponents/MainMenuViewComponent.cs` + `FooterLinksViewComponent.cs` (+ view tương ứng) — hoặc `@inject INavigationService` thẳng vào `_Layout` nếu thấy gọn hơn (chọn 1, ghi lý do vào CLAUDE.md)
- `Views/News/Index.cshtml`, `Views/Contact/Index.cshtml`
- `Views/Shared/_NewsCard.cshtml` (dùng chung home + tin tức + bài liên quan), `_NewsCardWide.cshtml` (card ngang cho listing)
- `wwwroot/css/news.css`, `wwwroot/css/contact.css`
- `wwwroot/js/news.js`, `wwwroot/js/contact.js`

**Sửa:**
- `Entities/NewsArticle.cs` (bỏ `CategoryTag`, thêm `CategoryId`), `Entities/Category.cs` (thêm `IconClass`), `Entities/SiteSetting.cs` (thêm hằng `SettingKeys`)
- `Data/AppDbContext.cs` (3 DbSet mới), `Data/Seed/SeedData.cs` (seed MenuItem/ContentBlock/NewsCategory + 2 trang sửa chữa + settings mới + gán CategoryId cho 8 bài + `IconClass` cho 6 danh mục cha)
- `Infrastructure/DependencyInjection.cs` (đăng ký 4 service mới)
- `Services/ContentQueryService.cs` (`Include(a => a.Category)`)
- `Views/Shared/_Layout.cshtml` — **xóa `policyLinks`, `CatIcon()`, 2 link sửa chữa, 11 link footer hardcode**; render từ `MenuItem` (cả desktop nav, offcanvas mobile, footer); `FooterAbout` + `FooterDisclaimer` từ settings
- `Views/Slug/Product.cshtml` (khối cam kết → ContentBlock), `Views/Slug/NewsDetail.cshtml` (`CategoryTag` → `Category.Name`, link tag về `/tin-tuc?chuyen-muc=`)
- `Views/Cart/Index.cshtml` + `Views/Checkout/Index.cshtml` (dòng trấn an → ContentBlock; "Shop báo khi xác nhận" → setting `ShippingNote`; khối bank → settings)
- `Views/Checkout/Complete.cshtml` (bank → settings)
- `Models/ViewModels/CheckoutViewModel.cs` (**xóa class `DemoBankInfo`**)
- `Views/Home/Index.cshtml` (dùng `_NewsCard` chung)
- `wwwroot/css/site.css` (nhận `.news-card*`, `.rz-chip`, `.rz-input` group, `.done-check` + `@keyframes shake`/`draw-stroke`), `home.css` + `product.css` + `cart.css` (bỏ phần đã chuyển đi — **kiểm tra không còn định nghĩa trùng**)
- `appsettings.json` (`ContactUpload`), `.gitignore` (bỏ qua `wwwroot/uploads/contact/`)
- `CLAUDE.md` (mục 1, 4.7 mới, mục 5 tick GĐ5, mục 6 lưu ý mới, mục 8 nhật ký)

---

## 10. Bẫy kỹ thuật (đọc kỹ — đều là lỗi đã/sắp gặp trong dự án này)

1. **CSS của partial dùng chung phải ở `site.css`.** GĐ4 đã dính: `.rz-breadcrumb` để trong `product.css` ⇒ trang giỏ hàng render `<ol>` thô "1. 2. 3.". `.news-card` hiện đang **định nghĩa trùng ở cả `home.css` và `product.css`** — gom về `site.css` rồi xóa 2 bản cũ, đừng để 3 bản.
2. **Route mới phải là attribute routing**, nếu để conventional sẽ bị fallback `{slug}` nuốt.
3. **`ISlugResolver` cache 5 phút** map slug→loại. Seed thêm StaticPage mới mà app đang chạy thì `/sua-chua` vẫn 404 tới khi hết cache — restart app khi test.
4. **Đổi `CategoryTag` → `CategoryId` là breaking change**: sửa `NewsDetail.cshtml`, `Home/Index.cshtml`, seed, và `ContentQueryService` phải `Include(a => a.Category)` nếu không sẽ `NullReferenceException` khi render tag.
5. **Không để bài featured lặp ở danh sách dưới** — loại theo Id trước khi phân trang, và nhớ điều này làm lệch `TotalItems` nếu tính ẩu.
6. Form POST viết `action` tay thì FormTagHelper **không tự thêm anti-forgery** ⇒ dùng `asp-antiforgery="true"` (đúng cách `Checkout/Index.cshtml` đang làm). Đừng thêm cả `@Html.AntiForgeryToken()` — 2 token trùng tên làm validate hỏng.
7. **Listener submit của mình chạy TRƯỚC jquery-validation** ⇒ phải `if (window.jQuery && jQuery(form).data("validator") && !jQuery(form).valid()) return;` rồi mới disable nút + spinner (xem `checkout.js`).
8. Enum/`[Required]` phải có **ErrorMessage tiếng Việt**, không thì lọt chuỗi `"The X field is required."` của framework vào HTML.
9. Upload: `<form>` phải có `enctype="multipart/form-data"`, không thì `IFormFile` luôn null.
10. `IMemoryCache` cho menu/content block: **nhớ `Invalidate()`** trong interface ngay từ bây giờ để GĐ6 gọi được.
11. Dừng `RenewZone.Web.exe` trước khi build (khóa DLL). Chạy migration khi app đã tắt.
12. Seed **idempotent** (mỗi hàm check bảng trống trước) — `DbInitializer` chạy mỗi lần app start.
13. Nội dung seed dùng thông tin **giả lập** (Lưu ý 6.2): không dùng địa chỉ/MST/số tài khoản thật, không copy bài viết site gốc.

---

## 11. Tiêu chí nghiệm thu (tự kiểm trước khi báo xong)

**Build & chạy**
- [ ] `dotnet build` **0 warning / 0 error**; migration apply sạch; `dotnet run` không exception trong log.

**Khử hardcode (phần user quan tâm nhất)**
- [ ] `grep` lại `_Layout.cshtml`: **không còn** mảng `policyLinks`, hàm `CatIcon`, link `/sua-chua` viết tay, 11 link footer viết tay.
- [ ] **Đổi 1 dòng trong DB → web khách đổi theo.** Test thật bằng sqlcmd: `UPDATE MenuItems SET Text='Chính sách & Quy định' WHERE ...` → restart (hoặc chờ hết cache 5') → menu đổi. Làm tương tự với 1 `SiteSetting` và 1 `ContentBlock`.
- [ ] `DemoBankInfo` đã bị xóa khỏi code; trang checkout + trang cảm ơn hiện thông tin bank lấy từ DB.
- [ ] Xóa rỗng 1 ContentBlock group → khối đó **ẩn**, trang vẫn bình thường (không khung trống, không lỗi).
- [ ] Xóa 1 SiteSetting → view dùng fallback, **không hiện chuỗi rỗng/ "null"**.

**Trang tin tức**
- [ ] `/tin-tuc` = 200, hiện 3 bài nổi bật + danh sách, bài nổi bật không lặp ở danh sách.
- [ ] `?chuyen-muc=thiet-bi` lọc đúng; `?q=iphone` tìm đúng; kết hợp cả hai vẫn đúng; phân trang giữ nguyên filter.
- [ ] Chuyên mục không tồn tại / không có kết quả → trạng thái rỗng đẹp, không 500.
- [ ] Click bài → trang chi tiết (đã có từ GĐ3) vẫn chạy, tag hiện tên chuyên mục và link về `/tin-tuc?chuyen-muc=`.

**Trang liên hệ**
- [ ] `/lien-he` = 200; thông tin cửa hàng khớp SiteSettings.
- [ ] Gửi thiếu trường bắt buộc → lỗi tiếng Việt + shake, **không mất dữ liệu đã nhập**.
- [ ] Gửi hợp lệ (kèm ảnh) → PRG → khối thành công; **verify sqlcmd**: `SELECT TOP 1 * FROM ContactMessages ORDER BY Id DESC` đúng dữ liệu + `AttachmentPath` trỏ file có thật trong `wwwroot/uploads/contact/`; tên file đã đổi thành GUID.
- [ ] Upload `.exe` hoặc file > 5MB → bị chặn ở **cả client lẫn server** (test server bằng curl bỏ qua JS).
- [ ] Điền honeypot → không có dòng mới trong DB nhưng vẫn báo thành công.
- [ ] Gửi 2 lần liên tiếp trong 30s → bị chặn lịch sự.
- [ ] F5 sau khi gửi → **không tạo bản ghi trùng**.
- [ ] `MapEmbedUrl` rỗng → khối bản đồ ẩn hẳn.

**Trang sửa chữa**
- [ ] `/sua-chua` + `/sua-chua-phan-cung` = 200, nội dung phong phú, bảng giá hiển thị đẹp và cuộn ngang được trên mobile.

**Không hồi quy**
- [ ] `/`, `/iphone`, `/san-pham`, `/tim-kiem?q=`, slug SP, `/gio-hang`, `/dat-hang`, `/gioi-thieu`, `/admin` đều 200; URL rác vẫn 404.
- [ ] **Luồng mua hàng GĐ4 còn nguyên**: thêm giỏ → đặt hàng → có Order trong DB.
- [ ] Menu desktop + offcanvas mobile + footer render đúng như trước khi refactor (so sánh mắt thường).

**Chất lượng giao diện**
- [ ] Kiểm 375 / 768 / 1440px: không tràn ngang, không chữ đè ảnh, không card vỡ.
- [ ] `prefers-reduced-motion`: tắt hiệu ứng, chức năng vẫn đủ.
- [ ] Không còn định nghĩa CSS trùng (`grep -c "\.news-card"` trên 3 file css).

---

## 12. Thứ tự thực hiện gợi ý

1. **Domain + Infrastructure trước**: entities → configurations → DbSets → migration → seed → services → DI. Build sạch rồi mới đụng UI.
2. **Refactor `_Layout` + PDP/giỏ/checkout theo DB** (phần A) — làm sớm để phát hiện sai sót khi mọi trang cũ vẫn phải chạy y như cũ.
3. **Dọn CSS chung** (phần C) — làm trước khi viết news.css/contact.css để không copy nhầm code sắp bị xóa.
4. **Trang tin tức** (B1) → **trang liên hệ** (B2) → **seed 2 trang sửa chữa** (B3).
5. Smoke test toàn bộ theo mục 11 → cập nhật `CLAUDE.md` → commit.

---

## 13. Kim chỉ nam thẩm mỹ

Giao diện phải **nhất quán với trang chủ / PDP / giỏ hàng đã có**: hồng brand tinh tế làm điểm nhấn chứ không phủ kín, giá và CTA dùng đỏ, bo góc mềm, bóng nhẹ nhiều tầng, chuyển động có chủ đích và ngắn.

Trang tin phải cho cảm giác **"đáng đọc"** — ảnh lớn, tiêu đề rõ tầng bậc, nhiều khoảng thở, không nhồi nhét. Trang liên hệ phải cho cảm giác **"đáng tin"** — thông tin cửa hàng minh bạch, form gọn gàng không dọa người dùng, phản hồi tức thì ở mọi thao tác.

Ưu tiên **cao cấp, gọn gàng, đáng tin** hơn là hiệu ứng phô trương. Nếu phải chọn giữa "thêm một animation" và "thêm 16px khoảng trắng" — chọn khoảng trắng.
