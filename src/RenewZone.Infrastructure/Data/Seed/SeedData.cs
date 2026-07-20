using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;

namespace RenewZone.Infrastructure.Data.Seed;

/// <summary>
/// Dữ liệu mẫu mô phỏng catalog site gốc (tên/giá tham khảo thị trường) — mô tả TỰ VIẾT,
/// ảnh placeholder, thông tin cửa hàng GIẢ LẬP (xem CLAUDE.md Lưu ý 6.2).
/// Mỗi hàm seed tự kiểm tra bảng trống trước khi chèn → idempotent.
/// </summary>
public static class SeedData
{
    private const string ProductImg = "/images/placeholder-product.svg";
    private const string BannerImg = "/images/placeholder-banner.svg";
    private const string NewsImg = "/images/placeholder-news.svg";
    private const string AvatarImg = "/images/placeholder-avatar.svg";

    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedSiteSettingsAsync(db);
        await SeedCatalogAsync(db);
        await SeedNewsAsync(db);
        await SeedStaticPagesAsync(db);
        await SeedBannersAsync(db);
        await SeedTestimonialsAsync(db);
    }

    // =============================================================
    // Cấu hình site (thông tin GIẢ LẬP cho demo)
    // =============================================================
    private static async Task SeedSiteSettingsAsync(AppDbContext db)
    {
        if (await db.SiteSettings.AnyAsync()) return;

        db.SiteSettings.AddRange(
            new SiteSetting { Key = SettingKeys.StoreName, Value = "RenewZone Demo" },
            new SiteSetting { Key = SettingKeys.Tagline, Value = "Thu cũ đổi mới - Lên đời dễ dàng" },
            new SiteSetting { Key = SettingKeys.Hotline, Value = "0900.000.000" },
            new SiteSetting { Key = SettingKeys.Address, Value = "123 Đường Số 1, Phường Demo, TP.HCM" },
            new SiteSetting { Key = SettingKeys.OpenHours, Value = "8h30 - 21h30 (T2 - CN)" },
            new SiteSetting { Key = SettingKeys.Email, Value = "lienhe@renewzone-demo.local" },
            new SiteSetting { Key = SettingKeys.TaxCode, Value = "0000000000" },
            new SiteSetting { Key = SettingKeys.FacebookUrl, Value = "https://facebook.com/" },
            new SiteSetting { Key = SettingKeys.MessengerUrl, Value = "https://m.me/" },
            new SiteSetting { Key = SettingKeys.ZaloUrl, Value = "https://zalo.me/" },
            new SiteSetting { Key = SettingKeys.MapUrl, Value = "https://maps.google.com/" },
            new SiteSetting { Key = SettingKeys.MapEmbedUrl, Value = "" });

        await db.SaveChangesAsync();
    }

    // =============================================================
    // Danh mục 2 cấp + 22 sản phẩm (kèm variants cho sản phẩm chủ lực)
    // =============================================================
    private static async Task SeedCatalogAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        // ----- Danh mục cha -----
        var iphone = Cat("iPhone", "iphone", 1);
        var macbook = Cat("MacBook", "macbook", 2);
        var ipad = Cat("iPad", "ipad", 3);
        var watch = Cat("Apple Watch", "apple-watch", 4);
        var airpods = Cat("AirPods", "airpods", 5);
        var thanhLy = Cat("Hàng thanh lý", "hang-thanh-ly", 6);

        // ----- Series con (hiển thị thành tab lọc trên trang danh mục) -----
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

        // ----- Sản phẩm ----- (giá VND; sale = null ⇒ bán đúng giá niêm yết)
        var p16pm = P(ipSeries[16], "iPhone 16 Pro Max 256GB", "iphone-16-pro-max-256gb", 34_990_000, 30_490_000, featured: true, bestSeller: true);
        AddOptions(p16pm, "Dung lượng", 1, ("256GB", 0, true), ("512GB", 3_500_000, false), ("1TB", 7_000_000, false));
        AddOptions(p16pm, "Màu sắc", 2, ("Titan Đen", 0, true), ("Titan Trắng", 0, false), ("Titan Tự Nhiên", 0, false), ("Titan Sa Mạc", 0, false));
        AddOptions(p16pm, "Tình trạng", 3, ("99%", 0, true), ("98%", -1_500_000, false));
        AddOptions(p16pm, "Loại SIM", 4, ("Chính hãng VN/A", 0, true), ("LL/A (Mỹ)", -800_000, false));

        var p15pm = P(ipSeries[15], "iPhone 15 Pro Max 256GB", "iphone-15-pro-max-256gb", 29_990_000, 20_990_000, bestSeller: true);
        AddOptions(p15pm, "Màu sắc", 1, ("Titan Đen", 0, true), ("Titan Trắng", 0, false), ("Titan Xanh", 0, false));
        AddOptions(p15pm, "Tình trạng", 2, ("99%", 0, true), ("98%", -1_200_000, false));

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

    private static Category Cat(string name, string slug, int order, Category? parent = null) => new()
    {
        Name = name,
        Slug = slug,
        DisplayOrder = order,
        Parent = parent,
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
                $"<p><strong>{name}</strong> tại RenewZone Demo là sản phẩm chính hãng, nguồn gốc rõ ràng, " +
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

    private static void AddOptions(Product p, string groupName, int order, params (string Value, decimal Adjust, bool IsDefault)[] values)
    {
        var group = new ProductOptionGroup { Name = groupName, DisplayOrder = order };
        for (var i = 0; i < values.Length; i++)
        {
            group.Values.Add(new ProductOptionValue
            {
                Value = values[i].Value,
                PriceAdjustment = values[i].Adjust,
                IsDefault = values[i].IsDefault,
                DisplayOrder = i
            });
        }
        p.OptionGroups.Add(group);
    }

    // =============================================================
    // Tin công nghệ (nội dung mẫu TỰ VIẾT)
    // =============================================================
    private static async Task SeedNewsAsync(AppDbContext db)
    {
        if (await db.NewsArticles.AnyAsync()) return;

        var articles = new (string Title, string Slug, string Tag, bool Featured, int DaysAgo)[]
        {
            ("iPhone 17 Pro Max lộ diện: thiết kế mới, camera nâng cấp toàn diện", "iphone-17-pro-max-lo-dien-thiet-ke-moi", "Thiết bị", true, 1),
            ("Apple phát hành bản cập nhật iOS mới: vá lỗi bảo mật quan trọng", "apple-phat-hanh-ban-cap-nhat-ios-moi", "Khoa học công nghệ", true, 3),
            ("MacBook Air M4 có gì mới? So sánh nhanh với thế hệ M3", "macbook-air-m4-co-gi-moi-so-sanh-m3", "Thiết bị", true, 5),
            ("5 mẹo tiết kiệm pin iPhone ai cũng nên biết", "5-meo-tiet-kiem-pin-iphone", "Thủ thuật", false, 8),
            ("Nên mua iPhone 15 Pro Max cũ 99% hay iPhone 16 mới?", "nen-mua-iphone-15-pro-max-cu-hay-iphone-16-moi", "Tư vấn", false, 12),
            ("Apple Watch hỗ trợ theo dõi sức khỏe tim mạch như thế nào?", "apple-watch-theo-doi-suc-khoe-tim-mach", "Khoa học công nghệ", false, 15),
            ("Hướng dẫn mua điện thoại trả góp 0% đơn giản tại TP.HCM", "huong-dan-mua-dien-thoai-tra-gop-0-phan-tram", "Tư vấn", false, 20),
            ("AirPods thế hệ mới có thể đo nhịp tim ngay trên tai nghe?", "airpods-the-he-moi-do-nhip-tim", "Thiết bị", false, 25),
        };

        var now = DateTime.Now;
        foreach (var a in articles)
        {
            db.NewsArticles.Add(new NewsArticle
            {
                Title = a.Title,
                Slug = a.Slug,
                CategoryTag = a.Tag,
                IsFeatured = a.Featured,
                ThumbnailUrl = NewsImg,
                PublishedAt = now.AddDays(-a.DaysAgo),
                Excerpt = $"{a.Title}. Bài viết demo phục vụ học tập — nội dung chi tiết sẽ được biên tập trong trang quản trị.",
                ContentHtml =
                    $"<p><strong>{a.Title}</strong></p>" +
                    "<p>Đây là nội dung mẫu tự viết cho dự án học tập RenewZone Clone. " +
                    "Bài viết thực tế sẽ được admin soạn bằng trình soạn thảo rich text ở Giai đoạn 6.</p>" +
                    "<p>Đoạn thứ hai minh họa bố cục nhiều đoạn văn của một bài tin công nghệ thông thường, " +
                    "giúp kiểm tra hiển thị trang chi tiết bài viết khi phát triển giao diện.</p>"
            });
        }

        await db.SaveChangesAsync();
    }

    // =============================================================
    // Trang tĩnh: giới thiệu + chính sách (nội dung mẫu TỰ VIẾT)
    // =============================================================
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
                    "<p>Nội dung mẫu tự viết cho dự án học tập RenewZone Clone. " +
                    "Nội dung chính thức của trang này sẽ được biên tập trong trang quản trị (Giai đoạn 6).</p>" +
                    "<p>Trang được render động theo slug từ bảng StaticPages — thêm trang mới không cần sửa code.</p>"
            });
        }

        await db.SaveChangesAsync();
    }

    // =============================================================
    // Banner trang chủ
    // =============================================================
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

    // =============================================================
    // Đánh giá khách hàng (tên giả lập)
    // =============================================================
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
}
