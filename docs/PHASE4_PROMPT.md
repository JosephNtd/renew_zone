# PROMPT — Giai đoạn 4: Giỏ hàng & Đặt hàng (RenewZone Clone)

> Copy toàn bộ file này gửi cho session Claude Code mới (đang mở tại `d:\Learn\lam_viec\renew_zone`).
> Mục tiêu: dựng **luồng giỏ hàng + checkout đẹp, mượt, đúng kiến trúc** — không phá vỡ Giai đoạn 0–3.

---

## 0. Cách làm việc (đọc trước khi code)

1. **Đọc `CLAUDE.md` trước tiên** — nắm kiến trúc 3 tầng, design tokens, các Lưu ý mục 6. Đọc thêm `docs/DESIGN.md` các mục **3.13 (giỏ/checkout), 3.18 (toast/mini-cart), 4.4 (fly-to-cart)**.
2. Bám đúng **design system đã có** (`wwwroot/css/site.css` tokens + `.btn-rz` + badge + toast; `wwwroot/js/site.js` có `window.RZ`). KHÔNG tạo token/màu mới.
3. Máy: **Windows 11 + PowerShell** (KHÔNG dùng `&&` khi gợi ý lệnh). NuGet Microsoft.* **pin `9.0.13`**. SQL Server tại `localhost`, DB `RenewZoneClone`.
4. **Order & OrderItem đã có sẵn trong DB (migration InitialCreate)** ⇒ Giai đoạn 4 **KHÔNG cần EF migration mới** (giỏ hàng lưu session, không lưu DB). Chỉ tạo Order khi khách bấm đặt hàng.
5. Trước khi build/run: nếu app đang chạy (`dotnet run`) sẽ **khóa DLL** → dừng tiến trình `RenewZone.Web.exe` + `dotnet run` trước khi `dotnet build`. Sau khi code xong **PHẢI chạy thật `dotnet run` và smoke test** (không chỉ build). Kết thúc: **0 warning / 0 error**.
6. Xong việc → tick checkbox Giai đoạn 4 trong `CLAUDE.md` mục 5, thêm 1 dòng nhật ký mục 8, cập nhật Lưu ý **5b** (bỏ chữ "placeholder"), cập nhật mục 1 trạng thái. Rồi commit git (message tiếng Việt, dùng heredoc `git commit -F - <<'EOF'`, KHÔNG dùng here-string PowerShell `@'...'@`). Kết `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

## 1. Nguyên tắc bắt buộc (nghiệp vụ)

- **Guest checkout** — khách KHÔNG cần tài khoản. Thông tin người nhận lưu thẳng vào `Order`.
- **Giá tính ở SERVER, không tin client.** Khi thêm giỏ / đặt hàng: nạp lại Product + các OptionValue từ DB, tính `UnitPrice = Product.DisplayPrice + Σ(PriceAdjustment các option đã chọn)`. Client chỉ gửi `productId`, `optionValueIds[]`, `quantity`.
- **Sản phẩm "Liên hệ" (`IsContactPrice = true`) KHÔNG được thêm giỏ** (Lưu ý 6.5) — chặn cả UI lẫn server (trả lỗi nếu bị gọi).
- **OrderItem lưu SNAPSHOT** `ProductName` / `VariantDescription` / `UnitPrice` / `Quantity` tại thời điểm đặt — không phụ thuộc product sống (admin xóa SP sau này không hỏng đơn).
- Giá VND `decimal(18,0)`, format `29.990.000đ` — server dùng `FormatExtensions.ToVnd()` (đã có), client dùng `Intl.NumberFormat('vi-VN')` + `"đ"`.
- Mọi POST phải có **anti-forgery token**.
- Thông tin nhạy cảm dùng **giả lập** (Lưu ý 6.2): khối chuyển khoản dùng ngân hàng demo (KHÔNG có SettingKey ngân hàng — hardcode text demo: *"Ngân hàng Demo Bank — STK 0000 0000 0000 — CTY RENEWZONE DEMO — nội dung: [Mã đơn]"*).

---

## 2. Phạm vi Giai đoạn 4

| # | Hạng mục | Route |
|---|---|---|
| A | Trang giỏ hàng (danh sách item + tóm tắt sticky) | `GET /gio-hang` |
| B | Trạng thái giỏ trống (đúng thông điệp gốc) | (trong A) |
| C | Mini-cart offcanvas (mở từ icon giỏ header) | (global, trong layout) |
| D | Trang checkout (form khách + tóm tắt đơn) | `GET/POST /dat-hang` |
| E | Trang cảm ơn (mã đơn + check animation) | `GET /dat-hang-thanh-cong` |
| F | Toast "Đã thêm vào giỏ" (thumb + nút Xem giỏ) | (global JS) |
| G | Fly-to-cart animation (ảnh bay vào icon giỏ) | (global JS) |
| H | Nối nút PDP "Thêm giỏ" + "Mua ngay" (thay placeholder) | (sửa PDP) |

**Các endpoint AJAX** (JSON): thêm / cập nhật SL / xóa / (xóa hết). Badge header cập nhật **không reload trang**.

---

## 3. Kiến trúc Backend

### 3.1 Domain (thuần, không EF)
- `Domain/Models/CartItem.cs`: `Key` (string, định danh dòng), `ProductId`, `ProductName`, `Slug`, `ImageUrl`, `UnitPrice` (đã gồm option), `VariantDescription` (vd `"256GB · Titan Đen · 99%"`), `OptionValueIds` (List<int> để dựng lại/so khớp), `Quantity`; computed `LineTotal => UnitPrice * Quantity`.
- `Domain/Models/Cart.cs`: `List<CartItem> Items`; computed `TotalQuantity`, `Subtotal`, `IsEmpty`.
- **Sinh `Key`** (tất định, không phụ thuộc thứ tự chọn): `productId + "-" + string.Join("-", OptionValueIds.OrderBy(x=>x))`. Cùng product + cùng bộ option ⇒ **cùng Key ⇒ cộng dồn số lượng**; khác option ⇒ dòng mới.

### 3.2 Web/Services — CartService (session, thuần web)
- `Web/Services/ICartService.cs` + `CartService.cs`. Lưu giỏ trong **Session** dưới key `"rz_cart"`, serialize **System.Text.Json**. Truy cập session qua `IHttpContextAccessor`.
- Cần **`builder.Services.AddHttpContextAccessor();`** trong `Program.cs` (HIỆN CHƯA CÓ — phải thêm). Đăng ký `AddScoped<ICartService, CartService>()`.
- API: `Task<Cart> GetCartAsync()`, `Task<CartOpResult> AddAsync(int productId, List<int> optionValueIds, int qty)`, `Task<CartOpResult> UpdateQtyAsync(string key, int qty)`, `Task<CartOpResult> RemoveAsync(string key)`, `Task ClearAsync()`.
- `AddAsync`: nạp Product (kèm OptionGroups.Values) từ DB (dùng `ICatalogQueryService` sẵn có hoặc query trực tiếp qua một service Infrastructure — **không để CartService tham chiếu AppDbContext trực tiếp** vì Web không phụ thuộc Infrastructure.Data; thêm 1 method vào `ICatalogQueryService`, vd `Task<Product?> GetProductForCartAsync(int id)` trả product + option values). Validate: product tồn tại + active + KHÔNG contact-price; optionValueIds phải thuộc đúng product; qty clamp **1..10**. Dựng `VariantDescription` từ các value đã chọn (nối bằng `" · "`).
- `CartOpResult`: `bool Ok`, `string? Message`, `Cart Cart`, `CartItem? Item` (dòng vừa tác động).

### 3.3 Infrastructure — OrderService
- `Domain/Interfaces/IOrderService.cs` + `Infrastructure/Services/OrderService.cs` (dùng `AppDbContext`).
- `Task<Order> CreateOrderAsync(CheckoutInput input, Cart cart)`:
  - Sinh `Code` unique: `"RZ" + yyMMdd + <4 số random>`; nếu đụng unique index thì thử lại (tối đa vài lần).
  - Tạo `Order` (Status=New, PaymentMethod, TotalAmount = cart.Subtotal, thông tin khách) + `OrderItems` snapshot từng dòng. `SaveChangesAsync`.
  - Trả `Order` (đã có `Code`).
- Đăng ký trong `Infrastructure/DependencyInjection.cs`.

### 3.4 Controllers & Routes (attribute routing — thắng fallback `{slug}`)
- `CartController`:
  - `GET /gio-hang` → `Index` (view trang giỏ).
  - `POST /gio-hang/them` → `Add(int productId, List<int> optionValueIds, int quantity=1)` → **JSON**.
  - `POST /gio-hang/cap-nhat` → `Update(string key, int quantity)` → **JSON**.
  - `POST /gio-hang/xoa` → `Remove(string key)` → **JSON**.
  - `GET /gio-hang/mini` → `MiniCart` → trả **partial** `_MiniCartBody` (để offcanvas nạp/refresh) *(hoặc trả JSON rồi render client — chọn partial cho đơn giản)*.
- `CheckoutController`:
  - `GET /dat-hang` → `Index` (nếu giỏ trống → redirect `/gio-hang`). Model gồm `CheckoutInput` + tóm tắt giỏ.
  - `POST /dat-hang` → `Index(CheckoutInput input)`; `[ValidateAntiForgeryToken]`; `ModelState` invalid → trả lại view. Hợp lệ → `OrderService.CreateOrderAsync` → `ClearAsync` → lưu `Code` vào **TempData** → redirect `GET /dat-hang-thanh-cong`.
  - `GET /dat-hang-thanh-cong` → `Complete` — đọc `Code` từ TempData (kèm vài field tóm tắt); nếu không có (refresh/vào thẳng) → redirect `/`.
- Tất cả POST AJAX: nhận **anti-forgery** qua header `RequestVerificationToken`.

### 3.5 Hợp đồng JSON (backend & frontend PHẢI khớp)
```
POST /gio-hang/them   { productId, optionValueIds:[int], quantity }
 → 200 { ok:true, message, cart:{ count, subtotal, subtotalText },
         item:{ key, name, variant, imageUrl, unitPriceText, quantity, lineTotalText } }
 → 200 { ok:false, message }              // vd sản phẩm Liên hệ / hết hiệu lực

