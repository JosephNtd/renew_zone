# DESIGN.md — Hệ thống thiết kế RenewZone Clone

> Bản thiết kế UI/UX hoàn chỉnh cho Giai đoạn 2–5 (web khách) + Giai đoạn 6 (admin).
> Nguyên tắc: **giữ DNA thương hiệu site gốc** (hồng orchid + đỏ giá + nền trắng) nhưng **tinh chỉnh lên chuẩn premium** — sạch hơn, nhiều khoảng thở hơn, chuyển động mượt hơn.
> Căn cứ: đã trích xuất token thật từ CSS site gốc (`:root` của style.css): `--color-main: #d78dba`, `--color-red: #ec2d3f`, `--color-black: #212529`, font Nunito Sans/Mulish/Roboto Slab/Pacifico, dùng Owl Carousel + Slick + Nivo + animate.css + Fancybox.

---

## 1. Định hướng thẩm mỹ (Design Direction)

**Từ khóa:** Tinh khôi – Tin cậy – Năng lượng bán lẻ Việt.

1. **Nền trắng là sân khấu, sản phẩm là diễn viên chính** — giao diện Apple-retail phải để ảnh sản phẩm nổi bật, màu sắc chỉ dùng để dẫn mắt (giá đỏ, CTA hồng-đỏ).
2. **Hồng RenewZone là chữ ký** — không dùng tràn lan, chỉ ở: thanh nav, tiêu đề section, viền hover, chi tiết nhỏ. Đỏ CHỈ dành cho giá & CTA mua.
3. **Bo tròn mềm + đổ bóng khói** — cảm giác "cầm được", hiện đại, thay cho viền cứng của bản gốc.
4. **Chuyển động có mục đích** — mọi animation phải trả lời "nó giúp gì cho người mua?": phản hồi thao tác, dẫn hướng ánh nhìn, tạo cảm giác nhanh.
5. **Mobile-first** — khách VN mua điện thoại chủ yếu trên điện thoại.

---

## 2. Theme Tokens (CSS Custom Properties — copy thẳng vào `site.css` ở Giai đoạn 2)

