# PROMPT — Giai đoạn 7: Hoàn thiện & Ra mắt (RenewZone Clone)

> Copy toàn bộ file này gửi cho session Claude Code mới (đang mở tại `d:\Learn\lam_viec\renew_zone`).
>
> **Mục tiêu:** Đưa website từ "chạy được" lên **"sẵn sàng cho người thật xem"** — chuyên nghiệp khi Google index, tử tế khi khách gặp lỗi, nhẹ và nhanh khi tải ảnh, và trả nốt món nợ kỹ thuật cuối cùng (lịch sử trạng thái đơn). Đây là **giai đoạn cuối** — xong là đóng dự án.
>
> **Bối cảnh:** Giai đoạn 0→6 đã xong toàn bộ chức năng: web khách đầy đủ + trang quản trị 22 màn, chủ shop tự vận hành được, sửa gì trong admin web khách đổi ngay. Giai đoạn 7 **không thêm chức năng nghiệp vụ mới** — chỉ mài giũa những thứ khiến sản phẩm trông "làm cho xong" khác với "làm chỉn chu".
>
> **Câu hỏi phải trả lời được khi xong:** *"Một người lạ gõ nhầm URL thì thấy một trang lỗi đẹp và biết đường về; Google index trang sản phẩm thì hiện đúng giá + ảnh trong kết quả tìm kiếm; admin upload ảnh 8MB thì hệ thống tự nén còn vài trăm KB; và khi có tranh chấp đơn hàng, admin tra được ai đã đổi trạng thái, lúc nào."*

---

## 0. Cách làm việc (đọc trước khi code)

1. **Đọc `CLAUDE.md` trước tiên** — đặc biệt mục **1** (trạng thái hiện tại), **4.2** (schema), **4.3** (routing slug), **4.6** (đơn hàng), **4.9** (nền tảng admin + luồng trạng thái đơn + cache tự động), và **toàn bộ Lưu ý mục 6**. Đọc `docs/DESIGN.md` mục **2** (tokens), **4** (animation), **7** (a11y) — trang lỗi phải dùng lại ngôn ngữ thị giác của web khách, không chế token mới.
2. Máy: **Windows 11 + PowerShell** (KHÔNG dùng `&&`). NuGet Microsoft.* **pin `9.0.13`** (Lưu ý 6.7). SQL Server `localhost`, DB `RenewZoneClone`. Tài khoản admin: **`admin` / `Admin@123`**.
3. **Giai đoạn này CÓ migration** (bảng `OrderStatusHistory`). Lệnh:
   `dotnet ef migrations add Phase7OrderHistory -p src/RenewZone.Infrastructure -s src/RenewZone.Web -o Data/Migrations`
   `dotnet ef database update -p src/RenewZone.Infrastructure -s src/RenewZone.Web`
   **TUYỆT ĐỐI KHÔNG drop DB** — có đơn test đang dùng.