POST /gio-hang/cap-nhat { key, quantity }
 → 200 { ok:true, cart:{count, subtotal, subtotalText}, item:{key, quantity, lineTotalText} }

POST /gio-hang/xoa    { key }
 → 200 { ok:true, cart:{count, subtotal, subtotalText}, removedKey }
```
- `subtotalText`, `lineTotalText`, `unitPriceText` là chuỗi đã format `...đ` (server format sẵn — client khỏi tính lại, tránh lệch làm tròn).
- `count` = tổng số lượng (để `RZ.cartBadge(count)`).

### 3.6 ViewModels
- `CheckoutInput` (POST + DataAnnotations): `CustomerName [Required]`, `Phone [Required][RegularExpression(^(0|\+84)\d{9,10}$)]`, `Email [EmailAddress]` (optional), `Address [Required]`, `Note`, `PaymentMethod` (enum, default Cod).
- `CartViewModel` (bọc `Cart`), `CheckoutViewModel` (`CheckoutInput` + `Cart` tóm tắt + khối bank demo), `OrderCompleteViewModel` (Code, CustomerName, Phone, TotalText, PaymentMethod label).

---

## 4. Design system tái dùng (KHÔNG chế lại)

- **Tokens** (`site.css`): `--brand-500 #CE5F9A`, `--red-500 #EC2D3F`, `--page #FAF7FA`, `--ink-900/700/500/400`, `--line`, `--paper`; gradient `--grad-cta`, `--grad-brand`, `--grad-shine`; radius `--r-sm/md/lg/xl/pill`; shadow `--sh-sm/md/lg/brand/red`; motion `--t-fast/base/slow`, `--ease-out`, `--ease-spring`. Font: `--font-display` (Be Vietnam Pro, cho tiêu đề/giá), `--font-body` (Nunito Sans).
- **Buttons**: base `.btn-rz` (+ `.btn-sm`/`.btn-lg`) với modifier: `.btn-buy` (gradient đỏ CTA + quét sáng), `.btn-cart-outline` (viền đỏ), `.btn-brand` (nền hồng), `.btn-ghost`, `.btn-dark-rz`.
- **Badge**: `.badge-installment`, `.badge-off`, `.badge-contact`. Keyframe có sẵn: `badge-pop`, `shine`, `toast-in/out/progress`, `phone-ring`.
- **JS API `window.RZ`** (site.js): `RZ.toast(msg, type)`, `RZ.cartBadge(count)` (đã tự chạy `badge-pop`). **Mở rộng thêm** trong `cart.js`: `RZ.cartToast({name, imageUrl})` (toast giàu, xem 5.F), `RZ.flyToCart(imgEl)` (xem 5.G), `RZ.addToCart(payload, sourceImgEl)` (gọi API + toast + fly + badge + refresh mini-cart).
- **Reveal-on-scroll**: thêm class `.reveal` vào markup, site.js tự xử lý (stagger 60ms). Tôn trọng `prefers-reduced-motion` (đã có pattern ở home.js/product.js).
- **Format tiền**: server `@price.ToVnd()`; client `new Intl.NumberFormat('vi-VN').format(n) + 'đ'`.
- File mới: `wwwroot/css/cart.css` (style trang giỏ/checkout/cảm ơn — nạp qua `@section Styles` ở 3 view đó) + phần **global** (offcanvas mini-cart, toast-cart, fly-clone) đặt vào **site.css** (vì header/offcanvas xuất hiện mọi trang). `wwwroot/js/cart.js` **nạp global** trong `_Layout` (defer), sau site.js.