```css
:root {
  /* ===== BRAND — hồng RenewZone tinh chỉnh từ #d78dba gốc ===== */
  --brand-50:  #FDF2F8;
  --brand-100: #FCE7F1;
  --brand-200: #F8CFE2;
  --brand-300: #F0AACB;   /* ~ #d78dba gốc, sáng hơn 1 nấc */
  --brand-400: #E183AF;
  --brand-500: #CE5F9A;   /* PRIMARY — đậm hơn gốc để chữ trắng đạt contrast */
  --brand-600: #B54983;
  --brand-700: #96386B;
  --brand-800: #772D56;
  --brand-900: #5C2343;

  /* ===== ACCENT ĐỎ — giữ nguyên token site gốc ===== */
  --red-500: #EC2D3F;     /* giá khuyến mãi, CTA mua */
  --red-600: #CC2C32;     /* hover */
  --red-700: #C31829;     /* active/pressed */

  /* ===== NEUTRALS (xám ám hồng nhẹ cho ấm) ===== */
  --ink-900: #17141A;     /* heading */
  --ink-700: #212529;     /* body — token gốc */
  --ink-500: #6C757D;     /* phụ — token gốc */
  --ink-400: #98A0A8;     /* placeholder, meta */
  --line:    #ECE7EC;     /* border mảnh */
  --paper:   #FFFFFF;     /* card */
  --page:    #FAF7FA;     /* nền trang — trắng ngà ám hồng cực nhẹ */

  /* ===== SUPPORT ===== */
  --success: #28A745;     /* còn hàng — token gốc */
  --warning: #F59E0B;     /* sao đánh giá, badge hot */
  --info:    #0D6EFD;

  /* ===== GRADIENTS ===== */
  --grad-cta:   linear-gradient(135deg, #EC2D3F 0%, #C2447E 100%);  /* nút Mua ngay */
  --grad-brand: linear-gradient(135deg, #CE5F9A 0%, #96386B 100%);  /* nav, tiêu đề section */
  --grad-hero:  linear-gradient(120deg, #1B1420 0%, #3A2033 55%, #96386B 130%); /* nền khối hero */
  --grad-shine: linear-gradient(105deg, transparent 40%, rgba(255,255,255,.35) 50%, transparent 60%); /* hiệu ứng quét sáng */

  /* ===== TYPOGRAPHY ===== */
  --font-display: "Be Vietnam Pro", system-ui, sans-serif;  /* heading, giá, nút */
  --font-body:    "Nunito Sans", system-ui, sans-serif;      /* nội dung — nối tiếp site gốc */
  --font-accent:  "Pacifico", cursive;                        /* CHỈ dùng cho tagline header */
  --text-xs: .75rem;  --text-sm: .875rem;  --text-base: 1rem;
  --text-lg: 1.125rem; --text-xl: 1.375rem; --text-2xl: 1.75rem; --text-3xl: 2.25rem;

  /* ===== SPACING (bội số 4px) ===== */
  --sp-1: 4px; --sp-2: 8px; --sp-3: 12px; --sp-4: 16px;
  --sp-5: 20px; --sp-6: 24px; --sp-8: 32px; --sp-10: 40px; --sp-14: 56px; --sp-section: 64px;

  /* ===== RADIUS ===== */
  --r-sm: 8px; --r-md: 12px; --r-lg: 16px; --r-xl: 24px; --r-pill: 999px;

  /* ===== SHADOWS (bóng khói nhiều lớp) ===== */
  --sh-sm:   0 1px 2px rgba(23,20,26,.06), 0 1px 6px rgba(23,20,26,.04);
  --sh-md:   0 4px 12px rgba(23,20,26,.08), 0 2px 4px rgba(23,20,26,.05);
  --sh-lg:   0 12px 32px rgba(23,20,26,.12), 0 4px 8px rgba(23,20,26,.06);
  --sh-brand:0 8px 24px rgba(206,95,154,.35);   /* glow hồng cho CTA hover */
  --sh-red:  0 8px 24px rgba(236,45,63,.35);    /* glow đỏ cho nút mua */

  /* ===== MOTION ===== */
  --t-fast: 150ms; --t-base: 250ms; --t-slow: 420ms;
  --ease-out:    cubic-bezier(.22,.61,.36,1);
  --ease-spring: cubic-bezier(.34,1.56,.64,1);   /* nảy nhẹ — badge, icon */

  /* ===== LAYOUT ===== */
  --container: 1280px;
  --header-h: 76px; --header-h-shrink: 60px;
}
```

**Font tải từ Google Fonts:** Be Vietnam Pro (400/500/600/700/800 — thiết kế riêng cho tiếng Việt, dấu đẹp), Nunito Sans (400/600/700), Pacifico (400, chỉ tagline). `font-display: swap`.

**Breakpoints (theo Bootstrap 5):** `sm 576 · md 768 · lg 992 · xl 1200`. Grid sản phẩm: mobile 2 cột → md 3 cột → lg 4 cột (khớp site gốc 3–4 cột).

---

## 3. Components — đặc tả chi tiết

### 3.1 Topbar (thanh mỏng trên cùng)
- Cao 36px, nền `--grad-brand`, chữ trắng `--text-xs`.
- Trái: 📍 địa chỉ rút gọn · Phải: "Giờ mở cửa 8h30–21h30" + hotline.
- Mobile: chỉ giữ hotline, chạy marquee nhẹ nếu tràn. Ẩn khi cuộn xuống (nhường chỗ header sticky).

### 3.2 Header chính (sticky)
- Nền trắng, `--sh-sm`, cao `--header-h`; cuộn quá 80px → thu về `--header-h-shrink`, logo scale 0.85, thêm bóng `--sh-md` (transition `--t-base`).
- Bố cục 3 khối: **Logo + tagline** (tagline font Pacifico màu `--brand-500`, ẩn ở mobile) · **Ô tìm kiếm pill** lớn ở giữa (bo `--r-pill`, viền `--line`, focus → viền `--brand-400` + glow nhẹ; icon kính lúp trong nút tròn nền `--grad-brand`) · **Cụm phải**: khối hotline (icon điện thoại rung — xem 4.6) + nút giỏ hàng.
- **Nút giỏ hàng:** icon túi + badge đếm tròn nền `--red-500` chữ trắng; badge `transform-origin: center` — khi số thay đổi chạy keyframe `badge-pop` (scale 0→1.25→1, `--ease-spring`).
- Mobile: hamburger trái, logo giữa, giỏ phải; search tụt xuống hàng 2; menu trượt từ trái (offcanvas 300ms).

