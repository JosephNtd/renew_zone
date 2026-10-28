using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;

namespace RenewZone.Infrastructure.Data.Seed;

/// <summary>
/// Dữ liệu mẫu mô phỏng catalog site gốc (tên/giá tham khảo thị trường) — mô tả TỰ VIẾT,
/// ảnh placeholder, thông tin cửa hàng GIẢ LẬP (xem CLAUDE.md Lưu ý 6.2).
/// Mỗi hàm seed tự kiểm tra bảng trống trước khi chèn → idempotent.
/// </summary>
public static partial class SeedData
{
    private const string ProductImg = "/images/placeholder-product.svg";
    private const string BannerImg = "/images/placeholder-banner.svg";
    private const string NewsImg = "/images/placeholder-news.svg";
    private const string AvatarImg = "/images/placeholder-avatar.svg";

    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedSiteSettingsAsync(db);
        await SeedCatalogAsync(db);
        await SeedNewsCategoriesAsync(db);
        await SeedNewsAsync(db);
        await SeedStaticPagesAsync(db);
        await SeedBannersAsync(db);
        await SeedTestimonialsAsync(db);
        await SeedMenuAsync(db);
        await SeedContentBlocksAsync(db);
        await BackfillPhase5Async(db);
        await BackfillDetailedContentAsync(db);
        await BackfillImagesAsync(db);
        // Đã TẮT BackfillRebrandAsync (SeedData.Rebrand.cs) từ 22/10/2026: cửa hàng giữ tên RenewZone,
        // không tự đổi thương hiệu trên DB đang chạy nữa.
    }

    // Cấu hình site (thông tin GIẢ LẬP cho demo).
    // Idempotent ở mức TỪNG KEY: DB đã có settings cũ vẫn nhận được key mới của giai đoạn sau
    // (nếu check "bảng trống" thì thêm key mới sẽ không bao giờ được seed vào DB đang chạy).
    private static async Task SeedSiteSettingsAsync(AppDbContext db)
    {
        var existingKeys = await db.SiteSettings.Select(s => s.Key).ToListAsync();
        var existing = existingKeys.ToHashSet(StringComparer.Ordinal);

        var defaults = new List<SiteSetting>();
        defaults.AddRange([
            new SiteSetting { Key = SettingKeys.StoreName, Value = "RenewZone" },
            // Chưa có ảnh logo (LogoUrl/LogoDarkUrl để trống) ⇒ web khách hiện logo CHỮ,
            // và đây là phần được tô màu thương hiệu trong tên cửa hàng ở trên.
            new SiteSetting { Key = SettingKeys.LogoHighlight, Value = "Zone" },
            new SiteSetting { Key = SettingKeys.Tagline, Value = "Thu cũ đổi mới - Lên đời dễ dàng" },
            new SiteSetting { Key = SettingKeys.Hotline, Value = "0900.000.000" },
            new SiteSetting { Key = SettingKeys.Address, Value = "123 Đường Số 1, Phường Demo, TP.HCM" },
            new SiteSetting { Key = SettingKeys.OpenHours, Value = "8h30 - 21h30 (T2 - CN)" },
            new SiteSetting { Key = SettingKeys.Email, Value = "lienhe@renewzone.local" },
            new SiteSetting { Key = SettingKeys.TaxCode, Value = "0000000000" },
            new SiteSetting { Key = SettingKeys.FacebookUrl, Value = "https://facebook.com/" },
            new SiteSetting { Key = SettingKeys.MessengerUrl, Value = "https://m.me/" },
            new SiteSetting { Key = SettingKeys.ZaloUrl, Value = "https://zalo.me/" },
            new SiteSetting { Key = SettingKeys.MapUrl, Value = "https://maps.google.com/" },
            // Bản đồ chung chung (trung tâm TP.HCM) — địa chỉ cửa hàng là giả lập nên không nhúng toạ độ thật
            new SiteSetting
            {
                Key = SettingKeys.MapEmbedUrl,
                Value = "https://www.google.com/maps?q=Ho+Chi+Minh+City&output=embed"
            },

            // Chuyển khoản GIẢ LẬP (Lưu ý 6.2 — tuyệt đối không dùng tài khoản thật)
            new SiteSetting { Key = SettingKeys.BankName, Value = "Demo Bank (ngân hàng giả lập)" },
            new SiteSetting { Key = SettingKeys.BankAccountNumber, Value = "0000 0000 0000" },
            new SiteSetting { Key = SettingKeys.BankAccountName, Value = "RENEWZONE" },
            new SiteSetting { Key = SettingKeys.BankTransferNoteTemplate, Value = "Thanh toan don {0}" },

            new SiteSetting { Key = SettingKeys.ShippingNote, Value = "Shop báo khi xác nhận" },
            new SiteSetting
            {
                Key = SettingKeys.ContactSubjects,
                Value = "Tư vấn mua hàng|Thu cũ đổi mới|Bảo hành|Sửa chữa|Khiếu nại|Khác"
            },
            new SiteSetting
            {
                Key = SettingKeys.ContactIntro,
                Value = "Bạn cần tư vấn chọn máy, báo giá thu cũ đổi mới hay hỗ trợ bảo hành? " +
                        "Để lại thông tin bên dưới, shop phản hồi ngay trong giờ làm việc."
            },
            new SiteSetting { Key = SettingKeys.NewsPageTitle, Value = "Tin công nghệ" },
            new SiteSetting
            {
                Key = SettingKeys.NewsPageIntro,
                Value = "Tin tức, đánh giá và mẹo dùng thiết bị Apple — cập nhật mỗi tuần."
            },
            new SiteSetting
            {
                Key = SettingKeys.FooterAbout,
                Value = "Chuyên iPhone, MacBook, iPad, Apple Watch, AirPods chính hãng — mới & đã qua sử dụng."
            },
            new SiteSetting
            {
                Key = SettingKeys.FooterDisclaimer,
                Value = "Website học tập (clone) — không phải cửa hàng thật, không bán hàng."
            }]);

        var missing = defaults.Where(s => !existing.Contains(s.Key)).ToList();
        if (missing.Count == 0) return;

        db.SiteSettings.AddRange(missing);
        await db.SaveChangesAsync();
    }

    // Danh mục 2 cấp + 22 sản phẩm (kèm variants cho sản phẩm chủ lực)
    private static async Task SeedCatalogAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var attributes = new SeedAttributes();

        // Danh mục cha
        var iphone = Cat("iPhone", "iphone", 1, icon: "bi-phone");
        var macbook = Cat("MacBook", "macbook", 2, icon: "bi-laptop");
        var ipad = Cat("iPad", "ipad", 3, icon: "bi-tablet");
        var watch = Cat("Apple Watch", "apple-watch", 4, icon: "bi-smartwatch");
        var airpods = Cat("AirPods", "airpods", 5, icon: "bi-earbuds");
        var thanhLy = Cat("Hàng thanh lý", "hang-thanh-ly", 6, icon: "bi-box-seam");

        // Series con (hiển thị thành tab lọc trên trang danh mục)
        var ipSeries = new Dictionary<int, Category>();
        for (var v = 11; v <= 17; v++)
        {
            ipSeries[v] = Cat($"iPhone {v} Series", $"iphone-{v}-series", v, iphone);
        }
        var mbAir = Cat("MacBook Air", "macbook-air", 1, macbook);
        var mbPro = Cat("MacBook Pro", "macbook-pro", 2, macbook);
        var ipadPro = Cat("iPad Pro", "ipad-pro", 1, ipad);
        var ipadAir = Cat("iPad Air", "ipad-air", 2, ipad);
        var ipadMini = Cat("iPad Mini", "ipad-mini", 3, ipad);

        db.Categories.AddRange(iphone, macbook, ipad, watch, airpods, thanhLy,
            mbAir, mbPro, ipadPro, ipadAir, ipadMini);
        db.Categories.AddRange(ipSeries.Values);

        // Sản phẩm (giá VND; sale = null ⇒ bán đúng giá niêm yết)
        var p16pm = P(ipSeries[16], "iPhone 16 Pro Max 256GB", "iphone-16-pro-max-256gb", 34_990_000, 30_490_000, featured: true, bestSeller: true);
        AddOptions(attributes, p16pm, "Dung lượng", 1, ("256GB", 0, true), ("512GB", 3_500_000, false), ("1TB", 7_000_000, false));
        AddOptions(attributes, p16pm, "Màu sắc", 2, ("Titan Đen", 0, true), ("Titan Trắng", 0, false), ("Titan Tự Nhiên", 0, false), ("Titan Sa Mạc", 0, false));
        AddOptions(attributes, p16pm, "Tình trạng", 3, ("99%", 0, true), ("98%", -1_500_000, false));
        AddOptions(attributes, p16pm, "Loại SIM", 4, ("Chính hãng VN/A", 0, true), ("LL/A (Mỹ)", -800_000, false));

        var p15pm = P(ipSeries[15], "iPhone 15 Pro Max 256GB", "iphone-15-pro-max-256gb", 29_990_000, 20_990_000, bestSeller: true);
        AddOptions(attributes, p15pm, "Màu sắc", 1, ("Titan Đen", 0, true), ("Titan Trắng", 0, false), ("Titan Xanh", 0, false));
        AddOptions(attributes, p15pm, "Tình trạng", 2, ("99%", 0, true), ("98%", -1_200_000, false));

        var p17pm = P(ipSeries[17], "iPhone 17 Pro Max 256GB", "iphone-17-pro-max-256gb", 37_990_000, null, featured: true, contactPrice: true);

        var products = new List<Product>
        {
            p16pm, p15pm, p17pm,
            P(ipSeries[16], "iPhone 16 Pro 128GB", "iphone-16-pro-128gb", 28_990_000, 25_990_000),
            P(ipSeries[15], "iPhone 15 Pro 128GB", "iphone-15-pro-128gb", 25_990_000, 16_490_000),
            P(ipSeries[15], "iPhone 15 128GB", "iphone-15-128gb", 19_990_000, 13_490_000, bestSeller: true),
            P(ipSeries[14], "iPhone 14 Pro Max 128GB", "iphone-14-pro-max-128gb", 26_990_000, 15_990_000),
            P(ipSeries[13], "iPhone 13 128GB", "iphone-13-128gb", 15_990_000, 9_490_000, bestSeller: true),
            P(ipSeries[11], "iPhone 11 64GB", "iphone-11-64gb", 10_990_000, 5_290_000),

            P(mbAir, "MacBook Air M3 15 inch 16GB/256GB", "macbook-air-m3-15-inch-16gb-256gb", 32_990_000, 27_490_000, featured: true),
            P(mbAir, "MacBook Air M1 13 inch 8GB/256GB 2020", "macbook-air-m1-13-inch-8gb-256gb-2020", 18_990_000, 13_990_000, bestSeller: true),
            P(mbPro, "MacBook Pro M4 14 inch 16GB/512GB", "macbook-pro-m4-14-inch-16gb-512gb", 39_990_000, 36_490_000, featured: true),

            P(ipadPro, "iPad Pro M4 11 inch WiFi 256GB", "ipad-pro-m4-11-inch-wifi-256gb", 28_990_000, 24_990_000, featured: true),
            P(ipadAir, "iPad Air M2 11 inch WiFi 128GB", "ipad-air-m2-11-inch-wifi-128gb", 16_990_000, 14_490_000),
            P(ipad, "iPad Gen 10 10.9 inch WiFi 64GB", "ipad-gen-10-wifi-64gb", 12_990_000, 8_990_000, bestSeller: true),
            P(ipadMini, "iPad Mini 7 WiFi 128GB", "ipad-mini-7-wifi-128gb", 14_990_000, 12_490_000),

            P(watch, "Apple Watch Series 10 46mm GPS", "apple-watch-series-10-46mm-gps", 11_990_000, 9_990_000, bestSeller: true),
            P(watch, "Apple Watch Ultra 2 49mm", "apple-watch-ultra-2-49mm", 21_990_000, 18_490_000),

            P(airpods, "AirPods Pro 2 (USB-C)", "airpods-pro-2-usb-c", 6_790_000, 5_190_000, bestSeller: true),
            P(airpods, "AirPods 4", "airpods-4", 3_990_000, 3_290_000),

            P(thanhLy, "iPhone 12 64GB - Thanh lý 97%", "iphone-12-64gb-thanh-ly", 8_990_000, 4_990_000),
            P(thanhLy, "MacBook Air M1 - Thanh lý 96%", "macbook-air-m1-thanh-ly", 13_990_000, 10_490_000),
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }

    private static Category Cat(string name, string slug, int order, Category? parent = null, string? icon = null) => new()
    {
        Name = name,
        Slug = slug,
        DisplayOrder = order,
        Parent = parent,
        IconClass = icon, // chỉ danh mục cha cần icon (hiện ở menu) — con để null
        ImageUrl = parent is null ? ProductImg : null // danh mục cha có ảnh cho khối danh mục trang chủ
    };

    private static Product P(Category category, string name, string slug, decimal original, decimal? sale,
        bool featured = false, bool bestSeller = false, bool contactPrice = false)
    {
        var p = new Product
        {
            Category = category,
            Name = name,
            Slug = slug,
            OriginalPrice = original,
            SalePrice = contactPrice ? null : sale,
            IsContactPrice = contactPrice,
            IsFeatured = featured,
            IsBestSeller = bestSeller,
            ShortDescription = $"{name} - hàng chính hãng, bảo hành dài hạn, hỗ trợ trả góp 0%, thu cũ đổi mới trợ giá.",
            DescriptionHtml =
                $"<p><strong>{name}</strong> tại RenewZone là sản phẩm chính hãng, nguồn gốc rõ ràng, " +
                "được kiểm tra kỹ trước khi giao. Máy đã qua sử dụng được phân loại tình trạng 98%/99% minh bạch.</p>" +
                "<p>Khách hàng được hỗ trợ trả góp 0% qua thẻ tín dụng, thu cũ đổi mới trợ giá và bảo hành " +
                "dài hạn tại cửa hàng. Nội dung mô tả chi tiết sẽ được biên tập trong trang quản trị.</p>",
            SpecsHtml =
                "<ul>" +
                "<li><strong>Màn hình:</strong> đang cập nhật</li>" +
                "<li><strong>Chip:</strong> đang cập nhật</li>" +
                "<li><strong>Camera:</strong> đang cập nhật</li>" +
                "<li><strong>Pin &amp; sạc:</strong> đang cập nhật</li>" +
                "</ul>"
        };

        p.Images.Add(new ProductImage { Url = ProductImg, IsMain = true, DisplayOrder = 0 });
        p.Images.Add(new ProductImage { Url = ProductImg, DisplayOrder = 1 });
        return p;
    }

    private static void AddOptions(SeedAttributes attributes, Product p, string groupName, int order, params (string Value, decimal Adjust, bool IsDefault)[] values)
    {
        var group = new ProductOptionGroup { Attribute = attributes.Attribute(groupName), DisplayOrder = order };
        for (var i = 0; i < values.Length; i++)
        {
            group.Values.Add(new ProductOptionValue
            {
                AttributeValue = attributes.Value(groupName, values[i].Value),
                PriceAdjustment = values[i].Adjust,
                IsDefault = values[i].IsDefault,
                DisplayOrder = i
            });
        }
        p.OptionGroups.Add(group);
    }

    /// <summary>
    /// Danh sách thuộc tính dùng chung khi seed (Giai đoạn 10): cùng tên nhóm ⇒ cùng một bản ghi
    /// ProductAttribute, cùng giá trị ⇒ cùng một ProductAttributeValue. EF thêm chúng cùng lúc với sản phẩm.
    /// </summary>
    private sealed class SeedAttributes
    {
        private readonly Dictionary<string, ProductAttribute> _attributes = new(StringComparer.OrdinalIgnoreCase);

        public ProductAttribute Attribute(string name)
        {
            if (!_attributes.TryGetValue(name, out var attribute))
            {
                attribute = new ProductAttribute { Name = name, DisplayOrder = _attributes.Count };
                _attributes[name] = attribute;
            }

            return attribute;
        }

        public ProductAttributeValue Value(string attributeName, string value)
        {
            var attribute = Attribute(attributeName);
            var existing = attribute.Values.FirstOrDefault(v => string.Equals(v.Value, value, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                return existing;
            }

            var created = new ProductAttributeValue { Value = value, DisplayOrder = attribute.Values.Count };
            attribute.Values.Add(created);
            return created;
        }
    }

    // Chuyên mục tin tức (slug dùng ở /tin-tuc?chuyen-muc=)
    private static async Task SeedNewsCategoriesAsync(AppDbContext db)
    {
        if (await db.NewsCategories.AnyAsync()) return;

        var categories = new (string Name, string Slug)[]
        {
            ("Thiết bị", "thiet-bi"),
            ("Khoa học công nghệ", "khoa-hoc-cong-nghe"),
            ("Thủ thuật", "thu-thuat"),
            ("Tư vấn", "tu-van"),
        };

        var order = 1;
        foreach (var c in categories)
        {
            db.NewsCategories.Add(new NewsCategory { Name = c.Name, Slug = c.Slug, DisplayOrder = order++ });
        }

        await db.SaveChangesAsync();
    }

    // Tin công nghệ (nội dung mẫu TỰ VIẾT)
    private static async Task SeedNewsAsync(AppDbContext db)
    {
        if (await db.NewsArticles.AnyAsync()) return;

        // Chuyên mục đã seed ở bước trước — map slug → Id để gán CategoryId
        var catBySlug = await db.NewsCategories.ToDictionaryAsync(c => c.Slug, c => c.Id);

        var articles = new (string Title, string Slug, string CategorySlug, bool Featured, int DaysAgo)[]
        {
            ("iPhone 17 Pro Max lộ diện: thiết kế mới, camera nâng cấp toàn diện", "iphone-17-pro-max-lo-dien-thiet-ke-moi", "thiet-bi", true, 1),
            ("Apple phát hành bản cập nhật iOS mới: vá lỗi bảo mật quan trọng", "apple-phat-hanh-ban-cap-nhat-ios-moi", "khoa-hoc-cong-nghe", true, 3),
            ("MacBook Air M4 có gì mới? So sánh nhanh với thế hệ M3", "macbook-air-m4-co-gi-moi-so-sanh-m3", "thiet-bi", true, 5),
            ("5 mẹo tiết kiệm pin iPhone ai cũng nên biết", "5-meo-tiet-kiem-pin-iphone", "thu-thuat", false, 8),
            ("Nên mua iPhone 15 Pro Max cũ 99% hay iPhone 16 mới?", "nen-mua-iphone-15-pro-max-cu-hay-iphone-16-moi", "tu-van", false, 12),
            ("Apple Watch hỗ trợ theo dõi sức khỏe tim mạch như thế nào?", "apple-watch-theo-doi-suc-khoe-tim-mach", "khoa-hoc-cong-nghe", false, 15),
            ("Hướng dẫn mua điện thoại trả góp 0% đơn giản tại TP.HCM", "huong-dan-mua-dien-thoai-tra-gop-0-phan-tram", "tu-van", false, 20),
            ("AirPods thế hệ mới có thể đo nhịp tim ngay trên tai nghe?", "airpods-the-he-moi-do-nhip-tim", "thiet-bi", false, 25),
        };

        var now = DateTime.Now;
        foreach (var a in articles)
        {
            db.NewsArticles.Add(new NewsArticle
            {
                Title = a.Title,
                Slug = a.Slug,
                CategoryId = catBySlug.TryGetValue(a.CategorySlug, out var catId) ? catId : null,
                IsFeatured = a.Featured,
                ThumbnailUrl = NewsImg,
                PublishedAt = now.AddDays(-a.DaysAgo),
                Excerpt = $"{a.Title}. Bài viết demo phục vụ học tập — nội dung chi tiết sẽ được biên tập trong trang quản trị.",
                ContentHtml =
                    $"<p><strong>{a.Title}</strong></p>" +
                    "<p>Đây là nội dung mẫu tự viết cho dự án học tập RenewZone. " +
                    "Bài viết thực tế sẽ được admin soạn bằng trình soạn thảo rich text ở Giai đoạn 6.</p>" +
                    "<p>Đoạn thứ hai minh họa bố cục nhiều đoạn văn của một bài tin công nghệ thông thường, " +
                    "giúp kiểm tra hiển thị trang chi tiết bài viết khi phát triển giao diện.</p>"
            });
        }

        await db.SaveChangesAsync();
    }

    // Trang tĩnh: giới thiệu + chính sách (nội dung mẫu TỰ VIẾT)
    private static async Task SeedStaticPagesAsync(AppDbContext db)
    {
        if (await db.StaticPages.AnyAsync()) return;

        var pages = new (string Title, string Slug)[]
        {
            ("Giới thiệu", "gioi-thieu"),
            ("Chính sách bảo hành", "chinh-sach-bao-hanh"),
            ("Chính sách thanh toán", "chinh-sach-thanh-toan"),
            ("Chính sách kiểm hàng", "chinh-sach-kiem-hang"),
            ("Chính sách mua hàng", "chinh-sach-mua-hang"),
            ("Chính sách vận chuyển", "chinh-sach-van-chuyen"),
            ("Chính sách bảo mật", "chinh-sach-bao-mat"),
            ("Chính sách trả góp", "chinh-sach-tra-gop"),
            ("Bảng giá thu cũ đổi mới", "bang-gia-thu-cu-doi-moi"),
            ("Máy mới", "may-moi"),
            ("Máy đã qua sử dụng", "may-da-qua-su-dung"),
            ("Bảo hành 20 năm", "bao-hanh-20-nam"),
        };

        foreach (var page in pages)
        {
            db.StaticPages.Add(new StaticPage
            {
                Title = page.Title,
                Slug = page.Slug,
                ContentHtml =
                    $"<h2>{page.Title}</h2>" +
                    "<p>Nội dung mẫu tự viết cho dự án học tập RenewZone. " +
                    "Nội dung chính thức của trang này sẽ được biên tập trong trang quản trị (Giai đoạn 6).</p>" +
                    "<p>Trang được render động theo slug từ bảng StaticPages — thêm trang mới không cần sửa code.</p>"
            });
        }

        // 2 trang sửa chữa có nội dung dài hơn (menu trỏ thẳng tới) — nội dung TỰ VIẾT, bảng giá tham khảo giả lập
        db.StaticPages.Add(new StaticPage
        {
            Title = "Dịch vụ sửa chữa",
            Slug = "sua-chua",
            ContentHtml = RepairServiceHtml
        });
        db.StaticPages.Add(new StaticPage
        {
            Title = "Sửa chữa phần cứng",
            Slug = "sua-chua-phan-cung",
            ContentHtml = RepairHardwareHtml
        });

        await db.SaveChangesAsync();
    }

    private const string RepairServiceHtml = """
        <h2>Dịch vụ sửa chữa thiết bị Apple</h2>
        <p>Trung tâm kỹ thuật RenewZone nhận kiểm tra và sửa chữa iPhone, iPad, MacBook,
        Apple Watch và AirPods. Máy được kiểm tra miễn phí, báo giá trước khi sửa, khách đồng ý mới tiến hành.</p>

        <h3>Nhóm dịch vụ chúng tôi nhận</h3>
        <ul>
          <li><strong>Màn hình &amp; cảm ứng:</strong> thay màn hình, xử lý sọc màn, loạn cảm ứng, ám màu.</li>
          <li><strong>Nguồn &amp; pin:</strong> thay pin chai, sửa lỗi không lên nguồn, sập nguồn ngẫu nhiên.</li>
          <li><strong>Sạc &amp; kết nối:</strong> thay chân sạc, sửa lỗi sạc chập chờn, mất Wi-Fi/Bluetooth.</li>
          <li><strong>Camera &amp; loa mic:</strong> thay cụm camera mờ/rung, xử lý loa rè, mic không thu tiếng.</li>
          <li><strong>Phần mềm:</strong> cài đặt lại hệ điều hành, cứu dữ liệu, xử lý máy treo táo.</li>
          <li><strong>Vệ sinh &amp; bảo dưỡng:</strong> vệ sinh bo mạch, tra keo tản nhiệt MacBook, chống ẩm.</li>
        </ul>

        <h3>Quy trình 4 bước</h3>
        <ol>
          <li><strong>Tiếp nhận:</strong> ghi nhận tình trạng máy, dán phiếu và niêm phong trước mặt khách.</li>
          <li><strong>Kiểm tra:</strong> kỹ thuật viên đo đạc, xác định lỗi và báo giá cụ thể — miễn phí kiểm tra.</li>
          <li><strong>Sửa chữa:</strong> tiến hành sau khi khách xác nhận báo giá; linh kiện thay ra được trả lại khách.</li>
          <li><strong>Bàn giao:</strong> test lại cùng khách, xuất phiếu bảo hành theo từng hạng mục.</li>
        </ol>

        <h3>Bảng giá tham khảo</h3>
        <p>Giá dưới đây là <em>mức tham khảo cho dự án demo</em>, chi phí thực tế phụ thuộc model và tình trạng máy.</p>
        <table>
          <thead>
            <tr><th>Hạng mục</th><th>Thời gian</th><th>Chi phí tham khảo</th><th>Bảo hành</th></tr>
          </thead>
          <tbody>
            <tr><td>Thay pin iPhone</td><td>30 – 45 phút</td><td>450.000 – 1.200.000đ</td><td>6 tháng</td></tr>
            <tr><td>Thay màn hình iPhone</td><td>45 – 90 phút</td><td>1.200.000 – 6.500.000đ</td><td>6 tháng</td></tr>
            <tr><td>Thay chân sạc</td><td>60 phút</td><td>350.000 – 900.000đ</td><td>3 tháng</td></tr>
            <tr><td>Thay pin MacBook</td><td>2 – 4 giờ</td><td>1.500.000 – 3.500.000đ</td><td>6 tháng</td></tr>
            <tr><td>Vệ sinh &amp; tra keo tản nhiệt MacBook</td><td>2 giờ</td><td>350.000 – 600.000đ</td><td>1 tháng</td></tr>
            <tr><td>Cài đặt lại hệ điều hành</td><td>1 – 2 giờ</td><td>150.000 – 300.000đ</td><td>—</td></tr>
          </tbody>
        </table>

        <h3>Cam kết của chúng tôi</h3>
        <ul>
          <li>Kiểm tra và báo giá miễn phí, không sửa vẫn không mất phí.</li>
          <li>Không thay linh kiện khi chưa có xác nhận của khách hàng.</li>
          <li>Bảo hành theo hạng mục, ghi rõ trên phiếu bàn giao.</li>
          <li>Hoàn tiền nếu không khắc phục được đúng lỗi đã nhận.</li>
        </ul>
        """;

    // Chú ý: TradeInPriceTableHtml được định nghĩa đầy đủ trong SeedData.Content.cs

    private const string RepairHardwareHtml = """
        <h2>Sửa chữa phần cứng chuyên sâu</h2>
        <p>Với các lỗi phần cứng nặng — vào nước, rơi vỡ, chập nguồn, mất tín hiệu — máy được đưa sang
        khu vực kỹ thuật chuyên sâu để đo đạc trên sơ đồ mạch trước khi can thiệp.</p>

        <h3>Các lỗi phần cứng thường gặp</h3>
        <ul>
          <li><strong>Vào nước:</strong> tháo máy, vệ sinh siêu âm, xử lý oxy hóa chân linh kiện.</li>
          <li><strong>Lỗi nguồn:</strong> đo dòng tiêu thụ, dò chập, thay IC nguồn.</li>
          <li><strong>Lỗi sạc:</strong> thay IC sạc, xử lý đường tín hiệu bị đứt sau va đập.</li>
          <li><strong>Mất sóng / Wi-Fi:</strong> hàn lại hoặc thay IC thu phát tín hiệu.</li>
          <li><strong>Lỗi màn hình MacBook:</strong> thay cáp màn, xử lý lỗi backlight.</li>
          <li><strong>Lỗi bàn phím &amp; trackpad:</strong> thay cụm phím, xử lý phím liệt do vào nước.</li>
        </ul>

        <h3>Thiết bị kỹ thuật sử dụng</h3>
        <ul>
          <li>Kính hiển vi soi mạch và máy khò nhiệt có kiểm soát nhiệt độ.</li>
          <li>Máy đo dòng, nguồn phòng thí nghiệm để dò chập trước khi cấp điện.</li>
          <li>Bể rửa siêu âm dùng cho máy vào nước.</li>
        </ul>

        <h3>Bảng giá tham khảo</h3>
        <table>
          <thead>
            <tr><th>Hạng mục</th><th>Mức độ</th><th>Chi phí tham khảo</th><th>Bảo hành</th></tr>
          </thead>
          <tbody>
            <tr><td>Vệ sinh máy vào nước</td><td>Nhẹ</td><td>300.000 – 700.000đ</td><td>1 tháng</td></tr>
            <tr><td>Sửa lỗi nguồn (thay IC nguồn)</td><td>Nặng</td><td>800.000 – 2.500.000đ</td><td>3 tháng</td></tr>
            <tr><td>Sửa lỗi sạc (thay IC sạc)</td><td>Trung bình</td><td>600.000 – 1.500.000đ</td><td>3 tháng</td></tr>
            <tr><td>Khắc phục mất sóng / Wi-Fi</td><td>Nặng</td><td>900.000 – 2.200.000đ</td><td>3 tháng</td></tr>
            <tr><td>Thay cáp màn hình MacBook</td><td>Trung bình</td><td>1.200.000 – 2.800.000đ</td><td>6 tháng</td></tr>
            <tr><td>Thay bàn phím MacBook</td><td>Nặng</td><td>1.500.000 – 3.900.000đ</td><td>6 tháng</td></tr>
          </tbody>
        </table>

        <h3>Lưu ý trước khi gửi máy</h3>
        <ol>
          <li>Sao lưu dữ liệu nếu máy còn mở được — sửa phần cứng luôn có rủi ro mất dữ liệu.</li>
          <li>Tắt tính năng tìm thiết bị và cung cấp mật khẩu mở máy để kỹ thuật test được đầy đủ.</li>
          <li>Máy vào nước nên mang tới càng sớm càng tốt, không tự sấy hoặc cắm sạc.</li>
          <li>Máy đã qua sửa chữa ở nơi khác vẫn nhận, nhưng thời gian xử lý có thể lâu hơn.</li>
        </ol>

        <p><em>Toàn bộ nội dung trang này là dữ liệu mẫu của dự án học tập, không phải cam kết dịch vụ thật.</em></p>
        """;

    // Banner trang chủ
    private static async Task SeedBannersAsync(AppDbContext db)
    {
        if (await db.Banners.AnyAsync()) return;

        db.Banners.AddRange(
            new Banner { Title = "iPhone 17 Pro Max - Đặt trước hôm nay", ImageUrl = BannerImg, LinkUrl = "/iphone-17-pro-max-256gb", Position = BannerPosition.HomeSlider, DisplayOrder = 1 },
            new Banner { Title = "Thu cũ đổi mới - Trợ giá đến 2 triệu", ImageUrl = BannerImg, LinkUrl = "/bang-gia-thu-cu-doi-moi", Position = BannerPosition.HomeSlider, DisplayOrder = 2 },
            new Banner { Title = "MacBook sale đến 30%", ImageUrl = BannerImg, LinkUrl = "/macbook", Position = BannerPosition.HomeSlider, DisplayOrder = 3 },
            new Banner { Title = "Ưu đãi iPad", ImageUrl = BannerImg, LinkUrl = "/ipad", Position = BannerPosition.HomeMiddle, DisplayOrder = 1 },
            new Banner { Title = "Hàng thanh lý giá tốt", ImageUrl = BannerImg, LinkUrl = "/hang-thanh-ly", Position = BannerPosition.HomeMiddle, DisplayOrder = 2 });

        await db.SaveChangesAsync();
    }

    // Đánh giá khách hàng (tên giả lập)
    private static async Task SeedTestimonialsAsync(AppDbContext db)
    {
        if (await db.Testimonials.AnyAsync()) return;

        var items = new (string Name, string Content)[]
        {
            ("Anh Minh - Quận 1", "Mua iPhone 15 Pro Max 99% mà máy đẹp như mới, shop tư vấn nhiệt tình, giá tốt hơn nhiều nơi mình khảo sát."),
            ("Chị Hương - Gò Vấp", "Lên đời từ iPhone 12 sang 16 Pro Max, được trợ giá thu cũ khá cao. Thủ tục trả góp nhanh gọn."),
            ("Anh Tuấn - Thủ Đức", "MacBook Air M1 thanh lý mà pin còn tốt, ngoại hình 96% đúng mô tả. Sẽ ủng hộ tiếp."),
            ("Chị Lan - Bình Thạnh", "Nhân viên kiểm tra máy kỹ trước mặt mình, bảo hành rõ ràng nên rất yên tâm."),
            ("Anh Khoa - Tân Bình", "Đặt hàng online, giao trong ngày, máy nguyên seal đúng cam kết. Rất hài lòng."),
        };

        var order = 1;
        foreach (var t in items)
        {
            db.Testimonials.Add(new Testimonial
            {
                CustomerName = t.Name,
                Content = t.Content,
                AvatarUrl = AvatarImg,
                DisplayOrder = order++
            });
        }

        await db.SaveChangesAsync();
    }

    // Điều hướng: menu chính + 2 cột link ở footer (thay hardcode trong _Layout)
    private static async Task SeedMenuAsync(AppDbContext db)
    {
        if (await db.MenuItems.AnyAsync()) return;

        // Menu chính.
        // Mục "Sản phẩm" là ProductMega: dropdown sinh động từ cây Category nên không khai báo link con.
        db.MenuItems.AddRange(
            Menu(MenuGroup.MainMenu, "Trang chủ", "/", 1, icon: "bi-house-door"),
            Menu(MenuGroup.MainMenu, "Giới thiệu", "/gioi-thieu", 2),
            new MenuItem
            {
                Group = MenuGroup.MainMenu,
                Type = MenuItemType.ProductMega,
                Text = "Sản phẩm",
                Url = "/san-pham",
                Icon = "bi-grid",
                DisplayOrder = 3
            });

        var repair = Menu(MenuGroup.MainMenu, "Sửa chữa", "/sua-chua", 4, icon: "bi-tools");
        repair.Children.Add(Menu(MenuGroup.MainMenu, "Dịch vụ sửa chữa", "/sua-chua", 1, icon: "bi-tools"));
        repair.Children.Add(Menu(MenuGroup.MainMenu, "Sửa chữa phần cứng", "/sua-chua-phan-cung", 2, icon: "bi-cpu"));

        var policy = Menu(MenuGroup.MainMenu, "Chính sách", "/chinh-sach-bao-hanh", 5, icon: "bi-shield-check");
        policy.Children.Add(Menu(MenuGroup.MainMenu, "Bảng giá thu cũ đổi mới", "/bang-gia-thu-cu-doi-moi", 1, icon: "bi-shield-check"));
        policy.Children.Add(Menu(MenuGroup.MainMenu, "Máy mới", "/may-moi", 2, icon: "bi-shield-check"));
        policy.Children.Add(Menu(MenuGroup.MainMenu, "Máy đã qua sử dụng", "/may-da-qua-su-dung", 3, icon: "bi-shield-check"));
        policy.Children.Add(Menu(MenuGroup.MainMenu, "Bảo hành 20 năm", "/bao-hanh-20-nam", 4, icon: "bi-shield-check"));

        db.MenuItems.AddRange(
            repair,
            policy,
            Menu(MenuGroup.MainMenu, "Tin công nghệ", "/tin-tuc", 6, icon: "bi-newspaper"),
            Menu(MenuGroup.MainMenu, "Liên hệ", "/lien-he", 7, icon: "bi-envelope"));

        // Footer: cột Chính sách
        db.MenuItems.AddRange(
            Menu(MenuGroup.FooterPolicy, "Chính sách bảo hành", "/chinh-sach-bao-hanh", 1),
            Menu(MenuGroup.FooterPolicy, "Chính sách thanh toán", "/chinh-sach-thanh-toan", 2),
            Menu(MenuGroup.FooterPolicy, "Chính sách vận chuyển", "/chinh-sach-van-chuyen", 3),
            Menu(MenuGroup.FooterPolicy, "Chính sách kiểm hàng", "/chinh-sach-kiem-hang", 4),
            Menu(MenuGroup.FooterPolicy, "Chính sách trả góp", "/chinh-sach-tra-gop", 5),
            Menu(MenuGroup.FooterPolicy, "Chính sách bảo mật", "/chinh-sach-bao-mat", 6));

        // Footer: cột Hỗ trợ
        db.MenuItems.AddRange(
            Menu(MenuGroup.FooterSupport, "Giới thiệu", "/gioi-thieu", 1),
            Menu(MenuGroup.FooterSupport, "Tin công nghệ", "/tin-tuc", 2),
            Menu(MenuGroup.FooterSupport, "Sửa chữa", "/sua-chua", 3),
            Menu(MenuGroup.FooterSupport, "Giỏ hàng", "/gio-hang", 4),
            Menu(MenuGroup.FooterSupport, "Liên hệ", "/lien-he", 5));

        await db.SaveChangesAsync();
    }

    private static MenuItem Menu(MenuGroup group, string text, string url, int order, string? icon = null) => new()
    {
        Group = group,
        Type = MenuItemType.Link,
        Text = text,
        Url = url,
        Icon = icon,
        DisplayOrder = order
    };

    // Khối nội dung dùng lại: cam kết PDP, trấn an giỏ/checkout, kênh liên hệ
    private static async Task SeedContentBlocksAsync(AppDbContext db)
    {
        if (await db.ContentBlocks.AnyAsync()) return;

        var hotline = await db.SiteSettings
            .Where(s => s.Key == SettingKeys.Hotline)
            .Select(s => s.Value)
            .FirstOrDefaultAsync() ?? "0900.000.000";

        var mapUrl = await db.SiteSettings
            .Where(s => s.Key == SettingKeys.MapUrl)
            .Select(s => s.Value)
            .FirstOrDefaultAsync() ?? "https://maps.google.com/";

        db.ContentBlocks.AddRange(
            // Cam kết ở trang chi tiết sản phẩm
            Block(ContentBlockGroups.ProductCommitments, "bi-patch-check-fill", "Hàng chính hãng, nguyên seal / đúng mô tả", 1),
            Block(ContentBlockGroups.ProductCommitments, "bi-arrow-repeat", "Thu cũ đổi mới, thu giá cao", 2),
            Block(ContentBlockGroups.ProductCommitments, "bi-shield-check", "Bảo hành dài hạn, 1 đổi 1", 3),
            Block(ContentBlockGroups.ProductCommitments, "bi-truck", "Giao nhanh nội thành, kiểm tra khi nhận", 4),

            // Dòng trấn an ở giỏ hàng + checkout
            Block(ContentBlockGroups.CartAssurance, "bi-shield-check", "Thu cũ đổi mới · Bảo hành dài hạn", 1),

            // Kênh liên hệ nhanh ở /lien-he
            Block(ContentBlockGroups.ContactChannels, "bi-telephone-fill", "Hotline", 1,
                subtitle: hotline, linkUrl: "tel:" + hotline.Replace(".", "").Replace(" ", "")),
            Block(ContentBlockGroups.ContactChannels, "bi-chat-dots-fill", "Zalo", 2,
                subtitle: "Chat ngay", linkUrl: "https://zalo.me/"),
            Block(ContentBlockGroups.ContactChannels, "bi-messenger", "Messenger", 3,
                subtitle: "Chat ngay", linkUrl: "https://m.me/"),
            Block(ContentBlockGroups.ContactChannels, "bi-geo-alt-fill", "Cửa hàng", 4,
                subtitle: "Chỉ đường", linkUrl: mapUrl));

        await db.SaveChangesAsync();
    }

    private static ContentBlock Block(string groupKey, string icon, string title, int order,
        string? subtitle = null, string? linkUrl = null) => new()
    {
        GroupKey = groupKey,
        Icon = icon,
        Title = title,
        Subtitle = subtitle,
        LinkUrl = linkUrl,
        DisplayOrder = order
    };

    // Bù dữ liệu cho các CỘT/BẢNG mới của Giai đoạn 5 trên DB đã seed từ trước.
    // Các hàm seed ở trên đều "bảng trống mới chạy" nên DB cũ sẽ không nhận được
    // Category.IconClass hay NewsArticle.CategoryId — bù riêng ở đây, vẫn idempotent
    // (chỉ chạm dòng đang thiếu, giữ nguyên đơn hàng test đã có).
    private static async Task BackfillPhase5Async(AppDbContext db)
    {
        var changed = false;

        // Icon danh mục cha
        var icons = new Dictionary<string, string>
        {
            ["iphone"] = "bi-phone",
            ["macbook"] = "bi-laptop",
            ["ipad"] = "bi-tablet",
            ["apple-watch"] = "bi-smartwatch",
            ["airpods"] = "bi-earbuds",
            ["hang-thanh-ly"] = "bi-box-seam",
        };

        var catsNeedingIcon = await db.Categories
            .Where(c => c.ParentId == null && c.IconClass == null)
            .ToListAsync();

        foreach (var cat in catsNeedingIcon)
        {
            if (icons.TryGetValue(cat.Slug, out var icon))
            {
                cat.IconClass = icon;
                changed = true;
            }
        }

        // Chuyên mục cho bài viết đã seed trước khi có bảng NewsCategories
        var articlesWithoutCategory = await db.NewsArticles.Where(a => a.CategoryId == null).ToListAsync();
        if (articlesWithoutCategory.Count > 0)
        {
            var catBySlug = await db.NewsCategories.ToDictionaryAsync(c => c.Slug, c => c.Id);

            var articleCategory = new Dictionary<string, string>
            {
                ["iphone-17-pro-max-lo-dien-thiet-ke-moi"] = "thiet-bi",
                ["apple-phat-hanh-ban-cap-nhat-ios-moi"] = "khoa-hoc-cong-nghe",
                ["macbook-air-m4-co-gi-moi-so-sanh-m3"] = "thiet-bi",
                ["5-meo-tiet-kiem-pin-iphone"] = "thu-thuat",
                ["nen-mua-iphone-15-pro-max-cu-hay-iphone-16-moi"] = "tu-van",
                ["apple-watch-theo-doi-suc-khoe-tim-mach"] = "khoa-hoc-cong-nghe",
                ["huong-dan-mua-dien-thoai-tra-gop-0-phan-tram"] = "tu-van",
                ["airpods-the-he-moi-do-nhip-tim"] = "thiet-bi",
            };

            foreach (var article in articlesWithoutCategory)
            {
                if (articleCategory.TryGetValue(article.Slug, out var catSlug)
                    && catBySlug.TryGetValue(catSlug, out var catId))
                {
                    article.CategoryId = catId;
                    changed = true;
                }
            }
        }

        // 2 trang sửa chữa (DB cũ đã có StaticPages nên hàm seed trang tĩnh không chạy)
        var repairPages = new (string Slug, string Title, string Html)[]
        {
            ("sua-chua", "Dịch vụ sửa chữa", RepairServiceHtml),
            ("sua-chua-phan-cung", "Sửa chữa phần cứng", RepairHardwareHtml),
        };

        foreach (var (slug, title, html) in repairPages)
        {
            if (!await db.StaticPages.AnyAsync(p => p.Slug == slug))
            {
                db.StaticPages.Add(new StaticPage { Title = title, Slug = slug, ContentHtml = html });
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }
}