---

## 5. Đặc tả UI từng màn (phần quan trọng nhất — làm cho ĐẸP)

### A. Trang giỏ hàng `/gio-hang`
**Bố cục desktop:** 2 cột — trái danh sách item (≈ 65%), phải **card Tóm tắt đơn dính (sticky, top offset dưới header)** (≈ 35%). Dưới `lg`: 1 cột, tóm tắt xuống cuối, thêm **thanh CTA dính đáy màn hình mobile** (tổng tiền + nút "Đặt hàng").
- Tiêu đề trang: "Giỏ hàng của bạn" + số lượng. Breadcrumb dùng partial `_Breadcrumb` (đã có).
- **Line item (card trắng, `--r-md`, `--sh-sm`, hover nâng nhẹ + viền `--brand-200`):**
  grid `[thumb 80px] [tên + variant] [đơn giá] [stepper] [thành tiền] [xóa]`.
  - Thumb 80px `--r-sm`, `object-fit:contain`, nền `--paper`, viền `--line`; link về PDP.
  - Tên: `--font-display` 600, hover `--brand-600`. Variant: `--ink-400`, nhỏ, nối `" · "`.
  - **Stepper**: pill viền `--line`, 2 nút tròn 30px `–`/`+` (hover nền `--brand-50`), số ở giữa; số **pop** (`badge-pop`) mỗi lần đổi; nút `–` disabled khi qty=1; chặn qty>10.
  - Thành tiền: đỏ `--red-500`, `--font-display` 700.
  - Nút xóa: icon `bi-trash`, ghost → hover `--red-500`; khi xóa → **row gập** (max-height→0 + opacity, 300ms `--ease-out`) rồi remove DOM; nếu hết item → hiện trạng thái trống (B) không reload.