### 3.3 Navbar + Mega dropdown
- Thanh nav nền `--grad-brand` (kế thừa menu hồng site gốc), chữ trắng 600.
- Item hover: gạch dưới trắng trượt vào từ trái (pseudo-element `scaleX 0→1`, `--t-base --ease-out`).
- **Dropdown 2 cấp** ("Sản phẩm", "Chính sách", "Sửa chữa"): panel trắng bo `--r-md`, `--sh-lg`, mở bằng `opacity 0→1 + translateY(8px→0)` 200ms; mỗi item con có icon danh mục nhỏ + hover nền `--brand-50` chữ `--brand-700`, thụt nhẹ `translateX(4px)`.
- Mũi tên chevron xoay 180° khi mở.

### 3.4 Buttons
| Variant | Nền | Chữ | Dùng cho | Hover |
|---|---|---|---|---|
| `btn-buy` | `--grad-cta` | trắng 700 | Mua ngay, Đặt hàng | nâng -2px + `--sh-red` + quét sáng `--grad-shine` chạy qua 600ms |
| `btn-cart` | trắng, viền 2px `--red-500` | `--red-500` | Thêm giỏ hàng | nền `--red-500` chữ trắng |
| `btn-brand` | `--brand-500` | trắng | CTA phụ (Xem tất cả, form) | `--brand-600` + `--sh-brand` |
| `btn-ghost` | trong suốt, viền `--line` | `--ink-700` | Hủy, back | viền `--brand-400` chữ `--brand-600` |
| `btn-dark` | `--ink-900` | trắng | Admin, footer | `--ink-700` |
- Sizes: sm 34px / md 42px / lg 50px, bo `--r-pill` cho CTA chính, `--r-sm` trong form. Active: scale .97. Loading: spinner thay chữ. Disabled: opacity .5.

### 3.5 Badges
- **"Trả góp 0%"**: góc trên-trái card, nền `--grad-cta`, chữ trắng `--text-xs` 700, bo `--r-sm`, bóng `--sh-sm`.
- **"-18%"** (tự tính từ OriginalPrice/SalePrice): tròn 40px góc trên-phải, nền `--warning`, chữ `--ink-900` 800.
- **"99%" / "98%"** (tình trạng máy): pill viền `--success` chữ `--success` nền trắng.
- **"Liên hệ"**: pill nền `--brand-100` chữ `--brand-700`.
- **"Thanh lý"**: pill nền `--ink-900` chữ `--warning`.

### 3.6 Product Card (linh hồn của site — partial `_ProductCard.cshtml`)
```
┌─────────────────────────┐
│ [Trả góp 0%]      [-18%]│  ← 2 badge góc
│                         │
│      ẢNH 1:1 (contain)  │  ← nền #fff, hover scale 1.06
│                         │
│  ── quick actions ──    │  ← trượt lên khi hover (desktop)
├─────────────────────────┤
│ iPhone 16 Pro Max 256GB │  ← 2 dòng max, ellipsis, hover → --brand-600
│ 34.990.000đ (gạch, xám) │
│ 30.490.000đ (đỏ, 700)   │  ← --red-500, --font-display
│ ★★★★★ · Đã bán 128      │  ← meta nhỏ --ink-400 (tùy chọn)
└─────────────────────────┘
```
- Card: nền `--paper`, bo `--r-lg`, viền 1px `--line`, padding `--sp-4`.
- **Hover (desktop):** nâng `translateY(-6px)` + `--sh-lg` + viền `--brand-200`; ảnh scale 1.06 (transition `--t-slow --ease-out`); thanh quick-action (icon: xem nhanh, thêm giỏ) trượt từ dưới `translateY(100%→0)`.
- Sản phẩm "Liên hệ": thay khối giá bằng badge "Liên hệ" + nút gọi hotline nhỏ; KHÔNG có icon thêm giỏ.
- Toàn card là 1 thẻ `<a>` (SEO + UX), quick-action dùng `stopPropagation`.

