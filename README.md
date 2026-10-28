# RenewZone

Website thương mại điện tử bán lẻ sản phẩm Apple (mới & đã qua sử dụng) — website thương mại điện tử bán lẻ sản phẩm Apple mới được thực hiện bởi Nhóm 13 phục vụ mục đích **bán hàng online**. Xây bằng **ASP.NET Core MVC (.NET 9)** theo kiến trúc 3 tầng, gồm **web khách hàng đầy đủ** (có tài khoản, giỏ hàng, đặt hàng, đánh giá) + **trang quản trị 24 màn** cho chủ shop tự vận hành.

> **Dự án học tập** — clone cấu trúc/bố cục/tính năng, KHÔNG dùng ảnh, logo, nội dung hay thông tin doanh nghiệp thật. Toàn bộ dữ liệu mẫu là giả lập (hotline `0900.000.000`, "RenewZone"…).

---

## Mục lục

- [Yêu cầu môi trường](#yêu-cầu-môi-trường)
- [Cài đặt & chạy dự án](#cài-đặt--chạy-dự-án) ← **bắt đầu ở đây**
- [Tài khoản đăng nhập](#tài-khoản-đăng-nhập)
- [Lưu ý quan trọng khi chạy](#-lưu-ý-quan-trọng-khi-chạy)
- [Làm việc với Migration](#làm-việc-với-migration)
- [Xử lý sự cố thường gặp](#xử-lý-sự-cố-thường-gặp)
- [Tính năng chính](#tính-năng-chính)
- [Stack công nghệ](#stack-công-nghệ)
- [Kiến trúc dự án](#kiến-trúc-dự-án)

---

## Yêu cầu môi trường

| Thành phần | Yêu cầu | Kiểm tra |
|---|---|---|
| **.NET SDK** | 9.0 trở lên (đang dùng `9.0.309`) | `dotnet --version` |
| **SQL Server** | SQL Server (bản Express/Developer đều được) hoặc LocalDB | `sqlcmd -S localhost -E -C -Q "SELECT @@VERSION"` |
| **dotnet-ef** | Chỉ cần khi tự chạy migration | `dotnet ef --version` |
| Hệ điều hành | Windows (đang phát triển trên Windows 11 + PowerShell) | |

Cài `dotnet-ef` nếu chưa có:

```powershell
dotnet tool install --global dotnet-ef
```

> Máy phát triển đang dùng `dotnet-ef` 10.0.8 — vẫn chạy tốt với EF Core 9 của dự án, không cần hạ cấp.

---

## Cài đặt & chạy dự án

### Bước 1 — Tải mã nguồn

```powershell
git clone <đường-dẫn-repository>
cd renew_zone
```

### Bước 2 — Kiểm tra chuỗi kết nối

Mở [RenewZone.Web/appsettings.json](RenewZone.Web/appsettings.json):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=RenewZoneClone;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

Sửa `Server=` cho khớp máy chủ SQL Server của bạn:

| Trường hợp | Giá trị `Server=` |
|---|---|
| SQL Server cài mặc định | `localhost` |
| SQL Server Express | `localhost\SQLEXPRESS` |
| LocalDB (đi kèm Visual Studio) | `(localdb)\MSSQLLocalDB` |
| Dùng tài khoản SQL thay vì Windows | bỏ `Trusted_Connection=True`, thêm `User Id=sa;Password=…` |

**Không cần tự tạo database** — ứng dụng sẽ tự tạo ở bước 4.

### Bước 3 — Khôi phục package

```powershell
dotnet restore
```

### Bước 4 — Chạy ứng dụng

```powershell
dotnet run --project src/RenewZone.Web
```

Lần chạy đầu tiên sẽ **mất 20–40 giây** vì `DbInitializer` tự động:

1. Tạo database `RenewZoneClone` và áp toàn bộ 7 migration.
2. Seed dữ liệu mẫu: 18 danh mục · 22 sản phẩm (kèm biến thể) · 8 bài viết · 12 trang tĩnh · banner · đánh giá · cấu hình site.
3. Tạo role `Admin` / `Customer` và tài khoản quản trị.

Toàn bộ bước này **idempotent** — chạy lại app nhiều lần không sinh dữ liệu trùng.

### Bước 5 — Mở trình duyệt

| | Địa chỉ |
|---|---|
| Web khách hàng | http://localhost:5216 |
| Trang quản trị | http://localhost:5216/admin |
| Bản HTTPS | https://localhost:7089 |

Cổng được khai trong [RenewZone.Web/Properties/launchSettings.json](RenewZone.Web/Properties/launchSettings.json) — đổi ở đó nếu bị trùng cổng.

---

## Tài khoản đăng nhập

### Quản trị viên

Truy cập `/admin/login`:

```
Tên đăng nhập:  admin
Mật khẩu:       Admin@123
```

### Khách hàng

Web khách **bắt buộc đăng nhập mới mua được hàng** (khách vãng lai chỉ xem sản phẩm và gửi form liên hệ). Tự đăng ký tại `/dang-ky`:

- **Tên đăng nhập là số điện thoại** (định dạng `0xxxxxxxxx` hoặc `+84xxxxxxxxx`).
- Email là tùy chọn, mật khẩu tối thiểu 6 ký tự.
- Quên mật khẩu ⇒ quản trị viên cấp lại tại `/admin/khach-hang` (dự án chưa nối SMS/email).

> Hai vai dùng **hai cookie độc lập** nên có thể đăng nhập đồng thời admin và khách hàng trong cùng một trình duyệt mà không đá nhau.

---

## ⚠️ Lưu ý quan trọng khi chạy

### 1. Không dùng `--no-launch-profile` khi test ở máy local

`launchSettings.json` là nơi duy nhất khai `ASPNETCORE_ENVIRONMENT=Development`. Bỏ nó đi thì .NET rơi về mặc định **Production**, nạp `appsettings.Production.json` và **kết nối vào database hosting thật** — mọi thao tác test sẽ ghi lên dữ liệu công khai. Rất khó phát hiện vì ứng dụng chạy hoàn toàn bình thường.

Nếu buộc phải chạy không dùng launch profile, hãy khai môi trường tường minh:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5099"
dotnet run --project src/RenewZone.Web --no-launch-profile
```

Kiểm tra nhanh xem app đang nối vào đâu:

```powershell
sqlcmd -S localhost -E -C -Q "SELECT DB_NAME(database_id) FROM sys.dm_exec_sessions WHERE is_user_process=1"
```

Không thấy phiên nào của ứng dụng nghĩa là nó đang nối tới máy chủ khác.

### 2. Bản clone mới sẽ hiện ảnh placeholder

Thư mục `RenewZone.Web/wwwroot/uploads/` bị `.gitignore` chặn (đây là file người dùng tải lên lúc chạy, không nên đưa vào repo). Vì vậy máy mới clone về sẽ thấy **ảnh SVG placeholder** thay cho ảnh sản phẩm thật — trang vẫn chạy đúng, chỉ khác phần hình ảnh. Muốn có ảnh thì tự tải lên qua trang quản trị.

### 3. `appsettings.Production.json` không có trong repo

File này chứa bí mật (chuỗi kết nối hosting, mật khẩu admin khi triển khai) nên bị `.gitignore` chặn. Clone mới sẽ **không có** file này — chỉ cần tạo lại khi nào thật sự đi deploy, chạy local thì không cần.

### 4. Dừng ứng dụng trước khi build lại

IIS/`dotnet run` khóa file `.dll` khi đang chạy ⇒ build đè sẽ báo lỗi "file in use". Nhấn `Ctrl+C` ở cửa sổ đang chạy trước khi `dotnet build`.

---

## Làm việc với Migration

`DbContext` nằm ở tầng `Infrastructure` còn startup project là `Web`, nên **phải chỉ rõ cả hai** bằng `-p` và `-s`:

```powershell
# Thêm migration mới
dotnet ef migrations add <TenMigration> -p src/RenewZone.Infrastructure -s src/RenewZone.Web -o Data/Migrations

# Áp migration vào database
dotnet ef database update -p src/RenewZone.Infrastructure -s src/RenewZone.Web

# Xem danh sách migration đã có
dotnet ef migrations list -p src/RenewZone.Infrastructure -s src/RenewZone.Web

# Xóa sạch database để seed lại từ đầu (MẤT toàn bộ dữ liệu)
dotnet ef database drop -p src/RenewZone.Infrastructure -s src/RenewZone.Web
```

Bảy migration hiện có, theo thứ tự:

`InitialCreate` → `Phase5Content` → `Phase6SoftDelete` → `Phase7OrderHistory` → `Phase8Reviews` → `Phase8ReviewPhone` → `Phase9Accounts`

> Bình thường **không cần chạy tay** lệnh nào ở trên — `DbInitializer` đã tự migrate khi ứng dụng khởi động.

---

## Xử lý sự cố thường gặp

| Triệu chứng | Nguyên nhân & cách xử lý |
|---|---|
| `A network-related or instance-specific error…` khi khởi động | SQL Server chưa chạy hoặc sai tên máy chủ. Bật service `MSSQLSERVER` trong `services.msc`, hoặc đổi `Server=` sang `(localdb)\MSSQLLocalDB`. |
| Lỗi `NU1202` khi restore | NuGet lấy nhầm package 10.x (chỉ hỗ trợ net10.0). Mọi package `Microsoft.*` trong dự án **pin cứng `9.0.13`** — đừng nâng cấp. |
| Build báo `file in use` / không copy được DLL | Ứng dụng đang chạy. Dừng bằng `Ctrl+C` rồi build lại. |
| Sửa file `.cshtml` mà giao diện không đổi | Dừng và chạy lại `dotnet run`. Riêng bản `dotnet publish` thì view đã biên dịch sẵn vào DLL, phải publish lại. |
| Trang quản trị báo tài khoản bị khóa | Sai mật khẩu 5 lần ⇒ khóa 10 phút. Mở khóa ngay bằng lệnh dưới bảng này. |
| Ảnh sản phẩm không hiện, log báo *"not found in the built time manifest"* | Bình thường với clone mới — xem [Lưu ý mục 2](#2-bản-clone-mới-sẽ-hiện-ảnh-placeholder). |
| Trang lỗi 500 không hiện dù đã ép lỗi | Trang 500 tùy chỉnh **chỉ chạy ở Production**; ở Development hiển thị Developer Exception Page (cố ý). |

Mở khóa tài khoản admin bị khóa tạm:

```powershell
sqlcmd -S localhost -d RenewZoneClone -E -C -Q "SET QUOTED_IDENTIFIER ON; UPDATE AspNetUsers SET LockoutEnd=NULL, AccessFailedCount=0 WHERE UserName='admin';"
```

> Bắt buộc có `SET QUOTED_IDENTIFIER ON;` — bảng `AspNetUsers` có filtered index nên lệnh UPDATE thiếu dòng này sẽ bị từ chối.

---

## Tính năng chính

### Web khách hàng

- **Trang chủ**: banner slider, khối danh mục, section sản phẩm theo danh mục, ưu đãi đặc biệt, bán chạy, đánh giá khách hàng, tin công nghệ.
- **Danh mục sản phẩm** 2 cấp (danh mục cha → dòng máy con) với tab lọc và phân trang.
- **Chi tiết sản phẩm**: gallery ảnh, bộ chọn biến thể (dung lượng/màu/tình trạng/SIM) đổi giá tức thì, thông số kỹ thuật, sản phẩm liên quan.
- **Tài khoản khách hàng**: đăng ký/đăng nhập bằng số điện thoại, sổ địa chỉ, danh sách yêu thích, lịch sử đơn hàng, tự hủy đơn, mua lại, đổi mật khẩu.
- **Giỏ hàng & đặt hàng**: giỏ lưu theo tài khoản, tính giá hoàn toàn ở máy chủ, luồng PRG chống đặt trùng.
- **Đánh giá sản phẩm**: chấm sao + bình luận, **chỉ dành cho người đã mua**, có huy hiệu "Đã mua hàng", quản trị viên duyệt và trả lời công khai.
- **Thông báo realtime**: khách nhận thông báo ngay khi đơn đổi trạng thái (SignalR).
- **Tin tức**, **trang tĩnh** (giới thiệu, chính sách), **liên hệ** (form + tải tệp an toàn + chống spam).
- **SEO**: thẻ meta/Open Graph, JSON-LD, `sitemap.xml`, `robots.txt` động, trang lỗi 404/500 tùy chỉnh.
- **URL slug phẳng** ngay tại root, bám sát site gốc: `/iphone`, `/gioi-thieu`, `/iphone-16-pro-max-256-gb`…

### Trang quản trị (`/admin`) — 24 màn

Dashboard · Đơn hàng (+ lịch sử trạng thái) · Khách hàng · Danh mục · Sản phẩm (+ ảnh + biến thể) · Đánh giá sản phẩm · Bài viết · Chuyên mục tin · Trang tĩnh · Banner · Đánh giá khách hàng · Menu · Khối nội dung · Hộp thư liên hệ · Cấu hình website · Thùng rác.

Điểm nổi bật: xóa mềm + thùng rác khôi phục được · trình soạn thảo rich text · kéo-thả sắp xếp · tải ảnh hàng loạt (tự nén + resize) · **thông báo realtime khi có đơn/đánh giá mới** (chuông + âm báo) · bộ đệm tự động làm mới khi lưu.

---

## Stack công nghệ

| Hạng mục | Công nghệ |
|---|---|
| Framework | **ASP.NET Core MVC**, **.NET 9** (`net9.0`) |
| Ngôn ngữ | C# (bật nullable + implicit usings) |
| Database | **SQL Server** + **EF Core 9.0.13** (code-first, migrations) |
| Xác thực | **ASP.NET Core Identity** — 2 cookie scheme độc lập (`Admin` / `Customer`) |
| Realtime | **SignalR** (thư viện client đặt local) |
| Xử lý ảnh | **SixLabors.ImageSharp 3.1.12** (resize + nén khi tải lên) |
| Giao diện | **Bootstrap 5** + CSS tùy chỉnh (design tokens) |
| JavaScript | jQuery + jQuery Validation + JS thuần |
| Carousel | **Swiper** |
| Rich text | **TinyMCE 7.6.1** |
| Kéo-thả | **SortableJS** |
| Icon | Bootstrap Icons |

> Mọi package `Microsoft.*` **pin cứng `9.0.13`** (bản 10.x chỉ hỗ trợ net10.0, cài không pin sẽ lỗi NU1202). Thư viện front-end tải về `wwwroot/lib/`, **không dùng CDN**.

---

## Kiến trúc dự án

Quy tắc phụ thuộc một chiều: **Web → Infrastructure → Domain** (không được ngược lại).

```
renew_zone/
├── CLAUDE.md                      # "Bộ nhớ" dự án — bối cảnh, quyết định kiến trúc, nhật ký
├── RenewZone.sln
├── docs/                          # DESIGN.md (design system) + kế hoạch từng giai đoạn
└── src/
    ├── README.md                  # File này
    ├── RenewZone.Domain/          # Nghiệp vụ THUẦN — KHÔNG phụ thuộc EF/ASP.NET
    │   ├── Common/                # BaseEntity, AuditableEntity
    │   ├── Entities/              # 21 thực thể (Category, Product, Order, ProductReview…)
    │   ├── Enums/                 # OrderStatus, PaymentMethod, ReviewStatus…
    │   ├── Interfaces/            # Abstraction cho các service
    │   └── Models/                # POCO (Cart, CartItem, OrderRequest, HomeContent…)
    ├── RenewZone.Infrastructure/  # EF Core + Identity
    │   ├── Data/                  # AppDbContext, Configurations/, Migrations/, Seed/
    │   ├── Identity/              # ApplicationUser, AppRoles
    │   ├── Services/              # Cài đặt các service nghiệp vụ
    │   └── DependencyInjection.cs # AddInfrastructure() — điểm đăng ký DI của tầng này
    └── RenewZone.Web/             # MVC
        ├── Areas/Admin/           # Trang quản trị (controller + view + layout riêng)
        ├── Controllers/           # Home, Product, Cart, Checkout, Auth, Account, News…
        ├── Hubs/                  # SignalR: AdminNotifyHub, CustomerNotifyHub
        ├── Services/              # Service thuần web (CartService, MediaService)
        ├── Views/
        └── wwwroot/               # css, js, images, uploads, lib
```

### Những điểm kiến trúc đáng chú ý

- **Routing slug phẳng** — route tĩnh (attribute routing) thắng route dự phòng `{slug}` nhờ độ ưu tiên; `SlugResolver` giữ bộ đệm ánh xạ slug → loại, tra lần lượt Category → Product → News → StaticPage. Slug là **duy nhất xuyên 4 bảng**, kiểm tra ngay khi quản trị viên lưu.
- **Bộ đệm tự làm mới** — `AppDbContext.SaveChanges` gom loại thực thể vừa ghi rồi gọi `ICacheInvalidator`; controller quản trị **không cần** nhớ gọi `Invalidate()` ở từng chỗ.
- **Xóa mềm + Thùng rác** — `ISoftDeletable` cùng bộ lọc toàn cục dựng bằng reflection; lệnh `Remove()` tự biến thành UPDATE. Riêng `Order`/`OrderItem`/`SiteSetting` **không** xóa mềm (sổ sách không được biến mất).
- **Mặc định an toàn ở khu quản trị** — `BaseAdminController` gắn sẵn `[Authorize]` + `[AutoValidateAntiforgeryToken]`, mọi controller kế thừa đều được bảo vệ; muốn mở phải cố ý khai `[AllowAnonymous]`.
- **Giá luôn tính ở máy chủ** — client chỉ gửi mã sản phẩm, mã biến thể và số lượng; máy chủ nạp lại dữ liệu để tính đơn giá, tuyệt đối không tin giá do client gửi lên.
- **Khử hardcode nội dung** — chuỗi "nội dung" nằm ở database (`SiteSetting`/`MenuItem`/`ContentBlock`), "nhãn chức năng" hardcode trong view, "cấu hình kỹ thuật" ở `appsettings.json`; kèm luật fallback bắt buộc để thiếu dữ liệu không làm vỡ layout.

---

## Trạng thái dự án

Đã hoàn thành **Giai đoạn 0 → 9**:

| Giai đoạn | Nội dung |
|---|---|
| 0–1 | Khởi tạo solution 3 tầng · database & dữ liệu mẫu |
| 2–3 | Layout & trang chủ · trang sản phẩm (danh mục, chi tiết, tìm kiếm) |
| 4–5 | Giỏ hàng & đặt hàng · tin tức, liên hệ, khử hardcode nội dung |
| 6 | Trang quản trị (nền tảng · catalog · nội dung và hệ thống) |
| 7 | Lịch sử trạng thái đơn · trang lỗi & SEO · tối ưu ảnh |
| 8 | Đánh giá & bình luận sản phẩm |
| 9 | Tài khoản khách hàng (đăng nhập, sổ địa chỉ, yêu thích, tự hủy đơn, realtime) |