- **Card Tóm tắt:** rows "Tạm tính" / "Phí vận chuyển" (= *"Shop báo khi xác nhận"* — trung thực cho demo) / divider / **"Tổng cộng"** cỡ lớn đỏ `--font-display` 800. CTA `.btn-rz .btn-buy .btn-lg` full "Tiến hành đặt hàng" → `/dat-hang`. Dưới CTA: dòng trấn an (icon khiên "Thu cũ đổi mới · Bảo hành dài hạn") + hàng icon thanh toán (COD, chuyển khoản).
- **Cập nhật đều qua AJAX** (stepper, xóa): cập nhật lạc quan (đổi UI ngay) → gọi API → nếu lỗi thì revert + `RZ.toast(msg,'error')`. Mọi thay đổi đồng bộ `RZ.cartBadge` + tổng tiền tóm tắt + (nếu mở) mini-cart.

### B. Giỏ trống
Canh giữa, nhiều khoảng thở. **Minh họa SVG túi giấy hồng tự vẽ inline** (dùng `--brand-200/300/400`, KHÔNG tải ảnh ngoài) — file `wwwroot/images/empty-cart.svg` hoặc inline. Thông điệp **đúng bản gốc**: `Không tồn tại sản phẩm nào trong giỏ hàng !` + phụ đề nhẹ ("Hãy chọn cho mình một sản phẩm ưng ý nhé") + `.btn-rz .btn-brand` "Về trang chủ" + `.btn-rz .btn-ghost` "Xem sản phẩm" (→ `/san-pham`).