4. **Có thêm 1 NuGet mới**: `SixLabors.ImageSharp` (xem mục 4 — **đọc kỹ phần license + pin version** trước khi cài).
5. App đang chạy sẽ **khóa DLL** → `taskkill /F /IM RenewZone.Web.exe` trước khi build. **Nhớ tắt server sau khi test xong** (bài học phiên trước: dễ quên tiến trình chạy ngầm — kiểm bằng `netstat -ano | findstr :5216`).
6. ⚠️ **Bẫy quan trọng nhất của giai đoạn này:** trang lỗi 500 **chỉ hiện ở môi trường Production**. Trong Development, ASP.NET hiện Developer Exception Page (chủ đích, gate bằng `app.Environment.IsDevelopment()`). Muốn test trang 500 tùy chỉnh phải chạy `ASPNETCORE_ENVIRONMENT=Production dotnet run`. Đừng tưởng trang 500 "không hoạt động" khi thấy trang lỗi xanh của framework.
7. Kết thúc mỗi đợt: **build 0 warning/0 error + thật sự `dotnet run` + smoke test bằng curl/sqlcmd**, không chỉ build. Xong mỗi đợt → tick trong `CLAUDE.md` mục 5, cập nhật mục 4 + thêm dòng nhật ký mục 8, commit tiếng Việt bằng heredoc `git commit -F - <<'EOF'` (**KHÔNG dùng here-string PowerShell `@'...'@`**). Kết `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
8. **Sau khi xong toàn bộ Giai đoạn 7 → dự án ĐÓNG.** Cập nhật mục 1 CLAUDE.md thành "Đã xong Giai đoạn 0→7 (hoàn tất)".

---

## 1. Triết lý giai đoạn hoàn thiện (đọc kỹ — quyết định chất lượng)

Giai đoạn 7 khác các giai đoạn trước ở chỗ: **không có màn hình mới, chỉ có sự chỉn chu**. Ba nguyên tắc:

### 1.1 "Trang lỗi tốt là trang lỗi giúp người ta đi tiếp"
Trang 404 không phải để thông báo "bạn sai rồi" — mà để **cứu vãn** một cú click hụt. Người dùng gõ nhầm URL vẫn là khách hàng tiềm năng. Trang lỗi phải: nói bằng giọng người (không phải mã lỗi), cho công cụ để tìm lại thứ họ cần (ô tìm kiếm, link danh mục phổ biến), và luôn có một lối ra rõ ràng (nút về trang chủ). Đẹp ở đây = *nhã nhặn, hữu ích, đúng thương hiệu* — không phải một con số "404" khổng lồ vô hồn.

### 1.2 "SEO là làm cho máy đọc được thứ mắt người đã thấy"
Web khách đã hiển thị đúng tên/giá/ảnh sản phẩm cho **người**. SEO chỉ là nói lại đúng những thứ đó cho **Google/Facebook/Zalo** bằng ngôn ngữ máy hiểu (thẻ meta, Open Graph, JSON-LD). Không bịa nội dung, không nhồi từ khóa — chỉ cấu trúc hóa cái đã có. Làm đúng thì trang sản phẩm hiện giá + ảnh + đánh giá ngay trong kết quả tìm kiếm Google (rich result), và khi share link lên Zalo/Facebook thì ra thẻ đẹp thay vì link trần.

### 1.3 "Tối ưu là thứ người dùng không thấy nhưng luôn cảm nhận được"
Nén ảnh, đặt `width/height` chống nhảy layout (CLS), `loading=lazy` — không ai khen "ảnh nén tốt", nhưng ai cũng cảm được trang tải nhanh và không giật. Đây là phần **âm thầm** của chất lượng. Nguyên tắc: tối ưu ở **nguồn** (nén lúc upload, một lần) thay vì ở mỗi request.

### 1.4 Không phá vỡ những gì đã chạy
Đây là điều luật cứng nhất. Web khách + admin đang chạy ổn qua 6 giai đoạn. Mọi thay đổi Giai đoạn 7 phải **cộng thêm**, không được sửa hành vi cũ. Sau mỗi đợt: chạy lại bộ smoke test hồi quy (16 route web khách + 15 màn admin chặn login + luồng mua hàng end-to-end) — y như các đợt trước đã làm.

---

## 2. Phạm vi — chia 3 đợt

> Làm xong đợt nào **build sạch + smoke test + commit đợt đó** rồi mới sang đợt sau.

### ĐỢT A — Nợ kỹ thuật: Lịch sử trạng thái đơn hàng
| # | Việc |
|---|---|
| A1 | Entity `OrderStatusHistory` + cấu hình + migration `Phase7OrderHistory` |
| A2 | Ghi lịch sử khi **tạo đơn** (mốc đầu: → New, "Khách đặt hàng") |
| A3 | Ghi lịch sử khi **admin đổi trạng thái** (kèm người đổi + ghi chú tùy chọn) |
| A4 | UI: khối "Lịch sử xử lý đơn" (timeline dọc) ở màn chi tiết đơn + ô nhập ghi chú khi đổi trạng thái |

### ĐỢT B — Trang lỗi + SEO
| # | Việc |
|---|---|
| B1 | Pipeline xử lý lỗi: `UseStatusCodePagesWithReExecute` + `UseExceptionHandler` trỏ về `ErrorController` |
| B2 | Trang **404** (đầy đủ layout khách, ô tìm kiếm, link cứu vãn) — thiết kế mục 5 |
| B3 | Trang **500** (layout tối giản tự chủ, không phụ thuộc DB) — thiết kế mục 5 |
| B4 | `SeoData` + partial `_SeoMeta`: title/description/canonical/Open Graph cho mọi trang khách |
| B5 | JSON-LD structured data: `Product` (PDP), `Organization`/`LocalBusiness` (trang chủ), `BreadcrumbList` |
| B6 | `/sitemap.xml` động (cache) + `/robots.txt` |

### ĐỢT C — Tối ưu ảnh + Kiểm thử tổng thể & đóng dự án
| # | Việc |
|---|---|
| C1 | `SixLabors.ImageSharp` vào `MediaService` (điểm TODO đã đánh dấu sẵn): resize khi upload, strip metadata, nén |
| C2 | Chống CLS: `width`/`height` cho ảnh biết kích thước, `loading=lazy`/`eager` đúng chỗ, preload ảnh hero |
| C3 | Rà soát responsive toàn site (375/768/1280/1920) + `prefers-reduced-motion` toàn site |
| C4 | Kiểm thử end-to-end luồng chính + checklist chất lượng ra mắt (mục 8) |
| C5 | Cập nhật CLAUDE.md → "hoàn tất Giai đoạn 7", commit đóng dự án |

---

## 3. ĐỢT A — Lịch sử trạng thái đơn hàng (trả nợ kỹ thuật)

**Vấn đề:** Hiện đổi trạng thái đơn chỉ ghi đè `Order.UpdatedAt`. Không có dấu vết *ai đổi, lúc nào, từ trạng thái nào sang trạng thái nào, vì sao*. Khi có tranh chấp ("sao đơn của tôi bị hủy?"), admin bó tay. Hệ thống thật bắt buộc phải có sổ nhật ký này.

### 3.1 Entity `OrderStatusHistory` (Domain/Entities)

```csharp
public class OrderStatusHistory : BaseEntity   // BaseEntity: chỉ Id. KHÔNG dùng AuditableEntity
{
    public int OrderId { get; set; }

    /// <summary>Trạng thái trước khi đổi. NULL ở mốc đầu tiên (lúc tạo đơn — chưa có "từ").</summary>
    public OrderStatus? FromStatus { get; set; }

    public OrderStatus ToStatus { get; set; }

    /// <summary>Ghi chú tùy chọn admin nhập khi đổi (vd "khách hẹn giao chiều thứ 7").</summary>
    public string? Note { get; set; }

    /// <summary>
    /// Ai thực hiện — SNAPSHOT tên tại thời điểm đổi (không phải FK sang AspNetUsers).
    /// Cùng triết lý với OrderItem: admin có thể bị xóa sau này, nhưng sổ sách phải giữ nguyên
    /// "ai đã làm". "Khách đặt hàng" cho mốc tạo đơn từ web khách.
    /// </summary>
    public string ChangedBy { get; set; } = null!;

    /// <summary>Thời điểm — set tay trong service (không auto vì đây không phải AuditableEntity).</summary>
    public DateTime ChangedAt { get; set; }