### 3.7 Section header (tiêu đề khối trang chủ)
- Kicker nhỏ uppercase `--brand-500` letter-spacing 2px (vd "APPLE CHÍNH HÃNG") → Tiêu đề `--text-2xl` 800 `--ink-900` → gạch chân ngắn 48px `--grad-brand` bo pill.
- Phải: link "Xem tất cả →" — mũi tên trượt sang phải 4px khi hover.

### 3.8 Hero (đầu trang chủ)
- Layout: slider chính (trái, ~70%) + 2 banner phụ xếp dọc (phải, ~30%; mobile ẩn hoặc tụt xuống).
- Slider dùng **Swiper**: effect `fade` + ảnh bên trong parallax nhẹ (scale 1.08→1 mỗi slide — cảm giác thở), autoplay 5s (pause khi hover), loop.
- Pagination bullets custom: chấm `--line` → active kéo dài thành thanh 24px nền trắng (transition width 300ms). Mũi tên tròn glass (nền trắng .8 + blur) hiện khi hover khối.
- Nền khối: `--grad-hero` với 2 đốm sáng radial hồng mờ (pseudo-element, `filter: blur(80px)`) — chiều sâu premium.

### 3.9 Khối danh mục (6 tile)
- Grid 6 cột (mobile 3): tile trắng bo `--r-lg`, ảnh danh mục + tên 600.
- Hover: viền `--brand-300`, ảnh nghiêng nhẹ `rotate(-4deg) scale(1.08)` (`--ease-spring`), nền chuyển `--brand-50`.

### 3.10 Series tabs (trang danh mục — iPhone 11 → 17 Series)
- Hàng pill cuộn ngang (scrollbar ẩn): mặc định nền trắng viền `--line`; active nền `--grad-brand` chữ trắng + `--sh-brand`.
- Chuyển tab: grid sản phẩm fade-out 150ms → fade-in stagger (xem 4.3).

### 3.11 Breadcrumb & Pagination
- Breadcrumb: `--text-sm` `--ink-400`, phân cách `›`, trang hiện tại `--ink-700` 600.
- Pagination: nút tròn 40px; active `--grad-brand` chữ trắng; hover viền `--brand-400`; có Prev/Next + "Trang 1/5" chữ nhỏ (khớp site gốc).

### 3.12 Trang chi tiết sản phẩm (PDP)
- **Gallery (trái, 45%):** ảnh chính vuông bo `--r-lg` + dải thumbnail dưới (thumb active viền 2px `--brand-500`); đổi ảnh = crossfade 250ms; click mở **GLightbox** (lightbox nhẹ thay Fancybox gốc); desktop có zoom-hover (ảnh phóng theo con trỏ).
- **Khối mua (phải, 55%):**
  - Tên `--text-2xl` 800 → hàng meta (tình trạng, đã bán) → **khối giá**: giá sale `--text-3xl` `--red-500` 800 + giá gốc gạch + badge -%.
  - **Variant selector** (4 nhóm: Dung lượng/Màu/Tình trạng/SIM): nút pill viền; chọn → viền `--brand-500` nền `--brand-50` + **tick tam giác góc phải-dưới** (giống TGDĐ — quen mắt khách VN); đổi lựa chọn → **giá chạy số** count-up 300ms + nháy scale 1.06 màu đậm hơn.
  - Hàng nút: `btn-buy` "MUA NGAY" (to, full) + `btn-cart` "Thêm giỏ" + nút hotline nền `--success`.
  - **Khối cam kết**: grid 2×2 icon + text (chính hãng, bảo hành, thu cũ, trả góp) nền `--brand-50` bo `--r-md`.
- **Tabs nội dung** (Thông số / Mô tả / Đánh giá): thanh tab sticky dưới header khi cuộn; gạch active `--grad-brand` trượt theo tab (indicator slide).
- **Sticky buy-bar mobile:** thanh cố định đáy màn hình: giá + 2 nút, hiện khi cuộn qua khối mua (translateY 100%→0).