### C. Mini-cart offcanvas (global)
Bootstrap **offcanvas end** (trượt phải), rộng ~380px (mobile full). Đặt partial `_MiniCart` trong `_Layout`. Icon giỏ header (`.cart-btn`) **JS chặn click mở offcanvas** (giữ `href="/gio-hang"` làm fallback không-JS).
- Header offcanvas: "Giỏ hàng (n)" + nút đóng.
- Body (scroll): danh sách gọn — thumb 56px, tên 1–2 dòng, variant nhỏ, `qty × đơn giá`, nút xóa nhỏ. Trống → mini empty (icon + "Giỏ hàng trống").
- Footer dính: "Tạm tính" + tổng đỏ; 2 nút: `.btn-ghost` "Xem giỏ hàng" (→/gio-hang) + `.btn-buy` "Thanh toán" (→/dat-hang).
- Sau khi **Thêm giỏ**: KHÔNG tự mở offcanvas (tránh phiền) — chỉ toast + fly + badge. Offcanvas chỉ mở khi bấm icon giỏ. Khi mở, nạp/refresh nội dung từ `GET /gio-hang/mini`.

### D. Trang checkout `/dat-hang`
2 cột: trái **form** (≈ 60%), phải **Tóm tắt đơn read-only** (≈ 40%, sticky) — liệt kê item (thumb 48px, tên, variant, qty×giá) + tổng. Mobile: tóm tắt gập lên đầu dạng accordion "Xem đơn hàng (n sản phẩm) — tổng …".
- **Form** chia nhóm có tiêu đề nhỏ:
  1. *Thông tin nhận hàng*: Họ tên\*, Số điện thoại\*, Email (tùy chọn), Địa chỉ nhận\* (textarea 2 dòng).
  2. *Ghi chú* (textarea, optional).
  3. *Phương thức thanh toán*: 2 **radio-card** (không phải radio trần) — "Thanh toán khi nhận hàng (COD)" (mặc định chọn) + "Chuyển khoản ngân hàng". Card active: viền `--brand-500` + nền `--brand-50` + chấm tick. Chọn "Chuyển khoản" → **hé lộ panel thông tin bank demo** (slide-down) với dòng nội dung CK gợi ý.
- **Input**: cao 46px, `--r-sm`, **label nổi phía trên**, focus viền `--brand-400` + glow (box-shadow brand nhạt). Lỗi: viền `--red-500` + message đỏ nhỏ + icon `bi-exclamation-circle` + **rung `shake` 300ms**. Dùng **jquery-validation-unobtrusive** (đã có trong `_ValidationScriptsPartial`) cho client + DataAnnotations server.
- Submit: `.btn-rz .btn-buy .btn-lg` full "Đặt hàng ngay"; khi submit → disable + spinner (tránh double-submit). **Checkout là POST form thường** (không AJAX) cho chắc chắn/robust; lỗi validate render lại có thông báo; thành công redirect sang E.
- Trên form: link nhỏ "← Quay lại giỏ hàng".