    public Order Order { get; set; } = null!;
}
```

**Quyết định kiến trúc — giải thích rõ trong code:**
- **KHÔNG dùng `AuditableEntity`** và **KHÔNG `ISoftDeletable`**: đây là bản ghi SỔ SÁCH bất biến (append-only ledger), giống `OrderItem`. Không sửa, không xóa mềm. Set `ChangedAt` tay trong service.
- **Snapshot `ChangedBy` (string) thay vì FK `ChangedByUserId`**: FK sang `AspNetUsers` sẽ vỡ nếu tài khoản admin bị xóa, và ta phải Include mỗi lần đọc. Snapshot tên là đủ cho mục đích tra cứu và bất biến theo thời gian — đúng bài học `OrderItem.ProductName` (CLAUDE.md mục 4.2).
- **`FromStatus` nullable**: mốc đầu tiên (tạo đơn) không có trạng thái "từ".
- Cấu hình EF: `OrderStatusHistory → Order` là **Cascade** (xóa đơn — không bao giờ xảy ra vì Order không xóa — thì lịch sử theo cùng). Index `(OrderId, ChangedAt)` để truy vấn timeline nhanh.
- Thêm `DbSet<OrderStatusHistory>` vào `AppDbContext`. Thêm `ICollection<OrderStatusHistory> StatusHistory` vào entity `Order`.
- **Cache**: `OrderStatusHistory` KHÔNG ảnh hưởng cache web khách ⇒ KHÔNG thêm dòng vào `CacheInvalidator.Map` (giống Order/OrderItem).

### 3.2 Ghi mốc đầu khi tạo đơn (A2)

Trong `OrderService.CreateOrderAsync` (Infrastructure, nơi tạo Order + OrderItems), **trong cùng một `SaveChanges`** thêm một `OrderStatusHistory`:
```
FromStatus = null, ToStatus = New, ChangedBy = "Khách đặt hàng", ChangedAt = now, Note = null
```
Đặt trong cùng transaction để lịch sử và đơn không bao giờ lệch nhau. Không cần biết danh tính khách (guest checkout) — "Khách đặt hàng" là đủ.

### 3.3 Ghi mốc khi admin đổi trạng thái (A3)

`OrderAdminService.ChangeStatusAsync` hiện có chữ ký `(int id, OrderStatus newStatus)`. **Đổi thành** `(int id, OrderStatus newStatus, string actor, string? note = null)`:
- `actor`: tên admin đang đăng nhập — **controller truyền vào** (`User.Identity!.Name` hoặc `userManager.GetUserName(User)`). ⚠️ **Giữ Infrastructure KHÔNG biết gì về HttpContext** — truyền tham số, đừng inject `IHttpContextAccessor` xuống Infrastructure chỉ vì tiện.
- Sau khi kiểm luồng hợp lệ (`OrderStatusRules.CanTransition`) và trước `SaveChanges`, thêm một `OrderStatusHistory` với `FromStatus = trạng thái cũ`, `ToStatus = newStatus`, `ChangedBy = actor`, `Note = note`.
- Lịch sử được ghi **trong cùng `SaveChanges`** với việc đổi `Order.Status` ⇒ nguyên tử, không thể có chuyện đổi trạng thái mà quên ghi sổ.
- `GetDetailAsync` bổ sung nạp `StatusHistory` (order by `ChangedAt`) vào DTO `OrderDetail` để view render timeline.

**Controller** (`OrderController.ChangeStatus`): action `[FromBody]` nhận thêm `Note` (nullable, giới hạn ~500 ký tự). Lấy `actor` từ `User`. Ghi chú là **tùy chọn** — không nhập vẫn đổi được.

### 3.4 UI — khối "Lịch sử xử lý đơn" (A4)

Ở màn chi tiết đơn (`Areas/Admin/Views/Order/Detail.cshtml`), thêm **một card mới "Lịch sử xử lý"** (đặt ở cột phải, dưới card trạng thái, hoặc full-width dưới cùng):
- **Timeline dọc** (khác với timeline ngang "pipeline" đã có — cái ngang cho biết đơn đang ở đâu, cái dọc này là NHẬT KÝ đầy đủ mọi lần đổi):
  - Mỗi mục: chấm tròn màu theo `ToStatus` (dùng lại `--adm-{status}` tokens) + đường nối dọc + nội dung.
  - Nội dung mỗi mục: "**[Tên người]** chuyển từ *[trạng thái cũ]* sang **[trạng thái mới]**" · thời gian tuyệt đối (`HH:mm dd/MM/yyyy`) + tương đối ("2 giờ trước") · dòng ghi chú (nếu có) in nghiêng, thụt vào.
  - Mốc đầu (FromStatus null): "**Khách đặt hàng** — đơn được tạo".
  - Mục mới nhất ở **trên cùng** (hoặc dưới cùng — chọn một, nhất quán; gợi ý: cũ→mới từ trên xuống để đọc như dòng thời gian tự nhiên).
- **Đổi UX nút đổi trạng thái**: khi bấm nút chuyển trạng thái (nhất là "Hủy đơn"), mở modal/prompt cho admin **nhập ghi chú tùy chọn** trước khi xác nhận. Dùng lại `RZAdmin.confirm` — cân nhắc mở rộng nó nhận thêm một ô textarea tùy chọn, HOẶC làm một modal riêng nhỏ. Ghi chú gửi kèm trong body AJAX. Không nhập → gửi note rỗng, vẫn chạy.
- CSS timeline dọc đặt trong `admin.css` (mục mới, vd 12b) — **KHÔNG** đặt ở view.

### 3.5 Bẫy Đợt A
- **Chữ ký `ChangeStatusAsync` đổi** ⇒ mọi chỗ gọi phải cập nhật (chỉ có `OrderController`). Interface `IOrderAdminService` cũng đổi.
- Ghi lịch sử phải nằm **trong cùng `SaveChanges`** với đổi trạng thái/tạo đơn — nếu tách 2 lần save, lỗi giữa chừng sẽ làm lệch sổ.
- `ChangedAt` set tay (`DateTime.Now`) vì không phải `AuditableEntity`. Đừng quên, không thì cột ra `0001-01-01`.
- Enum trong JSON body vẫn phải là TÊN (`JsonStringEnumConverter` đã bật từ GĐ6, Lưu ý 6.16) — action nhận `Note` thêm vào record input hiện có.

---

## 4. ĐỢT C (mô tả trước vì liên quan hạ tầng) — Tối ưu ảnh

> Đặt mô tả kỹ thuật ở đây cho liền mạch, nhưng **thực thi ở Đợt C**.

### 4.1 Thư viện: `SixLabors.ImageSharp`

- **⚠️ LICENSE — đọc trước khi cài:** ImageSharp từ v3 dùng **Six Labors Split License** — MIỄN PHÍ cho dự án nguồn mở / cá nhân / doanh thu nhỏ, nhưng **thương mại quy mô lớn phải mua license**. Dự án này là **học tập** nên dùng thoải mái, NHƯNG phải ghi rõ vào CLAUDE.md Lưu ý: *"Nếu thương mại hóa thật thì phải rà lại license ImageSharp hoặc đổi sang thư viện khác (vd `Magick.NET` / `SkiaSharp`)."*
- **Pin version**: chọn bản ImageSharp **3.1.x** ổn định mới nhất, tương thích net9.0. Pin cứng trong `.csproj` (không để dấu `*`/version động — nhất quán với kỷ luật pin 9.0.13 của Microsoft.*). Cài vào project **RenewZone.Web** (nơi có `MediaService`).

### 4.2 Resize + nén trong `MediaService.SaveAsync`

Điểm chèn **đã đánh dấu sẵn**: `// TODO (Giai đoạn 7): resize/nén ảnh tại đây (ImageSharp)` trong [MediaService.cs](src/RenewZone.Web/Services/MediaService.cs) (~dòng 84). Mọi chỗ upload đều đi qua hàm này ⇒ sửa 1 chỗ, không đụng controller nào (đúng như đã thiết kế ở GĐ6).