### 3.13 Giỏ hàng & Checkout
- Line item: ảnh 72px bo `--r-sm` · tên + variant text nhỏ `--ink-400` · **stepper** (– số +, nút tròn 28px, số nhảy pop khi đổi) · thành tiền đỏ · nút xóa (icon thùng, hover `--red-500`, xóa = item gập chiều cao + fade 300ms).
- Tóm tắt đơn: card dính (sticky) bên phải, tổng tiền `--text-2xl` đỏ.
- **Giỏ trống:** minh họa SVG túi giấy hồng + đúng thông điệp gốc *"Không tồn tại sản phẩm nào trong giỏ hàng !"* + `btn-brand` "Về trang chủ".
- Form checkout: input 46px bo `--r-sm`, label nổi phía trên, focus viền `--brand-400` + glow; lỗi viền đỏ + chữ đỏ `--text-sm` + icon ⚠, rung `shake` 300ms; radio thanh toán dạng card chọn (viền hồng khi active).
- **Trang cảm ơn:** icon check tròn xanh vẽ nét (SVG stroke-dashoffset animation 600ms) + mã đơn to + nút về trang chủ.

### 3.14 Tin tức
- **Bài nổi bật:** card lớn ảnh 16:9, overlay gradient đen từ đáy, tiêu đề trắng đè lên, tag chuyên mục pill `--brand-500` góc trên.
- **Danh sách:** ảnh trái 320×200 bo `--r-md` (hover scale 1.05) · phải: tag + tiêu đề (hover `--brand-600`) + meta ngày 🕐 + trích đoạn 2 dòng.
- Chi tiết bài: măng-sét: tag, tiêu đề `--text-3xl`, ngày; nội dung max-width 720px, `--text-lg`, line-height 1.8; box "Bài liên quan" cuối.

### 3.15 Testimonials (trang chủ)
- Swiper 3 card (mobile 1): card trắng bo `--r-xl`, dấu ngoặc kép lớn `--brand-200` góc, nội dung italic, avatar tròn 48px + tên 700 + 5 sao `--warning`.
- Autoplay 6s; drag được; pagination chấm hồng.

### 3.16 Footer
- Nền `--ink-900`, chữ `#B9B3BC`; 4 cột: Về cửa hàng (đọc SiteSettings) · Chính sách · Hỗ trợ · Fanpage/Map.
- Tiêu đề cột trắng 700 + gạch ngắn `--brand-500`; link hover → trắng + translateX(4px).
- Dải cuối: © + MST + "Website học tập, không phải cửa hàng thật" (đúng Lưu ý 6.2).