### E. Trang cảm ơn `/dat-hang-thanh-cong`
Card canh giữa, thoáng. **Icon check tròn xanh vẽ nét bằng SVG** (`stroke-dashoffset` animate 600ms: vẽ vòng tròn rồi vẽ dấu tick). Tiêu đề "Đặt hàng thành công!"; **Mã đơn** cỡ lớn `--font-display` (nút copy nhỏ + `RZ.toast('Đã copy mã đơn')`). Tóm tắt: tên khách, SĐT, tổng tiền (đỏ), phương thức thanh toán; nếu Chuyển khoản → nhắc lại thông tin bank + nội dung CK = mã đơn. Dòng "Shop sẽ gọi xác nhận trong ít phút". Nút: `.btn-brand` "Về trang chủ" + `.btn-ghost` "Tiếp tục mua sắm" (→/san-pham). (Guard: vào thẳng URL mà không có TempData → về `/`.)

### F. Toast "Đã thêm vào giỏ" (`RZ.cartToast`)
Trượt vào góc phải-trên (translateX), nền trắng, **viền trái 4px `--success`**, gồm: thumb sp nhỏ 40px + dòng "Đã thêm vào giỏ" + tên sp (1 dòng ellipsis) + link "Xem giỏ" (mở offcanvas hoặc →/gio-hang) + **progress bar mỏng chạy đáy**, tự tắt 3.5s. Dùng chung `#toastStack` đã có.

### G. Fly-to-cart (`RZ.flyToCart(imgEl)`, DESIGN 4.4)
Khi thêm giỏ: **clone ảnh sp**, đặt `position:fixed` tại rect ảnh nguồn, animate `transform: translate(...) scale(.15)` bay tới tâm icon giỏ header (`.cart-btn`) + `opacity` giảm, **600ms `--ease-out`**; kết thúc → remove clone → `RZ.cartBadge(count)` (badge pop). `prefers-reduced-motion` → bỏ bay, chỉ pop badge. Nguồn: ảnh chính PDP (`#pdpMainImg`) và (nếu làm quick-add ở card) ảnh card.

### H. Nối nút PDP (thay placeholder ở `product.js`)
- Trong `Views/Slug/Product.cshtml`: **thêm `data-value-id="@v.Id"`** vào mỗi nút option (`.pdp-opt`) để gửi đúng OptionValue Id; thêm 1 **stepper số lượng** trước nút hành động (mặc định 1).
- `product.js`: bỏ hàm `pendingCart()` toast placeholder. "Thêm vào giỏ" → thu `productId` + `optionValueIds` (từ `.pdp-opt.is-active[data-value-id]` mỗi nhóm) + qty → `RZ.addToCart(payload, document.getElementById('pdpMainImg'))`. "Mua ngay" → `await RZ.addToCart(...)` rồi `location.href='/gio-hang'` (hoặc thẳng `/dat-hang`). Sản phẩm "Liên hệ" không có 2 nút này (đã đúng sẵn).
- `_ProductCard.cshtml` (tùy chọn, nếu còn thời gian): thêm nút quick-add cho SP **không có option group & không phải Liên hệ** (gửi `optionValueIds=[]`); SP có variant thì nút quick-action vẫn chỉ dẫn về PDP. Không bắt buộc.

---

## 6. Animation & Micro-interaction (chỉ animate `transform`/`opacity`, 60fps)
- Fly-to-cart 600ms (G). Badge pop (có sẵn). Toast add (F).
- Stepper: số pop khi đổi; nút bấm `scale(.9)` active.
- Xóa item: gập chiều cao + fade 300ms.
- Radio-card thanh toán: check mượt, panel bank slide-down (max-height/opacity).
- Reveal-on-scroll cho line item / khối form (class `.reveal`).
- Check thành công (E): SVG `stroke-dashoffset` 600ms.
- **Tất cả** tôn trọng `prefers-reduced-motion` (tắt hiệu ứng, giữ chức năng).