**Logic:**
- Thêm `MaxWidth` (và có thể `MaxHeight`) vào `UploadProfileOptions` (appsettings.json), mỗi profile một mức: vd Product 1200, Banner 1600, News 1000, Editor 1000, Contact 1600. Mặc định trong C# = 0 hoặc một số an toàn (Lưu ý 6.14: mảng để `[]`, số thì đặt mặc định hợp lý).
- **Chỉ áp cho ẢNH** (jpg/jpeg/png/webp). File PDF (profile Contact) **bỏ qua resize**, ghi thẳng như cũ.
- Luồng mới: nạp ảnh bằng `Image.LoadAsync(stream)` → **chỉ resize khi `image.Width > MaxWidth`** (dùng `ResizeMode.Max` fit trong khung, giữ tỉ lệ; **KHÔNG phóng to** ảnh nhỏ — kiểm điều kiện trước) → **strip metadata** (EXIF/ICC — xóa dữ liệu GPS/thiết bị của khách + giảm dung lượng) → lưu bằng encoder theo đuôi (`JpegEncoder{ Quality = 82 }`, `PngEncoder`, `WebpEncoder{ Quality = 82 }`).
- **Bọc `try/catch`**: file "đuôi .jpg nhưng nội dung hỏng/không phải ảnh" sẽ làm `Image.LoadAsync` ném — bắt lại, trả `MediaResult` lỗi tử tế ("File ảnh không hợp lệ hoặc bị hỏng."), KHÔNG để văng 500. Đây là lớp kiểm bảo mật BONUS: file giả đuôi ảnh không decode được sẽ bị chặn ở đây kể cả khi qua được whitelist ContentType.
- `Delete()` giữ nguyên.

**Cân nhắc (ghi chú, không bắt buộc làm):** chuyển hết sang WebP để nhẹ hơn ~30% — nhưng đổi đuôi file làm phức tạp (đường dẫn đã lưu trong DB). Trong phạm vi GĐ7: **giữ nguyên định dạng, chỉ resize + nén**. Ghi WebP là "cải tiến tương lai".

**Bẫy:**
- Ảnh cũ đã upload TRƯỚC GĐ7 **không được nén lại** (chỉ áp cho upload mới). Chấp nhận được; nếu muốn backfill thì viết lệnh riêng — ngoài phạm vi.
- ImageSharp ngốn RAM với ảnh lớn — `RequestSizeLimit` + `MaxFileSizeMb` per profile đã chặn từ trước, đủ an toàn cho môi trường học tập.
- Test bằng ảnh THẬT nhiều megapixel (không phải file random 500 byte giả PNG như các đợt trước — file giả đó ImageSharp sẽ từ chối decode, đúng nhưng không kiểm được phần resize). Tạo 1 ảnh JPG lớn thật để test dung lượng giảm.

### 4.3 Chống CLS (Cumulative Layout Shift) — C2
- Ảnh **biết trước kích thước** (card sản phẩm, thumbnail tin, avatar) thêm `width`/`height` (px thật hoặc theo tỉ lệ) để trình duyệt chừa chỗ trước khi ảnh tải xong → không nhảy layout. Đây là yếu tố Core Web Vitals mà Google chấm.
- `loading="lazy"` cho ảnh dưới màn hình đầu (đa số đã có); **`loading="eager"` + `fetchpriority="high"`** cho ảnh hero/LCP (ảnh lớn nhất màn đầu) — ngược lại, lazy ảnh hero làm chậm chỉ số LCP.
- Cân nhắc `<link rel="preload">` ảnh hero đầu tiên trong `<head>` (chỉ ảnh đầu của slider).

---

## 5. Thiết kế Trang lỗi (Đợt B — phần design-heavy)

> **Kim chỉ nam:** trang lỗi vẫn là trang của THƯƠNG HIỆU. Dùng lại tokens (`--brand-*`, `--grad-brand`, font, motion) trong `docs/DESIGN.md` mục 2 + 4. Không được trông như trang lỗi mặc định trơ trọi của server.

### 5.1 Nguyên tắc phân biệt 404 vs 500 (quyết định kiến trúc quan trọng)

| | **404 — Không tìm thấy** | **500 — Lỗi máy chủ** |
|---|---|---|
| Nguyên nhân | Routing chạy OK, chỉ là slug không tồn tại ⇒ **DB đang sống** | Có gì đó THROW — **có thể chính DB/service đang chết** |
| Layout | Dùng **`_Layout` đầy đủ** (header, menu, footer, nút nổi) — an toàn vì DB sống, và giúp khách tìm lại đường | **Layout TỐI GIẢN tự chủ** (`_ErrorLayout`) — KHÔNG `@inject` service nào chạm DB |
| Vì sao | Header/footer đọc `ISiteContentService`/`INavigationService` (chạm DB) — với 404 thì OK | Nếu 500 do DB chết mà trang 500 lại gọi service chạm DB ⇒ **trang lỗi tự ném lỗi** → vòng lặp/trang trắng. Phải cắt hoàn toàn phụ thuộc. |
| Giọng điệu | Nhẹ nhàng, hơi dí dỏm, hữu ích | Xin lỗi, trấn an, nghiêm túc |
| Công cụ cứu vãn | Ô tìm kiếm + link danh mục + nút về trang chủ | Nút "Thử lại" + hotline (HARDCODE fallback, không đọc từ setting) + mã lỗi để báo hỗ trợ |

> ⚠️ **Đây là điểm senior nhất của trang lỗi:** trang 500 phải **resilient** — inline vài token CSS cần thiết ngay trong `_ErrorLayout` (hoặc `<style>` trong view), hotline hardcode một số fallback (vd đọc thử setting trong `try/catch`, hỏng thì dùng hằng), KHÔNG `@inject` gì có thể ném. Trang 404 thì thoải mái dùng full layout.

### 5.2 Trang 404 — bố cục & thị giác