### 3.17 Cụm nút nổi (floating, phải-dưới)
- 3 nút tròn 52px xếp dọc: **Gọi điện** (nền `--red-500`, animation `phone-ring` — rung lắc 10° mỗi 3s + vòng sóng `pulse-ring` lan ra) · **Zalo** (#0068FF) · **Messenger** (gradient FB).
- Hover: scale 1.12 + tooltip tên trượt ra bên trái.
- **Back-to-top**: nút tròn ghost hiện sau khi cuộn 400px (fade+scale), cuộn mượt về đầu.

### 3.18 Toast & mini-cart
- Thêm giỏ thành công: toast góc phải-trên trượt vào (translateX), nền trắng viền trái 4px `--success`, ảnh sp nhỏ + "Đã thêm vào giỏ" + nút "Xem giỏ", tự tắt 3.5s (progress bar mỏng chạy đáy toast).

### 3.19 Admin theme (Giai đoạn 6 — tách riêng, KHÔNG dùng theme khách)
- Sidebar tối `--ink-900` cố định trái 260px: logo, nhóm menu có icon, item active viền trái 3px `--brand-400` nền white/5%.
- Content nền `#F4F2F5`: page-title + breadcrumb; card trắng bo `--r-md` `--sh-sm`.
- Bảng dữ liệu: header dính, hàng hover `--brand-50`; badge trạng thái đơn theo màu (Mới=info, Xác nhận=brand, Đang giao=warning, Hoàn thành=success, Hủy=red).
- Stat card dashboard: icon tròn nền gradient + số to (count-up animation khi load).

---

## 4. Animation & Micro-interaction (tổng hợp)

| # | Hiệu ứng | Kỹ thuật | Thông số |
|---|---|---|---|
| 4.1 | **Reveal-on-scroll**: section/card fade-up khi vào viewport | IntersectionObserver tự viết (~20 dòng, thay animate.css gốc) — thêm class `.is-inview` | translateY 24px→0 + opacity, `--t-slow --ease-out`, **stagger 60ms** theo index card |
| 4.2 | Hero slider fade + parallax ảnh | Swiper effect fade + CSS scale | 5s/slide, ảnh scale 1.08→1 trong 5s |
| 4.3 | Đổi tab series / lọc | fade-out 150ms → đổi DOM → fade-in stagger | như 4.1 |
| 4.4 | **Fly-to-cart**: ảnh sp thu nhỏ bay vào icon giỏ | clone ảnh, FLIP + `transform` translate+scale(.1), remove khi xong | 600ms `--ease-out`, kết thúc → badge `badge-pop` |
| 4.5 | Giá đổi theo variant | count-up số (requestAnimationFrame) + scale pulse | 300ms |
| 4.6 | `phone-ring` nút gọi | keyframes rotate ±10° ×3 + `pulse-ring` (box-shadow lan) | mỗi 3s |
| 4.7 | Skeleton loading ảnh | nền `--line` + `--grad-shine` chạy ngang lặp | 1.2s linear infinite |
| 4.8 | Nút CTA quét sáng | pseudo-element `--grad-shine` translateX -100%→100% khi hover | 600ms |
| 4.9 | Header shrink khi cuộn | class `.scrolled` toggle bằng JS | `--t-base` |
| 4.10 | Check-mark trang cảm ơn | SVG stroke-dashoffset | 600ms delay 200ms |

**Quy tắc cứng:**
- Chỉ animate `transform` + `opacity` (60fps, không reflow). KHÔNG animate width/height/top trừ trường hợp gập item giỏ hàng.
- Mọi animation bọc trong `@media (prefers-reduced-motion: reduce) { … { animation: none; transition: none } }`.
- Hover-only effect chỉ áp dụng `@media (hover: hover)` — tránh dính hover trên touch.

---

## 5. Iconography & Hình ảnh

- **Icon set: Bootstrap Icons** (webfont, miễn phí, đủ bag/cart/phone/search/chevron...) — nhất quán 1 bộ, KHÔNG trộn Font Awesome như site gốc.
- Ảnh sản phẩm: vuông 1:1, `object-fit: contain`, nền trắng; banner 1400×450; thumbnail tin 16:9. Đang dùng 4 SVG placeholder (Giai đoạn 1) — mọi ratio đã chốt sẵn để thay ảnh thật không vỡ layout.
- Logo demo: wordmark "RenewZone" font Be Vietnam Pro 800, chữ "Zone" màu `--brand-500`, chấm tròn `--red-500` cuối — tự vẽ SVG (không dùng logo thật, Lưu ý 6.2).

## 6. Tổ chức file khi triển khai (Giai đoạn 2)

```
wwwroot/
├── css/
│   ├── site.css        ← tokens (mục 2) + base + layout + components dùng chung
│   ├── home.css        ← hero, section trang chủ
│   ├── product.css     ← card đã nằm site.css; PDP + danh mục
│   ├── cart.css        ← giỏ + checkout
│   └── admin.css       ← theme admin (chỉ load trong _AdminLayout)
├── js/
│   ├── site.js         ← header shrink, reveal-on-scroll, back-to-top, toast, cart badge
│   ├── product.js      ← gallery, variant→giá, fly-to-cart
│   └── cart.js         ← stepper, AJAX cập nhật
└── lib/  (đã có bootstrap, jquery; thêm swiper + glightbox — tải file vào lib, không CDN để chạy offline)
```
- Thứ tự load: bootstrap → site.css → css trang (qua `@section Styles`). JS cuối body, `defer`.
- Swiper thay cho cả Owl/Slick/Nivo của site gốc (1 thư viện cho mọi carousel).

## 7. Accessibility & chất lượng

- Contrast: mọi cặp màu chữ/nền đạt WCAG AA (đã chọn `--brand-500` đủ đậm cho chữ trắng; giá đỏ trên trắng 4.6:1).
- Focus-visible: outline 2px `--brand-400` offset 2px — không bao giờ `outline: none` trần.
- Ảnh có `alt`= tên sản phẩm; nút icon có `aria-label`; carousel có nút prev/next thật (không chỉ swipe).
- `<html lang="vi">`, heading đúng thứ bậc h1→h3 mỗi trang 1 h1.