## 7. Responsive
- Giỏ: `lg+` 2 cột; `<lg` 1 cột + **CTA dính đáy mobile** (tổng + Đặt hàng). Line item `<sm` xuống layout 2 hàng (thumb+info trên, stepper+tổng+xóa dưới).
- Checkout: `lg+` 2 cột; `<lg` tóm tắt thành accordion trên đầu.
- Offcanvas mini-cart: full-width `<sm`.

## 8. Accessibility
- Stepper: `aria-label` cho ± , `role`/`aria-live="polite"` cho số lượng & tổng khi cập nhật AJAX.
- Offcanvas: focus trap của Bootstrap; nút đóng có `aria-label`.
- Form: mỗi input có `<label>` liên kết, lỗi gắn `aria-describedby`, `aria-invalid`.
- Radio-card: dùng `<input type=radio>` thật ẩn + label bọc (bàn phím chọn được).
- Tương phản chữ/nền đạt AA; focus ring rõ.

---

## 9. Danh sách file (tạo/sửa)

**Tạo mới:**
- `Domain/Models/CartItem.cs`, `Domain/Models/Cart.cs`
- `Domain/Interfaces/IOrderService.cs`
- `Infrastructure/Services/OrderService.cs`
- `Web/Services/ICartService.cs`, `Web/Services/CartService.cs`, `Web/Services/CartOpResult.cs`
- `Web/ViewComponents/CartBadgeViewComponent.cs` + `Web/Views/Shared/Components/CartBadge/Default.cshtml`
- `Web/Controllers/CartController.cs`, `Web/Controllers/CheckoutController.cs`
- `Web/Models/ViewModels/CartViewModel.cs`, `CheckoutViewModel.cs` (chứa `CheckoutInput`), `OrderCompleteViewModel.cs`
- `Web/Views/Cart/Index.cshtml`, `Web/Views/Checkout/Index.cshtml`, `Web/Views/Checkout/Complete.cshtml`
- `Web/Views/Shared/_MiniCart.cshtml` (offcanvas) + `_MiniCartBody.cshtml` (nội dung refresh) + `_CartLine.cshtml` (tùy chọn dùng chung)
- `wwwroot/css/cart.css`, `wwwroot/js/cart.js`, `wwwroot/images/empty-cart.svg`

**Sửa:**
- `Program.cs`: `AddHttpContextAccessor()`, `AddScoped<ICartService, CartService>()`; (Order/Cart service Infrastructure đã qua `AddInfrastructure`). KHÔNG cần route mới (attribute routing).
- `Infrastructure/DependencyInjection.cs`: đăng ký `IOrderService`; thêm method product-for-cart vào `ICatalogQueryService`/`CatalogQueryService` (hoặc service riêng) để CartService lấy product qua abstraction (Web không đụng AppDbContext).
- `Web/Views/Shared/_Layout.cshtml`: thay `<span class="badge-count">0</span>` bằng `@await Component.InvokeAsync("CartBadge")`; include `_MiniCart`; nạp `cart.css`? (phần global đã ở site.css) + `<script src="~/js/cart.js" defer></script>` sau site.js; đảm bảo có anti-forgery token cho JS (vd đặt `@Html.AntiForgeryToken()` 1 lần trong layout và JS đọc `input[name=__RequestVerificationToken]`).
- `Views/Slug/Product.cshtml`: thêm `data-value-id`, stepper số lượng.
- `wwwroot/js/product.js`: nối add-to-cart/mua-ngay thật (bỏ placeholder).
- `wwwroot/css/site.css`: thêm phần **global** mini-cart offcanvas + cart-toast + fly-clone + stepper (nếu dùng cả ở mini-cart).
- `CLAUDE.md`: tick Giai đoạn 4, cập nhật mục 1 + Lưu ý 5b + nhật ký 8.

---