- Bố cục: **canh giữa, một cột, nhiều khoảng thở**. Trong `_Layout` đầy đủ nên vẫn có header/footer/nút nổi bao quanh.
- **Illustration SVG tự vẽ** (KHÔNG dùng ảnh ngoài/thư viện): một hình minh họa on-brand — gợi ý: một chiếc hộp/túi mua hàng lạc lối + kính lúp, hoặc màn hình thiết bị hiện "?" , tông hồng brand + gradient `--grad-brand`. Kích thước vừa phải (~200–260px), KHÔNG phải con số "404" khổng lồ sến súa. Nếu dùng chữ "404" thì làm tinh tế (gradient, cỡ vừa, kết hợp icon).
- **Tiêu đề** (h1, font display): giọng người — vd *"Trang này đã đi lạc mất rồi"* / *"Không tìm thấy trang bạn cần"*.
- **Đoạn phụ**: 1–2 câu trấn an + gợi ý — *"Có thể đường dẫn đã thay đổi hoặc sản phẩm không còn nữa. Thử tìm lại bên dưới nhé."*
- **Ô tìm kiếm**: dùng lại style search pill của header, submit về `/tim-kiem?q=`. Đây là công cụ cứu vãn số 1.
- **Chip link danh mục phổ biến**: iPhone · MacBook · iPad · Apple Watch · AirPods (dùng lại `.rz-chip`). Lấy từ menu categories (DB sống) hoặc hardcode top — cân nhắc: đọc từ `INavigationService`/categories cho tự động, nhưng nếu muốn 404 nhẹ thì hardcode 5 link cố định cũng chấp nhận được. **Nêu rõ lựa chọn trong code.**
- **CTA chính**: nút gradient "Về trang chủ".
- **Animation** (tôn trọng `prefers-reduced-motion`): illustration **float nhẹ** (translateY ±6px, ~3s ease-in-out infinite — CHỈ phần trang trí); text + CTA **reveal stagger** khi vào trang (dùng lại cơ chế `.reveal` của `site.js`, hoặc animation CSS đơn giản). Không lố.

### 5.3 Trang 500 — bố cục & thị giác

- Layout tối giản `_ErrorLayout`: chỉ logo wordmark (HTML/CSS, không cần DB) + khối lỗi canh giữa. Nền có thể dùng `--grad-hero` hoặc nền sáng đơn giản.
- **Illustration SVG** khác 404: gợi ý ổ cắm rớt/dây đứt, hoặc icon công cụ sửa chữa — báo hiệu "đang khắc phục", tông trầm hơn 404.
- **Tiêu đề**: *"Đã có lỗi xảy ra"* / *"Hệ thống đang gặp trục trặc"*.
- **Đoạn phụ**: xin lỗi + trấn an — *"Chúng tôi đã ghi nhận sự cố và đang khắc phục. Bạn vui lòng thử lại sau ít phút."*
- **CTA**: "Thử lại" (reload trang trước / `history.back()` hoặc về `/`) + "Gọi hotline" (số **hardcode fallback** — hoặc `try` đọc setting, `catch` dùng hằng `0900.000.000`).
- **Mã lỗi/Request ID**: hiện nhỏ, mờ, monospace ở cuối — kèm nút copy (giống copy mã đơn ở trang cảm ơn GĐ4). Để khách đọc cho hỗ trợ khi gọi. `ErrorViewModel.RequestId` đã có sẵn.
- Animation: tối giản (500 là lúc mọi thứ đang lỗi, không nên "vui"). Chỉ fade-in nhẹ.

### 5.4 Pipeline & Controller (B1)

- **`ErrorController`** (tầng Web, KHÔNG thuộc Area Admin): 
  - `GET /loi/{code}` — nhận status code, chọn view: 404 → `NotFound`, 500 → `ServerError`, còn lại → view chung (`GenericError`, dùng lại template 404 với copy khác). Trả **đúng status code gốc** (StatusCodePages tự giữ, nhưng view 500 thì action set `Response.StatusCode = 500`).
  - Có thể thêm alias `GET /404`, `/500` cho dễ test tay.
- **Program.cs pipeline** — thêm (thứ tự quan trọng):
  ```
  app.UseExceptionHandler("/loi/500");                 // exception chưa bắt → trang 500 (CHỈ chạy khi !IsDevelopment)
  app.UseStatusCodePagesWithReExecute("/loi/{0}");     // 404/403/... → re-execute sang ErrorController, GIỮ status code + URL gốc
  ```
  - **Dùng `WithReExecute` KHÔNG dùng redirect**: re-execute render trang lỗi mà **giữ nguyên URL gốc và status code** (tốt cho SEO + UX). Redirect sẽ đổi URL thành `/loi/404` và trả 302 — SAI (Google coi là trang tồn tại).
  - Đặt sớm trong pipeline (sau `UseHttpsRedirection`, quanh `UseRouting`). Route `/loi/{code}` là 2 đoạn nên KHÔNG lọt fallback `{slug}`. Vẫn phải đảm bảo `ErrorController` đăng ký (route conventional `default` đã cover `/loi/...`? Không — `/loi/{code}` cần attribute route trên action). Dùng attribute routing.
- **⚠️ Bẫy Development (Lưu ý mục 0.6):** `UseExceptionHandler` chỉ gắn khi `!IsDevelopment()`. Test trang 500 phải chạy Production. Trang 404 thì test được ở cả Dev (StatusCodePages không bị gate). Có thể tạm bỏ điều kiện `if (!IsDevelopment)` để test rồi trả lại — nhưng nhớ **trả lại** (Developer Exception Page cực hữu ích khi dev).
- **Admin area lỗi**: nếu URL `/admin/xyz-khong-ton-tai` → cũng ra trang 404 khách? Hơi lệch ngữ cảnh nhưng chấp nhận được cho phạm vi này. Cân nhắc (tùy chọn, không bắt buộc): 404 trong `/admin/*` render một empty-state gọn trong shell admin. **Nêu rõ nếu bỏ qua.**

### 5.5 Danh sách file trang lỗi
- `Areas`-ngoài: `Controllers/ErrorController.cs`
- `Views/Error/NotFound.cshtml` (404, `Layout = _Layout`), `Views/Error/ServerError.cshtml` (500, `Layout = _ErrorLayout`), tùy chọn `GenericError.cshtml`
- `Views/Shared/_ErrorLayout.cshtml` (tối giản, tự chủ)
- `wwwroot/css/error.css` (hoặc gộp vào site.css — cân nhắc: 404 dùng site.css nên có thể để trong site.css mục mới; 500 KHÔNG nạp site.css nên phần CSS của 500 phải inline hoặc trong error.css riêng mà `_ErrorLayout` nạp). **Quyết định: CSS trang 500 tự chứa trong `_ErrorLayout` (inline `<style>` hoặc `error.css` riêng), CSS trang 404 vào site.css.** Ghi rõ lý do (bài học GĐ4: CSS partial dùng chung phải nằm ở file mọi trang dùng nó đều nạp).
- Illustration: SVG inline trong view (không thêm file ảnh).
- `Models/ErrorViewModel.cs` — mở rộng nếu cần (thêm `StatusCode`, `Title`, `Message`).

---

