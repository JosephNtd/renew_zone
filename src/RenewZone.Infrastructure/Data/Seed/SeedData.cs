using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;

namespace RenewZone.Infrastructure.Data.Seed;

/// <summary>
/// Dá»¯ liá»‡u máº«u mÃ´ phá»ng catalog site gá»‘c (tÃªn/giÃ¡ tham kháº£o thá»‹ trÆ°á»ng) â€” mÃ´ táº£ Tá»° VIáº¾T,
/// áº£nh placeholder, thÃ´ng tin cá»­a hÃ ng GIáº¢ Láº¬P (xem CLAUDE.md LÆ°u Ã½ 6.2).
/// Má»—i hÃ m seed tá»± kiá»ƒm tra báº£ng trá»‘ng trÆ°á»›c khi chÃ¨n â†’ idempotent.
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
        // Chạy CUỐI: đổi tên thương hiệu cũ còn sót trên DB đã seed từ trước (local + hosting).
        // Phải sau BackfillDetailedContentAsync vì hàm đó ghi nội dung mới vào Products/NewsArticles.
        await BackfillRebrandAsync(db);
    }

    // Cáº¥u hÃ¬nh site (thÃ´ng tin GIáº¢ Láº¬P cho demo).
    // Idempotent á»Ÿ má»©c Tá»ªNG KEY: DB Ä‘Ã£ cÃ³ settings cÅ© váº«n nháº­n Ä‘Æ°á»£c key má»›i cá»§a giai Ä‘oáº¡n sau
    // (náº¿u check "báº£ng trá»‘ng" thÃ¬ thÃªm key má»›i sáº½ khÃ´ng bao giá» Ä‘Æ°á»£c seed vÃ o DB Ä‘ang cháº¡y).
    private static async Task SeedSiteSettingsAsync(AppDbContext db)
    {
        var existingKeys = await db.SiteSettings.Select(s => s.Key).ToListAsync();
        var existing = existingKeys.ToHashSet(StringComparer.Ordinal);

        var defaults = new List<SiteSetting>();
        defaults.AddRange([
            new SiteSetting { Key = SettingKeys.StoreName, Value = "Apple Store Uyên NTT" },
            // Chưa có ảnh logo (LogoUrl/LogoDarkUrl để trống) ⇒ web khách hiện logo CHỮ,
            // và đây là phần được tô màu thương hiệu trong tên cửa hàng ở trên.
            new SiteSetting { Key = SettingKeys.LogoHighlight, Value = "Uyên NTT" },
            new SiteSetting { Key = SettingKeys.Tagline, Value = "Thu cÅ© Ä‘á»•i má»›i - LÃªn Ä‘á»i dá»… dÃ ng" },
            new SiteSetting { Key = SettingKeys.Hotline, Value = "0900.000.000" },
            new SiteSetting { Key = SettingKeys.Address, Value = "123 ÄÆ°á»ng Sá»‘ 1, PhÆ°á»ng Demo, TP.HCM" },
            new SiteSetting { Key = SettingKeys.OpenHours, Value = "8h30 - 21h30 (T2 - CN)" },
            new SiteSetting { Key = SettingKeys.Email, Value = "lienhe@applestoreuyenntt.local" },
            new SiteSetting { Key = SettingKeys.TaxCode, Value = "0000000000" },
            new SiteSetting { Key = SettingKeys.FacebookUrl, Value = "https://facebook.com/" },
            new SiteSetting { Key = SettingKeys.MessengerUrl, Value = "https://m.me/" },
            new SiteSetting { Key = SettingKeys.ZaloUrl, Value = "https://zalo.me/" },
            new SiteSetting { Key = SettingKeys.MapUrl, Value = "https://maps.google.com/" },
            // Báº£n Ä‘á»“ chung chung (trung tÃ¢m TP.HCM) â€” Ä‘á»‹a chá»‰ cá»­a hÃ ng lÃ  giáº£ láº­p nÃªn khÃ´ng nhÃºng toáº¡ Ä‘á»™ tháº­t
            new SiteSetting
            {
                Key = SettingKeys.MapEmbedUrl,
                Value = "https://www.google.com/maps?q=Ho+Chi+Minh+City&output=embed"
            },

            // Chuyá»ƒn khoáº£n GIáº¢ Láº¬P (LÆ°u Ã½ 6.2 â€” tuyá»‡t Ä‘á»‘i khÃ´ng dÃ¹ng tÃ i khoáº£n tháº­t)
            new SiteSetting { Key = SettingKeys.BankName, Value = "Demo Bank (ngÃ¢n hÃ ng giáº£ láº­p)" },
            new SiteSetting { Key = SettingKeys.BankAccountNumber, Value = "0000 0000 0000" },
            new SiteSetting { Key = SettingKeys.BankAccountName, Value = "APPLE STORE UYEN NTT" },
            new SiteSetting { Key = SettingKeys.BankTransferNoteTemplate, Value = "Thanh toan don {0}" },

            new SiteSetting { Key = SettingKeys.ShippingNote, Value = "Shop bÃ¡o khi xÃ¡c nháº­n" },
            new SiteSetting
            {
                Key = SettingKeys.ContactSubjects,
                Value = "TÆ° váº¥n mua hÃ ng|Thu cÅ© Ä‘á»•i má»›i|Báº£o hÃ nh|Sá»­a chá»¯a|Khiáº¿u náº¡i|KhÃ¡c"
            },
            new SiteSetting
            {
                Key = SettingKeys.ContactIntro,
                Value = "Báº¡n cáº§n tÆ° váº¥n chá»n mÃ¡y, bÃ¡o giÃ¡ thu cÅ© Ä‘á»•i má»›i hay há»— trá»£ báº£o hÃ nh? " +
                        "Äá»ƒ láº¡i thÃ´ng tin bÃªn dÆ°á»›i, shop pháº£n há»“i ngay trong giá» lÃ m viá»‡c."
            },
            new SiteSetting { Key = SettingKeys.NewsPageTitle, Value = "Tin cÃ´ng nghá»‡" },
            new SiteSetting
            {
                Key = SettingKeys.NewsPageIntro,
                Value = "Tin tá»©c, Ä‘Ã¡nh giÃ¡ vÃ  máº¹o dÃ¹ng thiáº¿t bá»‹ Apple â€” cáº­p nháº­t má»—i tuáº§n."
            },
            new SiteSetting
            {
                Key = SettingKeys.FooterAbout,
                Value = "ChuyÃªn iPhone, MacBook, iPad, Apple Watch, AirPods chÃ­nh hÃ£ng â€” má»›i & Ä‘Ã£ qua sá»­ dá»¥ng."
            },
            new SiteSetting
            {
                Key = SettingKeys.FooterDisclaimer,
                Value = "Website há»c táº­p (clone) â€” khÃ´ng pháº£i cá»­a hÃ ng tháº­t, khÃ´ng bÃ¡n hÃ ng."
            }]);

        var missing = defaults.Where(s => !existing.Contains(s.Key)).ToList();
        if (missing.Count == 0) return;

        db.SiteSettings.AddRange(missing);
        await db.SaveChangesAsync();
    }

    // Danh má»¥c 2 cáº¥p + 22 sáº£n pháº©m (kÃ¨m variants cho sáº£n pháº©m chá»§ lá»±c)
    private static async Task SeedCatalogAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        // Danh má»¥c cha
        var iphone = Cat("iPhone", "iphone", 1, icon: "bi-phone");
        var macbook = Cat("MacBook", "macbook", 2, icon: "bi-laptop");
        var ipad = Cat("iPad", "ipad", 3, icon: "bi-tablet");
        var watch = Cat("Apple Watch", "apple-watch", 4, icon: "bi-smartwatch");
        var airpods = Cat("AirPods", "airpods", 5, icon: "bi-earbuds");
        var thanhLy = Cat("HÃ ng thanh lÃ½", "hang-thanh-ly", 6, icon: "bi-box-seam");

        // Series con (hiá»ƒn thá»‹ thÃ nh tab lá»c trÃªn trang danh má»¥c)
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

        // Sáº£n pháº©m (giÃ¡ VND; sale = null â‡’ bÃ¡n Ä‘Ãºng giÃ¡ niÃªm yáº¿t)
        var p16pm = P(ipSeries[16], "iPhone 16 Pro Max 256GB", "iphone-16-pro-max-256gb", 34_990_000, 30_490_000, featured: true, bestSeller: true);
        AddOptions(p16pm, "Dung lÆ°á»£ng", 1, ("256GB", 0, true), ("512GB", 3_500_000, false), ("1TB", 7_000_000, false));
        AddOptions(p16pm, "MÃ u sáº¯c", 2, ("Titan Äen", 0, true), ("Titan Tráº¯ng", 0, false), ("Titan Tá»± NhiÃªn", 0, false), ("Titan Sa Máº¡c", 0, false));
        AddOptions(p16pm, "TÃ¬nh tráº¡ng", 3, ("99%", 0, true), ("98%", -1_500_000, false));
        AddOptions(p16pm, "Loáº¡i SIM", 4, ("ChÃ­nh hÃ£ng VN/A", 0, true), ("LL/A (Má»¹)", -800_000, false));

        var p15pm = P(ipSeries[15], "iPhone 15 Pro Max 256GB", "iphone-15-pro-max-256gb", 29_990_000, 20_990_000, bestSeller: true);
        AddOptions(p15pm, "MÃ u sáº¯c", 1, ("Titan Äen", 0, true), ("Titan Tráº¯ng", 0, false), ("Titan Xanh", 0, false));
        AddOptions(p15pm, "TÃ¬nh tráº¡ng", 2, ("99%", 0, true), ("98%", -1_200_000, false));

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

            P(thanhLy, "iPhone 12 64GB - Thanh lÃ½ 97%", "iphone-12-64gb-thanh-ly", 8_990_000, 4_990_000),
            P(thanhLy, "MacBook Air M1 - Thanh lÃ½ 96%", "macbook-air-m1-thanh-ly", 13_990_000, 10_490_000),
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
        IconClass = icon, // chá»‰ danh má»¥c cha cáº§n icon (hiá»‡n á»Ÿ menu) â€” con Ä‘á»ƒ null
        ImageUrl = parent is null ? ProductImg : null // danh má»¥c cha cÃ³ áº£nh cho khá»‘i danh má»¥c trang chá»§
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
            ShortDescription = $"{name} - hÃ ng chÃ­nh hÃ£ng, báº£o hÃ nh dÃ i háº¡n, há»— trá»£ tráº£ gÃ³p 0%, thu cÅ© Ä‘á»•i má»›i trá»£ giÃ¡.",
            DescriptionHtml =
                $"<p><strong>{name}</strong> táº¡i Apple Store Uyên NTT lÃ  sáº£n pháº©m chÃ­nh hÃ£ng, nguá»“n gá»‘c rÃµ rÃ ng, " +
                "Ä‘Æ°á»£c kiá»ƒm tra ká»¹ trÆ°á»›c khi giao. MÃ¡y Ä‘Ã£ qua sá»­ dá»¥ng Ä‘Æ°á»£c phÃ¢n loáº¡i tÃ¬nh tráº¡ng 98%/99% minh báº¡ch.</p>" +
                "<p>KhÃ¡ch hÃ ng Ä‘Æ°á»£c há»— trá»£ tráº£ gÃ³p 0% qua tháº» tÃ­n dá»¥ng, thu cÅ© Ä‘á»•i má»›i trá»£ giÃ¡ vÃ  báº£o hÃ nh " +
                "dÃ i háº¡n táº¡i cá»­a hÃ ng. Ná»™i dung mÃ´ táº£ chi tiáº¿t sáº½ Ä‘Æ°á»£c biÃªn táº­p trong trang quáº£n trá»‹.</p>",
            SpecsHtml =
                "<ul>" +
                "<li><strong>MÃ n hÃ¬nh:</strong> Ä‘ang cáº­p nháº­t</li>" +
                "<li><strong>Chip:</strong> Ä‘ang cáº­p nháº­t</li>" +
                "<li><strong>Camera:</strong> Ä‘ang cáº­p nháº­t</li>" +
                "<li><strong>Pin &amp; sáº¡c:</strong> Ä‘ang cáº­p nháº­t</li>" +
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

    // ChuyÃªn má»¥c tin tá»©c (slug dÃ¹ng á»Ÿ /tin-tuc?chuyen-muc=)
    private static async Task SeedNewsCategoriesAsync(AppDbContext db)
    {
        if (await db.NewsCategories.AnyAsync()) return;

        var categories = new (string Name, string Slug)[]
        {
            ("Thiáº¿t bá»‹", "thiet-bi"),
            ("Khoa há»c cÃ´ng nghá»‡", "khoa-hoc-cong-nghe"),
            ("Thá»§ thuáº­t", "thu-thuat"),
            ("TÆ° váº¥n", "tu-van"),
        };

        var order = 1;
        foreach (var c in categories)
        {
            db.NewsCategories.Add(new NewsCategory { Name = c.Name, Slug = c.Slug, DisplayOrder = order++ });
        }

        await db.SaveChangesAsync();
    }

    // Tin cÃ´ng nghá»‡ (ná»™i dung máº«u Tá»° VIáº¾T)
    private static async Task SeedNewsAsync(AppDbContext db)
    {
        if (await db.NewsArticles.AnyAsync()) return;

        // ChuyÃªn má»¥c Ä‘Ã£ seed á»Ÿ bÆ°á»›c trÆ°á»›c â€” map slug â†’ Id Ä‘á»ƒ gÃ¡n CategoryId
        var catBySlug = await db.NewsCategories.ToDictionaryAsync(c => c.Slug, c => c.Id);

        var articles = new (string Title, string Slug, string CategorySlug, bool Featured, int DaysAgo)[]
        {
            ("iPhone 17 Pro Max lá»™ diá»‡n: thiáº¿t káº¿ má»›i, camera nÃ¢ng cáº¥p toÃ n diá»‡n", "iphone-17-pro-max-lo-dien-thiet-ke-moi", "thiet-bi", true, 1),
            ("Apple phÃ¡t hÃ nh báº£n cáº­p nháº­t iOS má»›i: vÃ¡ lá»—i báº£o máº­t quan trá»ng", "apple-phat-hanh-ban-cap-nhat-ios-moi", "khoa-hoc-cong-nghe", true, 3),
            ("MacBook Air M4 cÃ³ gÃ¬ má»›i? So sÃ¡nh nhanh vá»›i tháº¿ há»‡ M3", "macbook-air-m4-co-gi-moi-so-sanh-m3", "thiet-bi", true, 5),
            ("5 máº¹o tiáº¿t kiá»‡m pin iPhone ai cÅ©ng nÃªn biáº¿t", "5-meo-tiet-kiem-pin-iphone", "thu-thuat", false, 8),
            ("NÃªn mua iPhone 15 Pro Max cÅ© 99% hay iPhone 16 má»›i?", "nen-mua-iphone-15-pro-max-cu-hay-iphone-16-moi", "tu-van", false, 12),
            ("Apple Watch há»— trá»£ theo dÃµi sá»©c khá»e tim máº¡ch nhÆ° tháº¿ nÃ o?", "apple-watch-theo-doi-suc-khoe-tim-mach", "khoa-hoc-cong-nghe", false, 15),
            ("HÆ°á»›ng dáº«n mua Ä‘iá»‡n thoáº¡i tráº£ gÃ³p 0% Ä‘Æ¡n giáº£n táº¡i TP.HCM", "huong-dan-mua-dien-thoai-tra-gop-0-phan-tram", "tu-van", false, 20),
            ("AirPods tháº¿ há»‡ má»›i cÃ³ thá»ƒ Ä‘o nhá»‹p tim ngay trÃªn tai nghe?", "airpods-the-he-moi-do-nhip-tim", "thiet-bi", false, 25),
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
                Excerpt = $"{a.Title}. BÃ i viáº¿t demo phá»¥c vá»¥ há»c táº­p â€” ná»™i dung chi tiáº¿t sáº½ Ä‘Æ°á»£c biÃªn táº­p trong trang quáº£n trá»‹.",
                ContentHtml =
                    $"<p><strong>{a.Title}</strong></p>" +
                    "<p>ÄÃ¢y lÃ  ná»™i dung máº«u tá»± viáº¿t cho dá»± Ã¡n há»c táº­p Apple Store Uyên NTT. " +
                    "BÃ i viáº¿t thá»±c táº¿ sáº½ Ä‘Æ°á»£c admin soáº¡n báº±ng trÃ¬nh soáº¡n tháº£o rich text á»Ÿ Giai Ä‘oáº¡n 6.</p>" +
                    "<p>Äoáº¡n thá»© hai minh há»a bá»‘ cá»¥c nhiá»u Ä‘oáº¡n vÄƒn cá»§a má»™t bÃ i tin cÃ´ng nghá»‡ thÃ´ng thÆ°á»ng, " +
                    "giÃºp kiá»ƒm tra hiá»ƒn thá»‹ trang chi tiáº¿t bÃ i viáº¿t khi phÃ¡t triá»ƒn giao diá»‡n.</p>"
            });
        }

        await db.SaveChangesAsync();
    }

    // Trang tÄ©nh: giá»›i thiá»‡u + chÃ­nh sÃ¡ch (ná»™i dung máº«u Tá»° VIáº¾T)
    private static async Task SeedStaticPagesAsync(AppDbContext db)
    {
        if (await db.StaticPages.AnyAsync()) return;

        var pages = new (string Title, string Slug)[]
        {
            ("Giá»›i thiá»‡u", "gioi-thieu"),
            ("ChÃ­nh sÃ¡ch báº£o hÃ nh", "chinh-sach-bao-hanh"),
            ("ChÃ­nh sÃ¡ch thanh toÃ¡n", "chinh-sach-thanh-toan"),
            ("ChÃ­nh sÃ¡ch kiá»ƒm hÃ ng", "chinh-sach-kiem-hang"),
            ("ChÃ­nh sÃ¡ch mua hÃ ng", "chinh-sach-mua-hang"),
            ("ChÃ­nh sÃ¡ch váº­n chuyá»ƒn", "chinh-sach-van-chuyen"),
            ("ChÃ­nh sÃ¡ch báº£o máº­t", "chinh-sach-bao-mat"),
            ("ChÃ­nh sÃ¡ch tráº£ gÃ³p", "chinh-sach-tra-gop"),
            ("Báº£ng giÃ¡ thu cÅ© Ä‘á»•i má»›i", "bang-gia-thu-cu-doi-moi"),
            ("MÃ¡y má»›i", "may-moi"),
            ("MÃ¡y Ä‘Ã£ qua sá»­ dá»¥ng", "may-da-qua-su-dung"),
            ("Báº£o hÃ nh 20 nÄƒm", "bao-hanh-20-nam"),
        };

        foreach (var page in pages)
        {
            db.StaticPages.Add(new StaticPage
            {
                Title = page.Title,
                Slug = page.Slug,
                ContentHtml =
                    $"<h2>{page.Title}</h2>" +
                    "<p>Ná»™i dung máº«u tá»± viáº¿t cho dá»± Ã¡n há»c táº­p Apple Store Uyên NTT. " +
                    "Ná»™i dung chÃ­nh thá»©c cá»§a trang nÃ y sáº½ Ä‘Æ°á»£c biÃªn táº­p trong trang quáº£n trá»‹ (Giai Ä‘oáº¡n 6).</p>" +
                    "<p>Trang Ä‘Æ°á»£c render Ä‘á»™ng theo slug tá»« báº£ng StaticPages â€” thÃªm trang má»›i khÃ´ng cáº§n sá»­a code.</p>"
            });
        }

        // 2 trang sá»­a chá»¯a cÃ³ ná»™i dung dÃ i hÆ¡n (menu trá» tháº³ng tá»›i) â€” ná»™i dung Tá»° VIáº¾T, báº£ng giÃ¡ tham kháº£o giáº£ láº­p
        db.StaticPages.Add(new StaticPage
        {
            Title = "Dá»‹ch vá»¥ sá»­a chá»¯a",
            Slug = "sua-chua",
            ContentHtml = RepairServiceHtml
        });
        db.StaticPages.Add(new StaticPage
        {
            Title = "Sá»­a chá»¯a pháº§n cá»©ng",
            Slug = "sua-chua-phan-cung",
            ContentHtml = RepairHardwareHtml
        });

        await db.SaveChangesAsync();
    }

    private const string RepairServiceHtml = """
        <h2>Dá»‹ch vá»¥ sá»­a chá»¯a thiáº¿t bá»‹ Apple</h2>
        <p>Trung tÃ¢m ká»¹ thuáº­t Apple Store Uyên NTT nháº­n kiá»ƒm tra vÃ  sá»­a chá»¯a iPhone, iPad, MacBook,
        Apple Watch vÃ  AirPods. MÃ¡y Ä‘Æ°á»£c kiá»ƒm tra miá»…n phÃ­, bÃ¡o giÃ¡ trÆ°á»›c khi sá»­a, khÃ¡ch Ä‘á»“ng Ã½ má»›i tiáº¿n hÃ nh.</p>

        <h3>NhÃ³m dá»‹ch vá»¥ chÃºng tÃ´i nháº­n</h3>
        <ul>
          <li><strong>MÃ n hÃ¬nh &amp; cáº£m á»©ng:</strong> thay mÃ n hÃ¬nh, xá»­ lÃ½ sá»c mÃ n, loáº¡n cáº£m á»©ng, Ã¡m mÃ u.</li>
          <li><strong>Nguá»“n &amp; pin:</strong> thay pin chai, sá»­a lá»—i khÃ´ng lÃªn nguá»“n, sáº­p nguá»“n ngáº«u nhiÃªn.</li>
          <li><strong>Sáº¡c &amp; káº¿t ná»‘i:</strong> thay chÃ¢n sáº¡c, sá»­a lá»—i sáº¡c cháº­p chá»n, máº¥t Wi-Fi/Bluetooth.</li>
          <li><strong>Camera &amp; loa mic:</strong> thay cá»¥m camera má»/rung, xá»­ lÃ½ loa rÃ¨, mic khÃ´ng thu tiáº¿ng.</li>
          <li><strong>Pháº§n má»m:</strong> cÃ i Ä‘áº·t láº¡i há»‡ Ä‘iá»u hÃ nh, cá»©u dá»¯ liá»‡u, xá»­ lÃ½ mÃ¡y treo tÃ¡o.</li>
          <li><strong>Vá»‡ sinh &amp; báº£o dÆ°á»¡ng:</strong> vá»‡ sinh bo máº¡ch, tra keo táº£n nhiá»‡t MacBook, chá»‘ng áº©m.</li>
        </ul>

        <h3>Quy trÃ¬nh 4 bÆ°á»›c</h3>
        <ol>
          <li><strong>Tiáº¿p nháº­n:</strong> ghi nháº­n tÃ¬nh tráº¡ng mÃ¡y, dÃ¡n phiáº¿u vÃ  niÃªm phong trÆ°á»›c máº·t khÃ¡ch.</li>
          <li><strong>Kiá»ƒm tra:</strong> ká»¹ thuáº­t viÃªn Ä‘o Ä‘áº¡c, xÃ¡c Ä‘á»‹nh lá»—i vÃ  bÃ¡o giÃ¡ cá»¥ thá»ƒ â€” miá»…n phÃ­ kiá»ƒm tra.</li>
          <li><strong>Sá»­a chá»¯a:</strong> tiáº¿n hÃ nh sau khi khÃ¡ch xÃ¡c nháº­n bÃ¡o giÃ¡; linh kiá»‡n thay ra Ä‘Æ°á»£c tráº£ láº¡i khÃ¡ch.</li>
          <li><strong>BÃ n giao:</strong> test láº¡i cÃ¹ng khÃ¡ch, xuáº¥t phiáº¿u báº£o hÃ nh theo tá»«ng háº¡ng má»¥c.</li>
        </ol>

        <h3>Báº£ng giÃ¡ tham kháº£o</h3>
        <p>GiÃ¡ dÆ°á»›i Ä‘Ã¢y lÃ  <em>má»©c tham kháº£o cho dá»± Ã¡n demo</em>, chi phÃ­ thá»±c táº¿ phá»¥ thuá»™c model vÃ  tÃ¬nh tráº¡ng mÃ¡y.</p>
        <table>
          <thead>
            <tr><th>Háº¡ng má»¥c</th><th>Thá»i gian</th><th>Chi phÃ­ tham kháº£o</th><th>Báº£o hÃ nh</th></tr>
          </thead>
          <tbody>
            <tr><td>Thay pin iPhone</td><td>30 â€“ 45 phÃºt</td><td>450.000 â€“ 1.200.000Ä‘</td><td>6 thÃ¡ng</td></tr>
            <tr><td>Thay mÃ n hÃ¬nh iPhone</td><td>45 â€“ 90 phÃºt</td><td>1.200.000 â€“ 6.500.000Ä‘</td><td>6 thÃ¡ng</td></tr>
            <tr><td>Thay chÃ¢n sáº¡c</td><td>60 phÃºt</td><td>350.000 â€“ 900.000Ä‘</td><td>3 thÃ¡ng</td></tr>
            <tr><td>Thay pin MacBook</td><td>2 â€“ 4 giá»</td><td>1.500.000 â€“ 3.500.000Ä‘</td><td>6 thÃ¡ng</td></tr>
            <tr><td>Vá»‡ sinh &amp; tra keo táº£n nhiá»‡t MacBook</td><td>2 giá»</td><td>350.000 â€“ 600.000Ä‘</td><td>1 thÃ¡ng</td></tr>
            <tr><td>CÃ i Ä‘áº·t láº¡i há»‡ Ä‘iá»u hÃ nh</td><td>1 â€“ 2 giá»</td><td>150.000 â€“ 300.000Ä‘</td><td>â€”</td></tr>
          </tbody>
        </table>

        <h3>Cam káº¿t cá»§a chÃºng tÃ´i</h3>
        <ul>
          <li>Kiá»ƒm tra vÃ  bÃ¡o giÃ¡ miá»…n phÃ­, khÃ´ng sá»­a váº«n khÃ´ng máº¥t phÃ­.</li>
          <li>KhÃ´ng thay linh kiá»‡n khi chÆ°a cÃ³ xÃ¡c nháº­n cá»§a khÃ¡ch hÃ ng.</li>
          <li>Báº£o hÃ nh theo háº¡ng má»¥c, ghi rÃµ trÃªn phiáº¿u bÃ n giao.</li>
          <li>HoÃ n tiá»n náº¿u khÃ´ng kháº¯c phá»¥c Ä‘Æ°á»£c Ä‘Ãºng lá»—i Ä‘Ã£ nháº­n.</li>
        </ul>
        """;

    // ChÃº Ã½: TradeInPriceTableHtml Ä‘Æ°á»£c Ä‘á»‹nh nghÄ©a Ä‘áº§y Ä‘á»§ trong SeedData.Content.cs

    private const string RepairHardwareHtml = """
        <h2>Sá»­a chá»¯a pháº§n cá»©ng chuyÃªn sÃ¢u</h2>
        <p>Vá»›i cÃ¡c lá»—i pháº§n cá»©ng náº·ng â€” vÃ o nÆ°á»›c, rÆ¡i vá»¡, cháº­p nguá»“n, máº¥t tÃ­n hiá»‡u â€” mÃ¡y Ä‘Æ°á»£c Ä‘Æ°a sang
        khu vá»±c ká»¹ thuáº­t chuyÃªn sÃ¢u Ä‘á»ƒ Ä‘o Ä‘áº¡c trÃªn sÆ¡ Ä‘á»“ máº¡ch trÆ°á»›c khi can thiá»‡p.</p>

        <h3>CÃ¡c lá»—i pháº§n cá»©ng thÆ°á»ng gáº·p</h3>
        <ul>
          <li><strong>VÃ o nÆ°á»›c:</strong> thÃ¡o mÃ¡y, vá»‡ sinh siÃªu Ã¢m, xá»­ lÃ½ oxy hÃ³a chÃ¢n linh kiá»‡n.</li>
          <li><strong>Lá»—i nguá»“n:</strong> Ä‘o dÃ²ng tiÃªu thá»¥, dÃ² cháº­p, thay IC nguá»“n.</li>
          <li><strong>Lá»—i sáº¡c:</strong> thay IC sáº¡c, xá»­ lÃ½ Ä‘Æ°á»ng tÃ­n hiá»‡u bá»‹ Ä‘á»©t sau va Ä‘áº­p.</li>
          <li><strong>Máº¥t sÃ³ng / Wi-Fi:</strong> hÃ n láº¡i hoáº·c thay IC thu phÃ¡t tÃ­n hiá»‡u.</li>
          <li><strong>Lá»—i mÃ n hÃ¬nh MacBook:</strong> thay cÃ¡p mÃ n, xá»­ lÃ½ lá»—i backlight.</li>
          <li><strong>Lá»—i bÃ n phÃ­m &amp; trackpad:</strong> thay cá»¥m phÃ­m, xá»­ lÃ½ phÃ­m liá»‡t do vÃ o nÆ°á»›c.</li>
        </ul>

        <h3>Thiáº¿t bá»‹ ká»¹ thuáº­t sá»­ dá»¥ng</h3>
        <ul>
          <li>KÃ­nh hiá»ƒn vi soi máº¡ch vÃ  mÃ¡y khÃ² nhiá»‡t cÃ³ kiá»ƒm soÃ¡t nhiá»‡t Ä‘á»™.</li>
          <li>MÃ¡y Ä‘o dÃ²ng, nguá»“n phÃ²ng thÃ­ nghiá»‡m Ä‘á»ƒ dÃ² cháº­p trÆ°á»›c khi cáº¥p Ä‘iá»‡n.</li>
          <li>Bá»ƒ rá»­a siÃªu Ã¢m dÃ¹ng cho mÃ¡y vÃ o nÆ°á»›c.</li>
        </ul>

        <h3>Báº£ng giÃ¡ tham kháº£o</h3>
        <table>
          <thead>
            <tr><th>Háº¡ng má»¥c</th><th>Má»©c Ä‘á»™</th><th>Chi phÃ­ tham kháº£o</th><th>Báº£o hÃ nh</th></tr>
          </thead>
          <tbody>
            <tr><td>Vá»‡ sinh mÃ¡y vÃ o nÆ°á»›c</td><td>Nháº¹</td><td>300.000 â€“ 700.000Ä‘</td><td>1 thÃ¡ng</td></tr>
            <tr><td>Sá»­a lá»—i nguá»“n (thay IC nguá»“n)</td><td>Náº·ng</td><td>800.000 â€“ 2.500.000Ä‘</td><td>3 thÃ¡ng</td></tr>
            <tr><td>Sá»­a lá»—i sáº¡c (thay IC sáº¡c)</td><td>Trung bÃ¬nh</td><td>600.000 â€“ 1.500.000Ä‘</td><td>3 thÃ¡ng</td></tr>
            <tr><td>Kháº¯c phá»¥c máº¥t sÃ³ng / Wi-Fi</td><td>Náº·ng</td><td>900.000 â€“ 2.200.000Ä‘</td><td>3 thÃ¡ng</td></tr>
            <tr><td>Thay cÃ¡p mÃ n hÃ¬nh MacBook</td><td>Trung bÃ¬nh</td><td>1.200.000 â€“ 2.800.000Ä‘</td><td>6 thÃ¡ng</td></tr>
            <tr><td>Thay bÃ n phÃ­m MacBook</td><td>Náº·ng</td><td>1.500.000 â€“ 3.900.000Ä‘</td><td>6 thÃ¡ng</td></tr>
          </tbody>
        </table>

        <h3>LÆ°u Ã½ trÆ°á»›c khi gá»­i mÃ¡y</h3>
        <ol>
          <li>Sao lÆ°u dá»¯ liá»‡u náº¿u mÃ¡y cÃ²n má»Ÿ Ä‘Æ°á»£c â€” sá»­a pháº§n cá»©ng luÃ´n cÃ³ rá»§i ro máº¥t dá»¯ liá»‡u.</li>
          <li>Táº¯t tÃ­nh nÄƒng tÃ¬m thiáº¿t bá»‹ vÃ  cung cáº¥p máº­t kháº©u má»Ÿ mÃ¡y Ä‘á»ƒ ká»¹ thuáº­t test Ä‘Æ°á»£c Ä‘áº§y Ä‘á»§.</li>
          <li>MÃ¡y vÃ o nÆ°á»›c nÃªn mang tá»›i cÃ ng sá»›m cÃ ng tá»‘t, khÃ´ng tá»± sáº¥y hoáº·c cáº¯m sáº¡c.</li>
          <li>MÃ¡y Ä‘Ã£ qua sá»­a chá»¯a á»Ÿ nÆ¡i khÃ¡c váº«n nháº­n, nhÆ°ng thá»i gian xá»­ lÃ½ cÃ³ thá»ƒ lÃ¢u hÆ¡n.</li>
        </ol>

        <p><em>ToÃ n bá»™ ná»™i dung trang nÃ y lÃ  dá»¯ liá»‡u máº«u cá»§a dá»± Ã¡n há»c táº­p, khÃ´ng pháº£i cam káº¿t dá»‹ch vá»¥ tháº­t.</em></p>
        """;

    // Banner trang chá»§
    private static async Task SeedBannersAsync(AppDbContext db)
    {
        if (await db.Banners.AnyAsync()) return;

        db.Banners.AddRange(
            new Banner { Title = "iPhone 17 Pro Max - Äáº·t trÆ°á»›c hÃ´m nay", ImageUrl = BannerImg, LinkUrl = "/iphone-17-pro-max-256gb", Position = BannerPosition.HomeSlider, DisplayOrder = 1 },
            new Banner { Title = "Thu cÅ© Ä‘á»•i má»›i - Trá»£ giÃ¡ Ä‘áº¿n 2 triá»‡u", ImageUrl = BannerImg, LinkUrl = "/bang-gia-thu-cu-doi-moi", Position = BannerPosition.HomeSlider, DisplayOrder = 2 },
            new Banner { Title = "MacBook sale Ä‘áº¿n 30%", ImageUrl = BannerImg, LinkUrl = "/macbook", Position = BannerPosition.HomeSlider, DisplayOrder = 3 },
            new Banner { Title = "Æ¯u Ä‘Ã£i iPad", ImageUrl = BannerImg, LinkUrl = "/ipad", Position = BannerPosition.HomeMiddle, DisplayOrder = 1 },
            new Banner { Title = "HÃ ng thanh lÃ½ giÃ¡ tá»‘t", ImageUrl = BannerImg, LinkUrl = "/hang-thanh-ly", Position = BannerPosition.HomeMiddle, DisplayOrder = 2 });

        await db.SaveChangesAsync();
    }

    // ÄÃ¡nh giÃ¡ khÃ¡ch hÃ ng (tÃªn giáº£ láº­p)
    private static async Task SeedTestimonialsAsync(AppDbContext db)
    {
        if (await db.Testimonials.AnyAsync()) return;

        var items = new (string Name, string Content)[]
        {
            ("Anh Minh - Quáº­n 1", "Mua iPhone 15 Pro Max 99% mÃ  mÃ¡y Ä‘áº¹p nhÆ° má»›i, shop tÆ° váº¥n nhiá»‡t tÃ¬nh, giÃ¡ tá»‘t hÆ¡n nhiá»u nÆ¡i mÃ¬nh kháº£o sÃ¡t."),
            ("Chá»‹ HÆ°Æ¡ng - GÃ² Váº¥p", "LÃªn Ä‘á»i tá»« iPhone 12 sang 16 Pro Max, Ä‘Æ°á»£c trá»£ giÃ¡ thu cÅ© khÃ¡ cao. Thá»§ tá»¥c tráº£ gÃ³p nhanh gá»n."),
            ("Anh Tuáº¥n - Thá»§ Äá»©c", "MacBook Air M1 thanh lÃ½ mÃ  pin cÃ²n tá»‘t, ngoáº¡i hÃ¬nh 96% Ä‘Ãºng mÃ´ táº£. Sáº½ á»§ng há»™ tiáº¿p."),
            ("Chá»‹ Lan - BÃ¬nh Tháº¡nh", "NhÃ¢n viÃªn kiá»ƒm tra mÃ¡y ká»¹ trÆ°á»›c máº·t mÃ¬nh, báº£o hÃ nh rÃµ rÃ ng nÃªn ráº¥t yÃªn tÃ¢m."),
            ("Anh Khoa - TÃ¢n BÃ¬nh", "Äáº·t hÃ ng online, giao trong ngÃ y, mÃ¡y nguyÃªn seal Ä‘Ãºng cam káº¿t. Ráº¥t hÃ i lÃ²ng."),
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

    // Äiá»u hÆ°á»›ng: menu chÃ­nh + 2 cá»™t link á»Ÿ footer (thay hardcode trong _Layout)
    private static async Task SeedMenuAsync(AppDbContext db)
    {
        if (await db.MenuItems.AnyAsync()) return;

        // Menu chÃ­nh.
        // Má»¥c "Sáº£n pháº©m" lÃ  ProductMega: dropdown sinh Ä‘á»™ng tá»« cÃ¢y Category nÃªn khÃ´ng khai bÃ¡o link con.
        db.MenuItems.AddRange(
            Menu(MenuGroup.MainMenu, "Trang chá»§", "/", 1, icon: "bi-house-door"),
            Menu(MenuGroup.MainMenu, "Giá»›i thiá»‡u", "/gioi-thieu", 2),
            new MenuItem
            {
                Group = MenuGroup.MainMenu,
                Type = MenuItemType.ProductMega,
                Text = "Sáº£n pháº©m",
                Url = "/san-pham",
                Icon = "bi-grid",
                DisplayOrder = 3
            });

        var repair = Menu(MenuGroup.MainMenu, "Sá»­a chá»¯a", "/sua-chua", 4, icon: "bi-tools");
        repair.Children.Add(Menu(MenuGroup.MainMenu, "Dá»‹ch vá»¥ sá»­a chá»¯a", "/sua-chua", 1, icon: "bi-tools"));
        repair.Children.Add(Menu(MenuGroup.MainMenu, "Sá»­a chá»¯a pháº§n cá»©ng", "/sua-chua-phan-cung", 2, icon: "bi-cpu"));

        var policy = Menu(MenuGroup.MainMenu, "ChÃ­nh sÃ¡ch", "/chinh-sach-bao-hanh", 5, icon: "bi-shield-check");
        policy.Children.Add(Menu(MenuGroup.MainMenu, "Báº£ng giÃ¡ thu cÅ© Ä‘á»•i má»›i", "/bang-gia-thu-cu-doi-moi", 1, icon: "bi-shield-check"));
        policy.Children.Add(Menu(MenuGroup.MainMenu, "MÃ¡y má»›i", "/may-moi", 2, icon: "bi-shield-check"));
        policy.Children.Add(Menu(MenuGroup.MainMenu, "MÃ¡y Ä‘Ã£ qua sá»­ dá»¥ng", "/may-da-qua-su-dung", 3, icon: "bi-shield-check"));
        policy.Children.Add(Menu(MenuGroup.MainMenu, "Báº£o hÃ nh 20 nÄƒm", "/bao-hanh-20-nam", 4, icon: "bi-shield-check"));

        db.MenuItems.AddRange(
            repair,
            policy,
            Menu(MenuGroup.MainMenu, "Tin cÃ´ng nghá»‡", "/tin-tuc", 6, icon: "bi-newspaper"),
            Menu(MenuGroup.MainMenu, "LiÃªn há»‡", "/lien-he", 7, icon: "bi-envelope"));

        // Footer: cá»™t ChÃ­nh sÃ¡ch
        db.MenuItems.AddRange(
            Menu(MenuGroup.FooterPolicy, "ChÃ­nh sÃ¡ch báº£o hÃ nh", "/chinh-sach-bao-hanh", 1),
            Menu(MenuGroup.FooterPolicy, "ChÃ­nh sÃ¡ch thanh toÃ¡n", "/chinh-sach-thanh-toan", 2),
            Menu(MenuGroup.FooterPolicy, "ChÃ­nh sÃ¡ch váº­n chuyá»ƒn", "/chinh-sach-van-chuyen", 3),
            Menu(MenuGroup.FooterPolicy, "ChÃ­nh sÃ¡ch kiá»ƒm hÃ ng", "/chinh-sach-kiem-hang", 4),
            Menu(MenuGroup.FooterPolicy, "ChÃ­nh sÃ¡ch tráº£ gÃ³p", "/chinh-sach-tra-gop", 5),
            Menu(MenuGroup.FooterPolicy, "ChÃ­nh sÃ¡ch báº£o máº­t", "/chinh-sach-bao-mat", 6));

        // Footer: cá»™t Há»— trá»£
        db.MenuItems.AddRange(
            Menu(MenuGroup.FooterSupport, "Giá»›i thiá»‡u", "/gioi-thieu", 1),
            Menu(MenuGroup.FooterSupport, "Tin cÃ´ng nghá»‡", "/tin-tuc", 2),
            Menu(MenuGroup.FooterSupport, "Sá»­a chá»¯a", "/sua-chua", 3),
            Menu(MenuGroup.FooterSupport, "Giá» hÃ ng", "/gio-hang", 4),
            Menu(MenuGroup.FooterSupport, "LiÃªn há»‡", "/lien-he", 5));

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

    // Khá»‘i ná»™i dung dÃ¹ng láº¡i: cam káº¿t PDP, tráº¥n an giá»/checkout, kÃªnh liÃªn há»‡
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
            // Cam káº¿t á»Ÿ trang chi tiáº¿t sáº£n pháº©m
            Block(ContentBlockGroups.ProductCommitments, "bi-patch-check-fill", "HÃ ng chÃ­nh hÃ£ng, nguyÃªn seal / Ä‘Ãºng mÃ´ táº£", 1),
            Block(ContentBlockGroups.ProductCommitments, "bi-arrow-repeat", "Thu cÅ© Ä‘á»•i má»›i, thu giÃ¡ cao", 2),
            Block(ContentBlockGroups.ProductCommitments, "bi-shield-check", "Báº£o hÃ nh dÃ i háº¡n, 1 Ä‘á»•i 1", 3),
            Block(ContentBlockGroups.ProductCommitments, "bi-truck", "Giao nhanh ná»™i thÃ nh, kiá»ƒm tra khi nháº­n", 4),

            // DÃ²ng tráº¥n an á»Ÿ giá» hÃ ng + checkout
            Block(ContentBlockGroups.CartAssurance, "bi-shield-check", "Thu cÅ© Ä‘á»•i má»›i Â· Báº£o hÃ nh dÃ i háº¡n", 1),

            // KÃªnh liÃªn há»‡ nhanh á»Ÿ /lien-he
            Block(ContentBlockGroups.ContactChannels, "bi-telephone-fill", "Hotline", 1,
                subtitle: hotline, linkUrl: "tel:" + hotline.Replace(".", "").Replace(" ", "")),
            Block(ContentBlockGroups.ContactChannels, "bi-chat-dots-fill", "Zalo", 2,
                subtitle: "Chat ngay", linkUrl: "https://zalo.me/"),
            Block(ContentBlockGroups.ContactChannels, "bi-messenger", "Messenger", 3,
                subtitle: "Chat ngay", linkUrl: "https://m.me/"),
            Block(ContentBlockGroups.ContactChannels, "bi-geo-alt-fill", "Cá»­a hÃ ng", 4,
                subtitle: "Chá»‰ Ä‘Æ°á»ng", linkUrl: mapUrl));

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

    // BÃ¹ dá»¯ liá»‡u cho cÃ¡c Cá»˜T/Báº¢NG má»›i cá»§a Giai Ä‘oáº¡n 5 trÃªn DB Ä‘Ã£ seed tá»« trÆ°á»›c.
    // CÃ¡c hÃ m seed á»Ÿ trÃªn Ä‘á»u "báº£ng trá»‘ng má»›i cháº¡y" nÃªn DB cÅ© sáº½ khÃ´ng nháº­n Ä‘Æ°á»£c
    // Category.IconClass hay NewsArticle.CategoryId â€” bÃ¹ riÃªng á»Ÿ Ä‘Ã¢y, váº«n idempotent
    // (chá»‰ cháº¡m dÃ²ng Ä‘ang thiáº¿u, giá»¯ nguyÃªn Ä‘Æ¡n hÃ ng test Ä‘Ã£ cÃ³).
    private static async Task BackfillPhase5Async(AppDbContext db)
    {
        var changed = false;

        // Icon danh má»¥c cha
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

        // ChuyÃªn má»¥c cho bÃ i viáº¿t Ä‘Ã£ seed trÆ°á»›c khi cÃ³ báº£ng NewsCategories
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

        // 2 trang sá»­a chá»¯a (DB cÅ© Ä‘Ã£ cÃ³ StaticPages nÃªn hÃ m seed trang tÄ©nh khÃ´ng cháº¡y)
        var repairPages = new (string Slug, string Title, string Html)[]
        {
            ("sua-chua", "Dá»‹ch vá»¥ sá»­a chá»¯a", RepairServiceHtml),
            ("sua-chua-phan-cung", "Sá»­a chá»¯a pháº§n cá»©ng", RepairHardwareHtml),
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