## 10. Lưu ý kỹ thuật (bẫy hay gặp)
- **KHÔNG cần migration** — giỏ ở session, Order/OrderItem đã có bảng. (Chỉ thêm migration nếu bạn quyết định thêm cột — mặc định KHÔNG.)
- `AddHttpContextAccessor()` hiện **chưa** đăng ký — thiếu là CartService không đọc được session.
- Route AJAX 2 segment (`/gio-hang/them`) không đụng fallback `{slug}` (1 segment) — vẫn nên dùng **attribute routing** cho tường minh.
- Anti-forgery cho `fetch`: gửi header `RequestVerificationToken`. Controller POST gắn `[ValidateAntiForgeryToken]`.
- Session JSON: `CartItem` cần có setter/ctor để `System.Text.Json` deserialize (tránh chỉ `init` không parameterless nếu dùng record positional — kiểm tra round-trip).
- Tính lại giá & `VariantDescription` ở **server** lúc thêm — client gửi id, không gửi giá/tên.
- SP "Liên hệ": server trả `{ok:false, message:"Sản phẩm này vui lòng liên hệ hotline"}`.
- Format tiền để **server trả sẵn chuỗi** trong JSON (khớp `ToVnd()`), client không tự format số tiền tổng để tránh lệch.
- Mã đơn unique — bắt `DbUpdateException` do trùng `Code` và thử lại.
- Dừng `dotnet run`/`RenewZone.Web.exe` trước khi build (khóa DLL). Sau đó chạy lại app và smoke test.

---

## 11. Tiêu chí nghiệm thu (tự kiểm trước khi báo xong)
- [ ] `dotnet build` **0 warning / 0 error**; app `dotnet run` chạy, không exception trong log.
- [ ] Thêm giỏ từ PDP (có variant): đúng giá = base + Σ adjustment; badge tăng; toast + fly chạy; cùng variant cộng dồn, khác variant tách dòng.
- [ ] Thêm SP "Liên hệ" bị chặn (toast lỗi), không vào giỏ.
- [ ] `/gio-hang`: stepper +/- cập nhật thành tiền & tổng & badge qua AJAX (không reload); xóa item mượt; xóa hết → trạng thái trống đúng thông điệp gốc.
- [ ] Mini-cart offcanvas mở từ icon giỏ, hiển thị đúng, 2 nút điều hướng đúng.
- [ ] `/dat-hang`: validate client+server (thiếu tên/SĐT/địa chỉ, SĐT sai định dạng → lỗi + shake); chọn Chuyển khoản hiện panel bank; đặt thành công → tạo Order + OrderItems trong DB (verify sqlcmd: `SELECT TOP 1 * FROM Orders ORDER BY Id DESC` + OrderItems khớp snapshot), giỏ được clear.
- [ ] `/dat-hang-thanh-cong`: hiện mã đơn + check animation; vào thẳng URL không có TempData → về `/`.
- [ ] Reload lại `/gio-hang` sau khi thêm: giỏ vẫn còn (session giữ). Giỏ trống khi mới vào.
- [ ] Responsive: mobile có CTA dính đáy giỏ + accordion tóm tắt checkout; offcanvas full-width.
- [ ] `prefers-reduced-motion`: tắt fly/anim, chức năng vẫn đủ.
- [ ] `/`, `/iphone`, `/san-pham`, `/admin` vẫn 200 (không hồi quy Giai đoạn 0–3).

## 12. Sau khi xong
Cập nhật `CLAUDE.md` (tick Giai đoạn 4, sửa Lưu ý 5b bỏ "placeholder", mục 1 trạng thái, thêm nhật ký mục 8 ghi rõ quyết định: session-cart, Key cộng dồn, không migration, AJAX contract). Commit git tiếng Việt qua heredoc.

**Kim chỉ nam thẩm mỹ:** giao diện phải nhất quán với trang chủ/PDP đã có — hồng brand tinh tế, giá đỏ nổi bật, bo góc mềm, bóng nhẹ, chuyển động mượt và có chủ đích. Ưu tiên cảm giác "cao cấp, gọn gàng, đáng tin" hơn là hiệu ứng phô trương.