## 6. ĐỢT B — SEO (chi tiết)

### 6.1 `SeoData` + partial `_SeoMeta` (B4)

Hiện `_Layout` đọc `ViewData["Title"]` + `ViewData["MetaDescription"]` rời rạc. Nâng lên một **đối tượng `SeoData`** (Web/Models) mang đầy đủ:
```csharp
public class SeoData
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? CanonicalPath { get; set; }   // đường dẫn tương đối, helper dựng thành absolute
    public string? OgImage { get; set; }          // ảnh share (absolute)
    public string OgType { get; set; } = "website"; // "product" cho PDP, "article" cho tin
    public bool NoIndex { get; set; }             // true cho /gio-hang, /dat-hang, /tim-kiem
}
```
- Controller gán `ViewData["Seo"] = new SeoData {...}` (hoặc dùng một base controller / helper). Trang không gán → `_SeoMeta` tự dựng mặc định từ `SiteSettings` (StoreName + Tagline) — **fallback an toàn**, đúng triết lý GĐ5 (thiếu dữ liệu không làm vỡ).
- **Partial `_SeoMeta.cshtml`** render trong `<head>` của `_Layout` (thay các dòng title/description hiện tại): `<title>`, `<meta name=description>`, `<link rel=canonical>`, `<meta name=robots>` (index/noindex), khối **Open Graph** (`og:title/description/image/url/type/site_name`, `og:locale=vi_VN`), tùy chọn Twitter card.
- **Absolute URL**: canonical + og:url + og:image cần URL tuyệt đối (scheme+host). Viết helper đọc `Request.Scheme` + `Request.Host`. Cẩn thận HTTPS.
- **Gán SeoData cho từng loại trang khách**:
  - PDP (`Slug/Product`): title = tên SP, description = ShortDescription, ogImage = ảnh chính, ogType = "product", canonical = `/{slug}`.
  - Danh mục (`Slug/Category`): title/description theo danh mục; canonical = `/{slug}` (KHÔNG kèm `?series=&page=` để tránh trùng lặp nội dung — hoặc dùng canonical trỏ trang gốc). Trang 2+ cân nhắc `rel=prev/next` (tùy chọn, có thể ghi là cải tiến).
  - Tin (`Slug/NewsDetail`): ogType="article", ogImage = thumbnail.
  - Trang tĩnh, trang chủ, /tin-tuc, /lien-he: title/description hợp lý.
  - **`NoIndex = true`** cho `/gio-hang`, `/dat-hang`, `/dat-hang-thanh-cong`, `/tim-kiem` (trang kết quả tìm kiếm không nên vào index).

### 6.2 JSON-LD structured data (B5)

Nhúng `<script type="application/ld+json">` (render qua `@Html.Raw` với JSON build ở server, **encode cẩn thận** để không vỡ nếu tên SP có ký tự đặc biệt — dùng `System.Text.Json.JsonSerializer`, KHÔNG nối chuỗi tay):
- **PDP → `Product`**: name, image, description, `offers` { price = giá bán (SalePrice ?? OriginalPrice), priceCurrency "VND", availability }. SP "Liên hệ" thì bỏ `offers` hoặc để availability phù hợp. Đây là thứ cho **rich result** (giá + ảnh hiện ngay trong kết quả Google).
- **Trang chủ → `Organization`** (hoặc `LocalBusiness` với địa chỉ/giờ mở cửa/hotline từ SiteSettings): name, url, logo, contactPoint (hotline), sameAs (Facebook/Zalo links).
- **BreadcrumbList** trên PDP + trang danh mục (khớp breadcrumb hiển thị).
- Đặt trong partial riêng (`_JsonLd.cshtml`) hoặc trong `_SeoMeta`. Dữ liệu GIẢ LẬP đúng Lưu ý 6.2 (không dùng địa chỉ/MST thật).

### 6.3 Sitemap + robots (B6)

- **`SitemapController` → `GET /sitemap.xml`** (attribute route, `Produces("application/xml")` hoặc trả `Content(xml, "application/xml")`):
  - Liệt kê: trang chủ + `/san-pham` + `/tin-tuc` + `/lien-he` + toàn bộ **Category IsActive** + **Product IsActive** + **NewsArticle IsActive** + **StaticPage IsActive**.
  - `<loc>` = URL tuyệt đối, `<lastmod>` = `UpdatedAt ?? CreatedAt` định dạng ISO 8601 (`yyyy-MM-dd`), tùy chọn `<changefreq>`/`<priority>`.
  - **KHÔNG liệt kê** trang admin, giỏ, checkout, tìm kiếm.
  - **Cache**: sitemap query nhiều bảng ⇒ cache `IMemoryCache` TTL ngắn (vd 30–60 phút) HOẶC móc vào `ICacheInvalidator` (thêm scope `Sitemap`). Đơn giản nhất: TTL 1 giờ, chấp nhận trễ. Nêu rõ lựa chọn.
  - Build XML bằng `XDocument`/`XmlWriter` (đúng namespace `http://www.sitemaps.org/schemas/sitemap/0.9`), KHÔNG nối chuỗi tay (escape ký tự).
- **`GET /robots.txt`** (controller động để host đúng, HOẶC file tĩnh trong wwwroot):
  - `User-agent: *`, `Disallow: /admin/`, `/gio-hang`, `/dat-hang`, `/tim-kiem`; `Allow: /`; `Sitemap: <absolute>/sitemap.xml`.
  - Vì cần host tuyệt đối cho dòng Sitemap ⇒ **controller động** gọn hơn file tĩnh. Nêu rõ.
- Cả hai route 1-2 đoạn — cẩn thận không lọt fallback `{slug}` (`/robots.txt` có dấu chấm, `/sitemap.xml` cũng vậy — attribute route sẽ thắng, nhưng verify).

### 6.4 Bẫy SEO
- **Canonical/og:url phải absolute + đúng scheme** — sai host là Google index nhầm.
- **JSON-LD build bằng serializer, không nối chuỗi** — tên SP có dấu `"` là vỡ cả script.
- **`NoIndex` cho trang giao dịch** — quên thì Google index trang giỏ hàng rỗng.
- **Sitemap chỉ chứa URL 200** — đưa slug đã ẩn/xóa vào là Google báo lỗi. Lọc `IsActive` (query filter soft-delete tự lo phần đã xóa).
- Test sitemap: `curl /sitemap.xml` phải ra XML hợp lệ (có thể pipe qua xmllint nếu có). Test robots: `curl /robots.txt`.

---

## 7. Danh sách file (tạo/sửa) — tổng hợp 3 đợt

**Đợt A — OrderStatusHistory**
- `Domain/Entities/OrderStatusHistory.cs` (mới) · sửa `Order.cs` (thêm navigation)
- `Infrastructure/Data/Configurations/OrderStatusHistoryConfiguration.cs` (mới) · sửa `AppDbContext` (DbSet)
- `Infrastructure/Data/Migrations/*_Phase7OrderHistory.cs` (sinh ra)
- Sửa `OrderService.cs` (ghi mốc tạo đơn), `OrderAdminService.cs` + `IOrderAdminService.cs` (chữ ký + ghi mốc + nạp history vào detail), `Domain/Models/Admin/OrderAdminModels.cs` (DTO history + `OrderDetail`)
- Sửa `Areas/Admin/Controllers/OrderController.cs` (actor + note), `Areas/Admin/Views/Order/Detail.cshtml` (timeline dọc + ô ghi chú), `wwwroot/css/admin.css` (CSS timeline dọc), có thể `wwwroot/js/admin.js` (mở rộng confirm nhận note)

**Đợt B — Trang lỗi + SEO**
- `Controllers/ErrorController.cs`, `SitemapController.cs`, `RobotsController.cs` (mới)
- `Views/Error/NotFound.cshtml`, `Views/Error/ServerError.cshtml`, (tùy chọn `GenericError.cshtml`), `Views/Shared/_ErrorLayout.cshtml` (mới)
- `Views/Shared/_SeoMeta.cshtml`, `Views/Shared/_JsonLd.cshtml` (mới) · sửa `_Layout.cshtml` (nạp `_SeoMeta`, bỏ 2 dòng title/description cũ)
- `Models/SeoData.cs` (mới) · sửa `ErrorViewModel.cs`
- Sửa các controller/ view khách để gán `SeoData` (Home, Slug/*, Product, News, Contact)
- `wwwroot/css/site.css` (CSS trang 404 + phần error dùng chung) · `wwwroot/css/error.css` HOẶC inline trong `_ErrorLayout` (CSS trang 500)
- Sửa `Program.cs` (pipeline lỗi)

**Đợt C — Tối ưu ảnh + kiểm thử**
- Sửa `RenewZone.Web.csproj` (thêm ImageSharp pin version)
- Sửa `Services/MediaService.cs` (resize/nén tại điểm TODO), `Models/Options/UploadOptions.cs` (thêm `MaxWidth`), `appsettings.json` (MaxWidth mỗi profile)
- Sửa các view có ảnh (thêm width/height, fetchpriority hero) — `_ProductCard`, `_NewsCard`, hero, PDP gallery…
- Sửa `CLAUDE.md` (mọi đợt), thêm nhật ký, cập nhật trạng thái "hoàn tất"

---

## 8. Tiêu chí nghiệm thu

### Chung (mỗi đợt)
- [ ] `dotnet build` **0 warning / 0 error**; `dotnet run` không exception trong log.
- [ ] Web khách **không hồi quy**: `/`, `/iphone`, `/san-pham`, `/tim-kiem?q=`, slug SP, `/gio-hang`, `/dat-hang`, `/tin-tuc`, `/lien-he`, `/gioi-thieu`, `/sua-chua` đều 200.
- [ ] Admin **không hồi quy**: chưa đăng nhập thì mọi `/admin/*` đều 302 về login; luồng mua hàng end-to-end vẫn tạo được đơn.
- [ ] **Nhớ tắt server sau khi test** (`taskkill /F /IM RenewZone.Web.exe`, verify `netstat`).

### Đợt A — Lịch sử trạng thái đơn
- [ ] Tạo đơn mới (từ web khách) → `OrderStatusHistory` có **1 dòng** `null→New`, ChangedBy "Khách đặt hàng" (verify sqlcmd).
- [ ] Admin đổi `New→Confirmed` kèm ghi chú → có dòng mới `New→Confirmed`, ChangedBy = **tên admin**, Note đúng.
- [ ] Bước nhảy sai (`New→Shipping`) vẫn bị từ chối và **KHÔNG** ghi dòng lịch sử rác.
- [ ] Màn chi tiết đơn hiện **timeline dọc** đầy đủ các mốc, đúng thứ tự thời gian, có tên người + ghi chú.
- [ ] Xóa/không có: đơn cũ (tạo trước GĐ7) không có lịch sử vẫn hiển thị bình thường (không vỡ), chỉ thiếu timeline.

### Đợt B — Trang lỗi + SEO
- [ ] `curl /url-rac-xyz` → **status 404** (không phải 302) + **URL giữ nguyên** + trang 404 tùy chỉnh có ô tìm kiếm + link về trang chủ.
- [ ] Chạy **Production**, ép một exception → trang 500 tùy chỉnh hiện, có Request ID, **KHÔNG** trang lỗi trắng/framework. Trang 500 vẫn render được **kể cả khi giả lập DB lỗi** (test: đổi connection string sai tạm thời → trang 500 vẫn ra, không vòng lặp).
- [ ] `curl /sitemap.xml` → XML hợp lệ, chứa trang chủ + SP + danh mục + tin + trang tĩnh (IsActive), **KHÔNG** chứa `/admin`/`/gio-hang`.
- [ ] `curl /robots.txt` → có `Disallow: /admin/` + dòng `Sitemap:` đúng host tuyệt đối.
- [ ] View-source PDP: có `<link rel=canonical>` absolute, khối `og:*`, và `<script type=application/ld+json>` chứa `Product` với giá đúng. Dán JSON-LD vào Google Rich Results Test (thủ công) → hợp lệ.
- [ ] `/gio-hang`, `/dat-hang`, `/tim-kiem` có `<meta name=robots content=noindex>`.
- [ ] Share thử link PDP (kiểm og tags) → title/description/ảnh đúng.

### Đợt C — Tối ưu ảnh + hoàn thiện
- [ ] Upload 1 ảnh JPG **thật, lớn (>2MB, >2000px)** qua màn sản phẩm → file lưu trên đĩa **nhỏ hơn hẳn** (vài trăm KB) + **chiều rộng ≤ MaxWidth** (verify bằng xem kích thước file thật, không phải file giả).
- [ ] Upload file **giả đuôi ảnh** (không decode được) → bị chặn tử tế ("File ảnh không hợp lệ"), KHÔNG văng 500.
- [ ] File PDF (form liên hệ) vẫn upload được, **không** bị đụng resize.
- [ ] Ảnh cũ (trước GĐ7) vẫn hiển thị bình thường.
- [ ] Kiểm **375 / 768 / 1280 / 1920px** toàn site: không tràn ngang, trang lỗi + timeline đơn không vỡ.
- [ ] `prefers-reduced-motion`: mọi animation mới (float illustration, reveal) tắt đúng, chức năng vẫn đủ.
- [ ] Card sản phẩm có `width/height` → không nhảy layout khi tải (kiểm mắt: refresh trang chậm mạng).

### Thước đo cuối cùng (đóng dự án)
- [ ] Một người lạ gõ nhầm URL → thấy trang 404 đẹp, tự tìm được đường về.
- [ ] Dán link sản phẩm vào Google Rich Results Test → hiện giá + ảnh.
- [ ] Toàn bộ luồng: xem SP → chọn biến thể → thêm giỏ → đặt hàng → admin xác nhận (có ghi lịch sử) → giao → hoàn thành, chạy trơn tru end-to-end.
- [ ] `git log` gọn gàng 3 commit GĐ7, working tree sạch, CLAUDE.md ghi "hoàn tất Giai đoạn 7".

---

## 9. Bẫy kỹ thuật tổng hợp (phần lớn là bài học thật của dự án)

1. **Trang 500 chỉ chạy ở Production** — Development hiện Developer Exception Page (Lưu ý 0.6). Test bằng `ASPNETCORE_ENVIRONMENT=Production`.
2. **Trang 500 KHÔNG được phụ thuộc DB/service có thể chết** — dùng `_ErrorLayout` tối giản, hotline hardcode, CSS tự chứa. Nếu không, 500 do DB chết sẽ làm trang 500 tự ném → trang trắng.
3. **`UseStatusCodePagesWithReExecute` chứ không Redirect** — giữ status code + URL gốc (SEO). Redirect trả 302 làm Google tưởng trang tồn tại.
4. **CSS trang 404 vào site.css, CSS trang 500 tự chứa** — vì 500 không nạp site.css (bài học GĐ4: CSS phải nằm ở file mọi trang dùng nó đều nạp).
5. **Razor coi `<text>` của SVG là thẻ điều khiển** (Lưu ý 6.18) — vẽ SVG minh họa trong `@if`/`@for` phải bọc `<g>`.
6. **JSON-LD + XML sitemap build bằng serializer/XDocument, KHÔNG nối chuỗi** — ký tự đặc biệt trong tên SP làm vỡ.
7. **Canonical/og/sitemap URL phải absolute** — dựng từ `Request.Scheme + Request.Host`.
8. **ImageSharp: chỉ resize khi ảnh LỚN HƠN mức max** (đừng phóng to ảnh nhỏ) + **bọc try/catch** cho file hỏng + **strip EXIF** (privacy).
9. **ImageSharp license v3 là split license** — ghi chú rõ, dự án học tập thì OK.
10. **Options có `MaxWidth` mặc định trong C#** — theo Lưu ý 6.14 (binder nối thêm với mảng; số thì đặt mặc định hợp lý, không để 0 gây chia lỗi).
11. **Chữ ký `ChangeStatusAsync` đổi** — cập nhật interface + mọi chỗ gọi. Ghi lịch sử trong CÙNG `SaveChanges` với đổi trạng thái.
12. **`ChangedAt` set tay** (OrderStatusHistory không phải AuditableEntity) — quên là ra `0001-01-01`.
13. **Route `/loi/{code}`, `/sitemap.xml`, `/robots.txt`** dùng attribute routing, verify KHÔNG lọt fallback `{slug}` (đăng ký trước, hoặc nhờ có dấu `.`/nhiều đoạn).
14. **Nhớ tắt server sau test** (bài học phiên trước — dễ để chạy ngầm).

---

## 10. Thứ tự thực hiện gợi ý

**Đợt A** (nhanh, rõ, có giá trị ngay): entity + migration → ghi lịch sử ở tạo đơn & đổi trạng thái → verify sqlcmd → timeline UI + ô ghi chú → commit.

**Đợt B**: pipeline lỗi + `ErrorController` → **test 404 trước** (dễ, chạy Dev được) → trang 500 + `_ErrorLayout` (test Production) → `SeoData` + `_SeoMeta` (title/canonical/OG) → JSON-LD → sitemap + robots → commit. *Lời khuyên: dựng `_SeoMeta` với fallback an toàn TRƯỚC, rồi mới đi gán SeoData cho từng trang — thiếu gán thì vẫn có mặc định.*

**Đợt C**: ImageSharp vào MediaService → test upload ảnh thật → CLS (width/height, fetchpriority) → rà responsive + reduced-motion toàn site → kiểm thử end-to-end đầy đủ → cập nhật CLAUDE.md "hoàn tất" → commit đóng dự án.

---

## 11. Kim chỉ nam thẩm mỹ (trang lỗi & SEO)

- **Trang lỗi vẫn là trải nghiệm thương hiệu**, không phải ngõ cụt. Mỗi trang lỗi phải trả lời được: *"chuyện gì vừa xảy ra?"* (rõ ràng, không đổ lỗi cho khách) và *"giờ tôi làm gì?"* (một lối ra rõ ràng, ít nhất một CTA).
- **Nhất quán với phần còn lại**: cùng font, cùng tông hồng/đỏ, cùng nhịp bo góc và animation. Người dùng phải cảm được đây vẫn là RenewZone dù đang ở trang lỗi.
- **SEO là sự trung thực có cấu trúc**: nói với máy đúng thứ đã nói với người. Không nhồi từ khóa, không bịa đánh giá, không đánh dấu giá sai. Rich result chỉ có giá trị khi nó đúng.
- **Tối ưu là sự tôn trọng thời gian và dữ liệu của người dùng**: nén ảnh là tôn trọng gói cước 3G của khách; strip EXIF là tôn trọng quyền riêng tư của họ; chống CLS là tôn trọng sự tập trung của họ. Không ai thấy, nhưng ai cũng cảm được.

Nếu phải chọn giữa "một hiệu ứng đẹp hơn ở trang 404" và "một trang 500 chắc chắn không bao giờ tự vỡ" — **luôn chọn cái sau**. Trang lỗi mà lỗi là thất bại kép.

Sau khi xong Giai đoạn 7, dự án **đóng lại như một sản phẩm hoàn chỉnh**: chạy được, đẹp, chắc, index được, và một người chưa từng thấy vẫn tự dùng được từ đầu đến cuối — cả khi họ đi nhầm đường.
