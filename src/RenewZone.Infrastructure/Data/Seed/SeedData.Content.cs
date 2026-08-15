using Microsoft.EntityFrameworkCore;

namespace RenewZone.Infrastructure.Data.Seed;

/// <summary>
/// Nội dung ĐẦY ĐỦ (tự viết, không copy site gốc — Lưu ý 6.2) cho 22 sản phẩm + 8 bài tin công nghệ
/// đã seed ở <see cref="SeedData"/>. Tách file riêng vì SeedData.cs đã dài (713 dòng) và đây thuần là
/// dữ liệu văn bản, không phải logic.
///
/// Chạy dưới dạng BACKFILL (giống <c>BackfillPhase5Async</c>) chứ không sửa hàm seed gốc: 2 bảng
/// Products/NewsArticles đã có dữ liệu từ lâu nên "if (AnyAsync()) return" sẽ không bao giờ chạy lại.
/// Idempotent bằng cách chỉ ghi đè các dòng CÒN GIỮ NGUYÊN placeholder gốc (chuỗi đánh dấu bên dưới) —
/// nếu admin đã tự sửa mô tả/thông số qua trang quản trị thì KHÔNG bị ghi đè lại.
/// </summary>
public static partial class SeedData
{
    private sealed record ProductContent(string ShortDescription, string DescriptionHtml, string SpecsHtml);

    private sealed record NewsContent(string Excerpt, string ContentHtml);

    private static async Task BackfillDetailedContentAsync(AppDbContext db)
    {
        var changed = false;

        // Marker của nội dung mẫu cũ trong P() — còn nguyên nghĩa là admin chưa từng sửa tay.
        var placeholderProducts = await db.Products
            .Where(p => p.SpecsHtml != null && p.SpecsHtml.Contains("đang cập nhật"))
            .ToListAsync();

        foreach (var product in placeholderProducts)
        {
            if (!ProductContentBySlug.TryGetValue(product.Slug, out var content)) continue;
            product.ShortDescription = content.ShortDescription;
            product.DescriptionHtml = content.DescriptionHtml;
            product.SpecsHtml = content.SpecsHtml;
            changed = true;
        }

        // Marker của nội dung mẫu cũ trong SeedNewsAsync.
        var placeholderArticles = await db.NewsArticles
            .Where(a => a.ContentHtml.Contains("Bài viết thực tế sẽ được admin soạn"))
            .ToListAsync();

        foreach (var article in placeholderArticles)
        {
            if (!NewsContentBySlug.TryGetValue(article.Slug, out var content)) continue;
            article.Excerpt = content.Excerpt;
            article.ContentHtml = content.ContentHtml;
            changed = true;
        }

        // Cập nhật "Bảng giá thu cũ đổi mới" — chỉ khi nội dung vẫn là placeholder gốc
        var tradeInPage = await db.StaticPages.FirstOrDefaultAsync(p => p.Slug == "bang-gia-thu-cu-doi-moi");
        if (tradeInPage != null
            && (tradeInPage.ContentHtml?.Contains("Nội dung mẫu tự viết cho dự án học tập") ?? false))
        {
            tradeInPage.ContentHtml = TradeInPriceTableHtml;
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }

    // Sản phẩm — mô tả + thông số theo TỪNG model (thông số kỹ thuật tham khảo công khai,
    // diễn đạt tự viết — không copy nguyên văn marketing của Apple hay site gốc).
    private static readonly Dictionary<string, ProductContent> ProductContentBySlug = new()
    {
        ["iphone-16-pro-max-256gb"] = new ProductContent(
            "iPhone 16 Pro Max 256GB - chip A18 Pro, camera Telephoto 5x, khung Titan bền bỉ. Bảo hành dài hạn, hỗ trợ trả góp 0%.",
            """
            <p><strong>iPhone 16 Pro Max</strong> là flagship mạnh nhất của Apple hiện tại, sở hữu khung Titan cấp 5 nhẹ và bền hơn thế hệ trước, cùng màn hình Super Retina XDR 6.9 inch lớn nhất từ trước đến nay trên dòng Pro.</p>
            <p>Chip A18 Pro cho hiệu năng vượt trội, xử lý mượt các tác vụ AI trên máy (Apple Intelligence), trong khi cụm camera 48MP với ống Telephoto 5x giúp chụp xa sắc nét hơn hẳn. Nút Camera Control mới cho phép chụp ảnh nhanh chỉ với một chạm.</p>
            <p>Máy tại Apple Store Uyên NTT được kiểm tra 90 điểm trước khi giao, đi kèm bảo hành dài hạn, hỗ trợ trả góp 0% và thu cũ đổi mới trợ giá cao cho khách nâng cấp.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Super Retina XDR OLED 6.9", ProMotion 120Hz, Always-On, độ sáng tối đa 2000 nits</li>
            <li><strong>Chip:</strong> Apple A18 Pro, Neural Engine 16 nhân, hỗ trợ Apple Intelligence</li>
            <li><strong>Camera sau:</strong> Chính 48MP + Ultra Wide 48MP + Telephoto 12MP (zoom quang 5x)</li>
            <li><strong>Camera trước:</strong> TrueDepth 12MP</li>
            <li><strong>Pin &amp; sạc:</strong> Sử dụng cả ngày, sạc nhanh 20W, MagSafe 25W, sạc không dây Qi2</li>
            <li><strong>Khác:</strong> Khung Titan cấp 5, chống nước IP68, nút Action + Camera Control, cổng USB-C</li>
            </ul>
            """),

        ["iphone-15-pro-max-256gb"] = new ProductContent(
            "iPhone 15 Pro Max 256GB - camera Telephoto tetraprism 5x, khung Titan, chip A17 Pro. Đã qua kiểm định kỹ, giá tốt.",
            """
            <p><strong>iPhone 15 Pro Max</strong> là chiếc iPhone đầu tiên dùng khung Titan, nhẹ hơn thế hệ thép không gỉ trước đó nhưng vẫn giữ độ bền cao, kết hợp màn hình Super Retina XDR 6.7 inch ProMotion 120Hz sắc nét.</p>
            <p>Chip A17 Pro cùng GPU 6 nhân mang lại hiệu năng chơi game và dựng hình mạnh mẽ, trong khi ống kính Telephoto tetraprism cho zoom quang học 5x - lần đầu xuất hiện trên iPhone.</p>
            <p>Máy được phân loại tình trạng minh bạch (99%/98%), test pin và các cảm biến trước khi lên kệ, bảo hành dài hạn kèm hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Super Retina XDR OLED 6.7", ProMotion 120Hz, Dynamic Island</li>
            <li><strong>Chip:</strong> Apple A17 Pro, GPU 6 nhân</li>
            <li><strong>Camera sau:</strong> Chính 48MP + Ultra Wide 12MP + Telephoto 12MP (tetraprism, zoom quang 5x)</li>
            <li><strong>Camera trước:</strong> TrueDepth 12MP</li>
            <li><strong>Pin &amp; sạc:</strong> Cả ngày sử dụng, sạc nhanh 20W, MagSafe, cổng USB-C</li>
            <li><strong>Khác:</strong> Khung Titan, chống nước IP68, nút Action Button</li>
            </ul>
            """),

        ["iphone-17-pro-max-256gb"] = new ProductContent(
            "iPhone 17 Pro Max - thế hệ mới nhất, thiết kế và camera nâng cấp toàn diện. Liên hệ để nhận giá tốt nhất và ưu đãi đặt trước.",
            """
            <p><strong>iPhone 17 Pro Max</strong> là sản phẩm mới nhất trong dòng iPhone Pro, được nâng cấp toàn diện về thiết kế, hiệu năng và hệ thống camera so với thế hệ trước. Đây là model đang được Apple Store Uyên NTT ưu tiên nhập hàng sớm.</p>
            <p>Do mới ra mắt, thông tin cấu hình chi tiết và giá bán sẽ được cập nhật liên tục theo nguồn hàng chính hãng. Quý khách quan tâm vui lòng để lại thông tin hoặc gọi hotline để được tư vấn giá tốt nhất và giữ chỗ đặt trước.</p>
            <p>Khách đặt trước tại Apple Store Uyên NTT được ưu tiên nhận máy sớm, hỗ trợ trả góp 0% và trợ giá thu cũ đổi mới ngay khi lên đời.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Super Retina XDR, ProMotion 120Hz, thiết kế viền mỏng hơn thế hệ trước</li>
            <li><strong>Chip:</strong> Thế hệ chip Apple mới nhất, hiệu năng AI vượt trội</li>
            <li><strong>Camera:</strong> Hệ thống camera nâng cấp toàn diện, cải thiện chụp xa và chụp đêm</li>
            <li><strong>Pin:</strong> Dung lượng lớn hơn, tối ưu thời lượng sử dụng cả ngày</li>
            <li><strong>Khác:</strong> Khung Titan, chống nước IP68, cổng USB-C</li>
            <li><em>Thông số đầy đủ sẽ được cập nhật khi có hàng chính hãng.</em></li>
            </ul>
            """),

        ["iphone-16-pro-128gb"] = new ProductContent(
            "iPhone 16 Pro 128GB - màn hình 6.3 inch nhỏ gọn, chip A18 Pro mạnh mẽ, camera Telephoto 5x.",
            """
            <p><strong>iPhone 16 Pro</strong> mang toàn bộ sức mạnh của dòng Pro vào thân máy 6.3 inch nhỏ gọn hơn, dễ cầm một tay nhưng vẫn đầy đủ camera Telephoto 5x, chip A18 Pro và khung Titan cao cấp.</p>
            <p>Phù hợp với khách hàng thích máy nhỏ gọn nhưng không muốn đánh đổi hiệu năng hay chất lượng camera so với bản Pro Max.</p>
            <p>Sản phẩm chính hãng, kiểm tra kỹ trước khi giao, bảo hành dài hạn và hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Super Retina XDR OLED 6.3", ProMotion 120Hz, Always-On</li>
            <li><strong>Chip:</strong> Apple A18 Pro</li>
            <li><strong>Camera sau:</strong> Chính 48MP + Ultra Wide 48MP + Telephoto 12MP (zoom quang 5x)</li>
            <li><strong>Pin &amp; sạc:</strong> Cả ngày sử dụng, MagSafe, USB-C</li>
            <li><strong>Khác:</strong> Khung Titan, chống nước IP68, nút Camera Control</li>
            </ul>
            """),

        ["iphone-15-pro-128gb"] = new ProductContent(
            "iPhone 15 Pro 128GB - khung Titan nhẹ bền, chip A17 Pro, camera Telephoto 3x zoom quang học.",
            """
            <p><strong>iPhone 15 Pro</strong> là phiên bản 6.1 inch nhỏ gọn của dòng Pro, dùng khung Titan nhẹ hơn thép không gỉ, trang bị chip A17 Pro và cụm camera zoom quang 3x linh hoạt cho nhu cầu chụp ảnh hàng ngày.</p>
            <p>Máy đã qua sử dụng được Apple Store Uyên NTT kiểm tra toàn diện, phân loại tình trạng minh bạch, phù hợp khách muốn trải nghiệm dòng Pro với chi phí hợp lý hơn máy mới.</p>
            <p>Đi kèm bảo hành dài hạn, hỗ trợ trả góp 0% và trợ giá thu cũ đổi mới.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Super Retina XDR OLED 6.1", ProMotion 120Hz, Dynamic Island</li>
            <li><strong>Chip:</strong> Apple A17 Pro</li>
            <li><strong>Camera sau:</strong> Chính 48MP + Ultra Wide 12MP + Telephoto 12MP (zoom quang 3x)</li>
            <li><strong>Pin &amp; sạc:</strong> Cả ngày sử dụng, MagSafe, USB-C</li>
            <li><strong>Khác:</strong> Khung Titan, chống nước IP68</li>
            </ul>
            """),

        ["iphone-15-128gb"] = new ProductContent(
            "iPhone 15 128GB - Dynamic Island, cổng USB-C, camera chính 48MP. Lựa chọn cân bằng giữa giá và trải nghiệm.",
            """
            <p><strong>iPhone 15</strong> mang Dynamic Island và cổng USB-C - hai nâng cấp được mong chờ nhất - xuống phân khúc phổ thông, cùng camera chính 48MP cho ảnh chụp chi tiết hơn hẳn thế hệ trước.</p>
            <p>Đây là mẫu bán chạy nhất tại Apple Store Uyên NTT nhờ mức giá hợp lý mà vẫn đầy đủ trải nghiệm iPhone hiện đại, phù hợp số đông người dùng.</p>
            <p>Máy được kiểm tra kỹ, bảo hành dài hạn, hỗ trợ trả góp 0% và thu cũ đổi mới.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Super Retina XDR OLED 6.1", Dynamic Island</li>
            <li><strong>Chip:</strong> Apple A16 Bionic</li>
            <li><strong>Camera sau:</strong> Chính 48MP + Ultra Wide 12MP</li>
            <li><strong>Pin &amp; sạc:</strong> Cả ngày sử dụng, MagSafe, cổng USB-C</li>
            <li><strong>Khác:</strong> Chống nước IP68, mặt kính cường lực Ceramic Shield</li>
            </ul>
            """),

        ["iphone-14-pro-max-128gb"] = new ProductContent(
            "iPhone 14 Pro Max 128GB - Dynamic Island đầu tiên, camera 48MP, màn hình luôn bật Always-On.",
            """
            <p><strong>iPhone 14 Pro Max</strong> là chiếc iPhone đầu tiên giới thiệu Dynamic Island và camera chính 48MP, cùng màn hình Always-On hiển thị thông tin liên tục mà không cần chạm mở khóa.</p>
            <p>Sau thời gian sử dụng, máy vẫn giữ hiệu năng ổn định nhờ chip A16 Bionic mạnh mẽ, là lựa chọn tốt cho khách muốn trải nghiệm dòng Pro Max với chi phí thấp hơn đáng kể so với đời mới.</p>
            <p>Apple Store Uyên NTT kiểm tra pin, màn hình, camera kỹ trước khi bán, bảo hành dài hạn kèm trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Super Retina XDR OLED 6.7", ProMotion 120Hz, Always-On, Dynamic Island</li>
            <li><strong>Chip:</strong> Apple A16 Bionic</li>
            <li><strong>Camera sau:</strong> Chính 48MP + Ultra Wide 12MP + Telephoto 12MP (zoom quang 3x)</li>
            <li><strong>Pin &amp; sạc:</strong> Cả ngày sử dụng, MagSafe, Lightning</li>
            <li><strong>Khác:</strong> Khung thép không gỉ, chống nước IP68</li>
            </ul>
            """),

        ["iphone-13-128gb"] = new ProductContent(
            "iPhone 13 128GB - hiệu năng bền bỉ với A15 Bionic, camera kép cải tiến, giá cực tốt cho nhu cầu phổ thông.",
            """
            <p><strong>iPhone 13</strong> vẫn là lựa chọn được yêu thích nhờ chip A15 Bionic mạnh mẽ, camera kép 12MP cải thiện đáng kể khả năng chụp thiếu sáng so với thế hệ trước, cùng thời lượng pin cải thiện rõ rệt.</p>
            <p>Với mức giá phải chăng, đây là chiếc iPhone lý tưởng cho học sinh, sinh viên hoặc người dùng cần một chiếc máy chính hãng ổn định, dùng bền trong nhiều năm.</p>
            <p>Máy đã qua kiểm định pin và ngoại hình, bảo hành dài hạn, hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Super Retina XDR OLED 6.1", 60Hz</li>
            <li><strong>Chip:</strong> Apple A15 Bionic</li>
            <li><strong>Camera sau:</strong> Kép 12MP (Wide + Ultra Wide)</li>
            <li><strong>Pin &amp; sạc:</strong> Cải thiện so với iPhone 12, MagSafe, Lightning</li>
            <li><strong>Khác:</strong> Chống nước IP68, Face ID</li>
            </ul>
            """),

        ["iphone-11-64gb"] = new ProductContent(
            "iPhone 11 64GB - máy nhập môn giá tốt, camera kép, chip A13 Bionic vẫn đủ mượt cho nhu cầu cơ bản.",
            """
            <p><strong>iPhone 11</strong> là lựa chọn tiết kiệm nhất để bắt đầu trải nghiệm hệ sinh thái Apple, với camera kép 12MP chụp ảnh tốt trong tầm giá và chip A13 Bionic vẫn đáp ứng tốt các tác vụ hàng ngày, lướt web, mạng xã hội.</p>
            <p>Phù hợp làm máy phụ, máy cho người lớn tuổi hoặc trẻ em, hay đơn giản là người dùng không đặt nặng cấu hình mạnh.</p>
            <p>Máy đã qua kiểm tra kỹ tại Apple Store Uyên NTT, bảo hành dài hạn, giá cực kỳ cạnh tranh.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Liquid Retina LCD 6.1"</li>
            <li><strong>Chip:</strong> Apple A13 Bionic</li>
            <li><strong>Camera sau:</strong> Kép 12MP (Wide + Ultra Wide)</li>
            <li><strong>Pin &amp; sạc:</strong> Dùng cả ngày, cổng Lightning</li>
            <li><strong>Khác:</strong> Chống nước IP68, Face ID</li>
            </ul>
            """),

        ["macbook-air-m3-15-inch-16gb-256gb"] = new ProductContent(
            "MacBook Air M3 15 inch 16GB/256GB - màn hình lớn, hiệu năng M3 mạnh mẽ, pin 18 giờ, thiết kế fanless im lặng.",
            """
            <p><strong>MacBook Air M3 15 inch</strong> mang màn hình Liquid Retina lớn 15.3 inch hiếm có trên dòng Air, phù hợp cho công việc văn phòng, học tập lẫn giải trí với không gian hiển thị rộng rãi.</p>
            <p>Chip M3 8-nhân CPU cho hiệu năng vượt trội so với M1/M2, xử lý tốt các tác vụ chỉnh sửa ảnh, dựng video nhẹ, trong khi thiết kế fanless giữ máy vận hành hoàn toàn im lặng.</p>
            <p>Cấu hình RAM 16GB rộng rãi, thoải mái cho đa nhiệm nhiều năm tới. Máy chính hãng, bảo hành dài hạn, hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Liquid Retina 15.3", 500 nits, dải màu rộng P3</li>
            <li><strong>Chip:</strong> Apple M3, 8-nhân CPU / 10-nhân GPU</li>
            <li><strong>RAM &amp; lưu trữ:</strong> 16GB hợp nhất / 256GB SSD</li>
            <li><strong>Pin:</strong> Lên đến 18 giờ sử dụng, sạc nhanh MagSafe 3</li>
            <li><strong>Kết nối:</strong> 2 cổng Thunderbolt/USB4, Wi-Fi 6E</li>
            <li><strong>Khác:</strong> Thiết kế fanless không quạt, hoàn toàn im lặng</li>
            </ul>
            """),

        ["macbook-air-m1-13-inch-8gb-256gb-2020"] = new ProductContent(
            "MacBook Air M1 13 inch 2020 - chiếc Mac đầu tiên dùng chip Apple Silicon, bền bỉ, pin trâu, giá hợp lý.",
            """
            <p><strong>MacBook Air M1</strong> là cột mốc quan trọng khi Apple chuyển từ chip Intel sang Apple Silicon, mang lại bước nhảy vọt về hiệu năng trên watt và thời lượng pin so với các thế hệ Air trước đó.</p>
            <p>Dù ra mắt từ 2020, chip M1 vẫn đủ mượt cho công việc văn phòng, lướt web, chỉnh ảnh cơ bản nhờ tối ưu phần mềm tốt của Apple, là lựa chọn tiết kiệm để trải nghiệm hệ sinh thái macOS.</p>
            <p>Máy đã qua sử dụng được kiểm tra pin, bàn phím, màn hình kỹ lưỡng, phân loại tình trạng minh bạch, bảo hành dài hạn.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Retina 13.3", 400 nits</li>
            <li><strong>Chip:</strong> Apple M1, 8-nhân CPU / 7-nhân GPU</li>
            <li><strong>RAM &amp; lưu trữ:</strong> 8GB hợp nhất / 256GB SSD</li>
            <li><strong>Pin:</strong> Lên đến 18 giờ sử dụng</li>
            <li><strong>Khác:</strong> Thiết kế fanless, Touch ID, 2 cổng Thunderbolt/USB4</li>
            </ul>
            """),

        ["macbook-pro-m4-14-inch-16gb-512gb"] = new ProductContent(
            "MacBook Pro M4 14 inch 16GB/512GB - màn hình Liquid Retina XDR, hiệu năng M4 đỉnh cao cho dân sáng tạo nội dung.",
            """
            <p><strong>MacBook Pro M4 14 inch</strong> là lựa chọn cho người dùng chuyên nghiệp cần hiệu năng mạnh: dựng video 4K, lập trình, thiết kế đồ họa - nhờ chip M4 thế hệ mới cùng màn hình Liquid Retina XDR độ tương phản cao, hỗ trợ ProMotion 120Hz mượt mà.</p>
            <p>Hệ thống cổng kết nối đầy đủ (Thunderbolt 4, HDMI, khe thẻ SDXC) giúp làm việc không cần thêm hub chuyển đổi, tản nhiệt chủ động cho phép duy trì hiệu năng cao trong thời gian dài.</p>
            <p>Máy chính hãng nguyên seal, bảo hành dài hạn, hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Liquid Retina XDR 14.2", ProMotion 120Hz, 1000 nits (1600 nits HDR)</li>
            <li><strong>Chip:</strong> Apple M4</li>
            <li><strong>RAM &amp; lưu trữ:</strong> 16GB hợp nhất / 512GB SSD</li>
            <li><strong>Pin:</strong> Lên đến 18 giờ sử dụng</li>
            <li><strong>Kết nối:</strong> 3 cổng Thunderbolt 4, HDMI, khe thẻ SDXC, MagSafe 3</li>
            <li><strong>Khác:</strong> Camera FaceTime HD 12MP, loa 6 củ hỗ trợ Spatial Audio</li>
            </ul>
            """),

        ["ipad-pro-m4-11-inch-wifi-256gb"] = new ProductContent(
            "iPad Pro M4 11 inch WiFi 256GB - màn hình Ultra Retina XDR OLED siêu mỏng, hiệu năng đỉnh cao cho công việc sáng tạo.",
            """
            <p><strong>iPad Pro M4</strong> là chiếc iPad mỏng nhẹ nhất từ trước đến nay, trang bị màn hình Ultra Retina XDR công nghệ OLED xếp lớp (tandem) cho độ tương phản và độ sáng vượt trội, lý tưởng để vẽ, dựng phim, chỉnh ảnh chuyên nghiệp.</p>
            <p>Chip M4 mang hiệu năng ngang ngửa laptop cao cấp, kết hợp Apple Pencil Pro với các cử chỉ mới giúp trải nghiệm sáng tạo tự nhiên hơn.</p>
            <p>Máy chính hãng, kiểm tra kỹ, bảo hành dài hạn, hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Ultra Retina XDR OLED 11", ProMotion 120Hz</li>
            <li><strong>Chip:</strong> Apple M4</li>
            <li><strong>Bộ nhớ:</strong> 256GB, kết nối Wi-Fi</li>
            <li><strong>Camera:</strong> 12MP sau, TrueDepth 12MP trước</li>
            <li><strong>Khác:</strong> Hỗ trợ Apple Pencil Pro, Magic Keyboard, cổng Thunderbolt/USB4</li>
            </ul>
            """),

        ["ipad-air-m2-11-inch-wifi-128gb"] = new ProductContent(
            "iPad Air M2 11 inch WiFi 128GB - hiệu năng M2 mạnh mẽ trong thân máy mỏng nhẹ, cân bằng giữa giá và trải nghiệm.",
            """
            <p><strong>iPad Air M2</strong> mang hiệu năng chip M2 từng có trên dòng Pro xuống phân khúc phổ thông hơn, đủ sức xử lý các tác vụ đa nhiệm, chỉnh sửa ảnh/video nhẹ nhàng và chơi game đồ họa cao.</p>
            <p>Máy hỗ trợ Apple Pencil Pro và Magic Keyboard, phù hợp cho học tập, làm việc di động mà không cần đầu tư vào dòng Pro đắt tiền hơn.</p>
            <p>Sản phẩm chính hãng, bảo hành dài hạn, hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Liquid Retina 11", dải màu rộng P3</li>
            <li><strong>Chip:</strong> Apple M2</li>
            <li><strong>Bộ nhớ:</strong> 128GB, kết nối Wi-Fi</li>
            <li><strong>Camera:</strong> 12MP sau, 12MP trước (Ultra Wide, hỗ trợ Center Stage)</li>
            <li><strong>Khác:</strong> Touch ID tích hợp nút nguồn, hỗ trợ Apple Pencil Pro</li>
            </ul>
            """),

        ["ipad-gen-10-wifi-64gb"] = new ProductContent(
            "iPad Gen 10 WiFi 64GB - thiết kế phẳng cạnh hiện đại, cổng USB-C, phù hợp học tập và giải trí hàng ngày.",
            """
            <p><strong>iPad thế hệ 10</strong> làm mới hoàn toàn thiết kế với viền phẳng, màu sắc trẻ trung và cổng sạc USB-C thay cho Lightning, mang lại trải nghiệm hiện đại hơn hẳn các đời iPad phổ thông trước đó.</p>
            <p>Với mức giá dễ tiếp cận, đây là lựa chọn phù hợp cho học sinh, sinh viên cần máy tính bảng học tập, xem phim, đọc sách hay lướt web mượt mà.</p>
            <p>Máy chính hãng, kiểm tra kỹ, bảo hành dài hạn, hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Liquid Retina 10.9"</li>
            <li><strong>Chip:</strong> Apple A14 Bionic</li>
            <li><strong>Bộ nhớ:</strong> 64GB, kết nối Wi-Fi</li>
            <li><strong>Camera:</strong> 12MP sau, 12MP trước Ultra Wide (Center Stage)</li>
            <li><strong>Khác:</strong> Cổng USB-C, hỗ trợ Apple Pencil (thế hệ 1 qua adapter)</li>
            </ul>
            """),

        ["ipad-mini-7-wifi-128gb"] = new ProductContent(
            "iPad Mini 7 WiFi 128GB - nhỏ gọn bỏ túi, chip A17 Pro mạnh mẽ, hỗ trợ Apple Pencil Pro.",
            """
            <p><strong>iPad Mini 7</strong> giữ thân hình nhỏ gọn 8.3 inch dễ mang theo nhưng được nâng cấp mạnh về hiệu năng với chip A17 Pro, đủ sức xử lý các ứng dụng nặng và game đồ họa cao.</p>
            <p>Lần đầu tiên iPad Mini hỗ trợ Apple Pencil Pro, phù hợp cho việc ghi chú nhanh, vẽ phác thảo hay đọc sách, xem phim khi di chuyển.</p>
            <p>Sản phẩm chính hãng, bảo hành dài hạn, hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> Liquid Retina 8.3"</li>
            <li><strong>Chip:</strong> Apple A17 Pro</li>
            <li><strong>Bộ nhớ:</strong> 128GB, kết nối Wi-Fi</li>
            <li><strong>Camera:</strong> 12MP sau, 12MP trước</li>
            <li><strong>Khác:</strong> Hỗ trợ Apple Pencil Pro, cổng USB-C</li>
            </ul>
            """),

        ["apple-watch-series-10-46mm-gps"] = new ProductContent(
            "Apple Watch Series 10 46mm GPS - màn hình lớn hơn, mỏng hơn, theo dõi sức khỏe toàn diện.",
            """
            <p><strong>Apple Watch Series 10</strong> có thiết kế mỏng nhất từ trước đến nay, màn hình OLED lớn hơn nhìn rõ ở mọi góc độ, cùng loạt tính năng theo dõi sức khỏe như đo nồng độ oxy trong máu, điện tâm đồ ECG và phát hiện ngưng thở khi ngủ.</p>
            <p>Phù hợp cho người tập luyện thể thao hàng ngày lẫn nhu cầu theo dõi sức khỏe chủ động, đồng bộ mượt mà với iPhone.</p>
            <p>Sản phẩm chính hãng, bảo hành dài hạn, hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> LTPO OLED luôn bật (Always-On), lớn hơn và mỏng hơn thế hệ trước</li>
            <li><strong>Chip:</strong> Apple S10</li>
            <li><strong>Sức khỏe:</strong> Đo oxy máu, điện tâm đồ ECG, phát hiện ngưng thở khi ngủ, đo nhiệt độ</li>
            <li><strong>Kết nối:</strong> GPS, Wi-Fi, Bluetooth 5.3</li>
            <li><strong>Khác:</strong> Chống nước 50m, sạc nhanh, dây đeo tương thích rộng</li>
            </ul>
            """),

        ["apple-watch-ultra-2-49mm"] = new ProductContent(
            "Apple Watch Ultra 2 49mm - màn hình sáng nhất, pin bền nhất, dành cho thể thao mạo hiểm và lặn biển.",
            """
            <p><strong>Apple Watch Ultra 2</strong> là chiếc đồng hồ bền bỉ nhất của Apple, khung Titan chống va đập, màn hình sáng tới 3000 nits nhìn rõ ngay cả dưới ánh nắng gắt, thời lượng pin lên đến 36 giờ cho các chuyến đi dài ngày.</p>
            <p>Trang bị GPS kép độ chính xác cao và khả năng chịu độ sâu lặn tới 100m, đây là lựa chọn hàng đầu cho người chơi thể thao mạo hiểm, chạy trail, lặn biển hoặc leo núi.</p>
            <p>Sản phẩm chính hãng, bảo hành dài hạn, hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Màn hình:</strong> LTPO OLED, độ sáng tối đa 3000 nits, kính Sapphire</li>
            <li><strong>Chip:</strong> Apple S9</li>
            <li><strong>Pin:</strong> Lên đến 36 giờ sử dụng bình thường, tới 72 giờ ở chế độ tiết kiệm pin</li>
            <li><strong>Kết nối:</strong> GPS kép chính xác cao, Wi-Fi, Bluetooth 5.3, Cellular (tùy bản)</li>
            <li><strong>Khác:</strong> Khung Titan, chống nước đến 100m, nút Action tùy chỉnh</li>
            </ul>
            """),

        ["airpods-pro-2-usb-c"] = new ProductContent(
            "AirPods Pro 2 (USB-C) - chống ồn chủ động thế hệ mới, âm thanh không gian, sạc nhanh qua USB-C.",
            """
            <p><strong>AirPods Pro 2</strong> bản USB-C mang chống ồn chủ động (ANC) mạnh gấp đôi thế hệ trước, chế độ Adaptive Audio tự động điều chỉnh âm lượng và tiếng ồn xung quanh theo thời gian thực.</p>
            <p>Âm thanh không gian cá nhân hóa (Personalized Spatial Audio) mang lại trải nghiệm nghe nhạc, xem phim sống động như rạp hát, trong khi hộp sạc hỗ trợ USB-C tiện lợi hơn khi dùng chung dây với iPhone, iPad, MacBook đời mới.</p>
            <p>Sản phẩm chính hãng, bảo hành dài hạn, hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Chống ồn:</strong> Chủ động (ANC) thế hệ 2, chế độ Xuyên âm thích ứng</li>
            <li><strong>Âm thanh:</strong> Không gian cá nhân hóa, tự động điều chỉnh theo chuyển động đầu</li>
            <li><strong>Chống nước:</strong> IP54 (tai nghe và hộp sạc)</li>
            <li><strong>Pin:</strong> Lên đến 6 giờ nghe nhạc (ANC bật), 30 giờ kèm hộp sạc</li>
            <li><strong>Khác:</strong> Hộp sạc USB-C, loa báo định vị, cảm ứng lực nhấn điều khiển</li>
            </ul>
            """),

        ["airpods-4"] = new ProductContent(
            "AirPods 4 - thiết kế hoàn toàn mới, âm thanh cải thiện rõ rệt, thoải mái đeo cả ngày.",
            """
            <p><strong>AirPods 4</strong> được thiết kế lại từ đầu với dáng tai nghe ôm sát hơn, mang lại cảm giác thoải mái hơn khi đeo trong thời gian dài, đồng thời cải thiện chất lượng âm thanh và khả năng thu giọng khi gọi điện.</p>
            <p>Là lựa chọn hợp lý cho người dùng cần một chiếc tai nghe true wireless gọn nhẹ, dễ dùng, tương thích tốt với hệ sinh thái Apple.</p>
            <p>Sản phẩm chính hãng, bảo hành dài hạn, hỗ trợ trả góp 0%.</p>
            """,
            """
            <ul>
            <li><strong>Thiết kế:</strong> Kiểu dáng mới, ôm tai thoải mái hơn thế hệ trước</li>
            <li><strong>Âm thanh:</strong> Driver mới, khử tiếng ồn khi đàm thoại</li>
            <li><strong>Pin:</strong> Lên đến 5 giờ nghe nhạc, 30 giờ kèm hộp sạc</li>
            <li><strong>Kết nối:</strong> Bluetooth 5.3, ghép nối nhanh với thiết bị Apple</li>
            <li><strong>Khác:</strong> Cảm ứng điều khiển trên thân tai nghe, hộp sạc USB-C</li>
            </ul>
            """),

        ["iphone-12-64gb-thanh-ly"] = new ProductContent(
            "iPhone 12 64GB - hàng thanh lý 97%, đã kiểm tra kỹ pin và ngoại hình, giá cực tốt cho người dùng tiết kiệm.",
            """
            <p><strong>iPhone 12 64GB</strong> hàng thanh lý được Apple Store Uyên NTT kiểm tra kỹ về pin, màn hình và các cảm biến trước khi lên kệ, ngoại hình đạt tình trạng 97% - vết xước rất nhỏ khó nhận thấy khi sử dụng bình thường.</p>
            <p>Máy vẫn hỗ trợ MagSafe, camera kép chụp ảnh tốt và chip A14 Bionic đủ mượt cho các nhu cầu hàng ngày, là lựa chọn tiết kiệm nếu bạn muốn dùng iPhone màn OLED với chi phí thấp.</p>
            <p>Hàng thanh lý có bảo hành ngắn hạn tại cửa hàng, hỗ trợ đổi trả trong thời gian quy định nếu phát sinh lỗi kỹ thuật.</p>
            """,
            """
            <ul>
            <li><strong>Tình trạng:</strong> Thanh lý 97%, đã kiểm tra pin &amp; ngoại hình</li>
            <li><strong>Màn hình:</strong> Super Retina XDR OLED 6.1"</li>
            <li><strong>Chip:</strong> Apple A14 Bionic</li>
            <li><strong>Camera sau:</strong> Kép 12MP (Wide + Ultra Wide)</li>
            <li><strong>Khác:</strong> Hỗ trợ MagSafe, chống nước IP68, cổng Lightning</li>
            </ul>
            """),

        ["macbook-air-m1-thanh-ly"] = new ProductContent(
            "MacBook Air M1 - hàng thanh lý 96%, pin đã kiểm tra dung lượng, hiệu năng M1 vẫn mượt cho công việc văn phòng.",
            """
            <p><strong>MacBook Air M1</strong> hàng thanh lý được kiểm tra kỹ tình trạng pin (dung lượng pin còn lại được đo và công bố), bàn phím, bản lề và ngoại hình trước khi bán, đạt tình trạng thẩm mỹ 96%.</p>
            <p>Hiệu năng chip M1 vẫn đáp ứng tốt các tác vụ văn phòng, học tập, lướt web mượt mà nhờ tối ưu phần cứng - phần mềm của Apple, là lựa chọn tiết kiệm để sở hữu MacBook chính hãng.</p>
            <p>Hàng thanh lý có bảo hành ngắn hạn tại cửa hàng, hỗ trợ đổi trả nếu phát sinh lỗi kỹ thuật trong thời gian quy định.</p>
            """,
            """
            <ul>
            <li><strong>Tình trạng:</strong> Thanh lý 96%, đã đo dung lượng pin còn lại</li>
            <li><strong>Màn hình:</strong> Retina 13.3"</li>
            <li><strong>Chip:</strong> Apple M1, 8-nhân CPU / 7-nhân GPU</li>
            <li><strong>RAM &amp; lưu trữ:</strong> 8GB hợp nhất / 256GB SSD</li>
            <li><strong>Khác:</strong> Thiết kế fanless, Touch ID, 2 cổng Thunderbolt/USB4</li>
            </ul>
            """),
    };

    // Tin công nghệ — 8 bài viết đầy đủ (tự viết, phong cách blog công nghệ, không phải tin thật).
    private static readonly Dictionary<string, NewsContent> NewsContentBySlug = new()
    {
        ["iphone-17-pro-max-lo-dien-thiet-ke-moi"] = new NewsContent(
            "Loạt thông tin rò rỉ về iPhone 17 Pro Max hé lộ thiết kế mới cùng hệ thống camera được nâng cấp toàn diện, hứa hẹn tạo nên cú hích lớn cho dòng Pro Max.",
            """
            <p>Chỉ còn vài tháng nữa là đến sự kiện ra mắt iPhone thường niên của Apple, và những thông tin rò rỉ đầu tiên về <strong>iPhone 17 Pro Max</strong> đã bắt đầu xuất hiện dày đặc trên các diễn đàn công nghệ. Theo nhiều nguồn tin từ chuỗi cung ứng, thế hệ Pro Max năm nay sẽ có thay đổi thiết kế đáng chú ý nhất trong nhiều năm trở lại đây.</p>
            <h3>Thiết kế được làm mới</h3>
            <p>Khác với những nâng cấp nhỏ giọt ở các thế hệ gần đây, iPhone 17 Pro Max được đồn đoán sẽ có cụm camera sau bố trí lại hoàn toàn, viền màn hình mỏng hơn nhờ công nghệ đóng gói mới, cùng vật liệu khung máy được tinh chỉnh để cân bằng giữa độ bền và trọng lượng.</p>
            <h3>Camera là trọng tâm nâng cấp</h3>
            <p>Nguồn tin từ các nhà phân tích chuỗi cung ứng cho biết Apple sẽ đầu tư mạnh vào cảm biến camera chính với độ phân giải cao hơn, cải thiện đáng kể khả năng chụp thiếu sáng và quay video ở độ phân giải cao. Ống kính Telephoto cũng được cho là sẽ có bước nhảy vọt về khả năng zoom quang học.</p>
            <h3>Hiệu năng và thời điểm ra mắt</h3>
            <p>Về hiệu năng, chip xử lý thế hệ mới được kỳ vọng mang lại cải thiện lớn cho các tác vụ trí tuệ nhân tạo trên thiết bị. Theo lịch trình thường niên, Apple nhiều khả năng sẽ công bố sản phẩm vào tháng 9, và người dùng tại Việt Nam có thể đặt mua ngay trong đợt mở bán đầu tiên.</p>
            <p><em>Apple Store Uyên NTT sẽ cập nhật thông tin giá bán và cấu hình chính thức ngay khi Apple công bố. Khách quan tâm có thể để lại thông tin đặt trước để được ưu tiên nhận máy sớm.</em></p>
            """),

        ["apple-phat-hanh-ban-cap-nhat-ios-moi"] = new NewsContent(
            "Apple vừa phát hành bản cập nhật iOS mới khắc phục nhiều lỗi bảo mật quan trọng, người dùng iPhone được khuyến nghị cập nhật ngay để bảo vệ thiết bị.",
            """
            <p>Apple vừa chính thức phát hành bản cập nhật hệ điều hành iOS mới, tập trung khắc phục một số lỗ hổng bảo mật được đánh giá ở mức nghiêm trọng. Đây là bản cập nhật mà người dùng iPhone nên cài đặt sớm để đảm bảo an toàn dữ liệu cá nhân.</p>
            <h3>Những gì bản cập nhật khắc phục</h3>
            <ul>
            <li>Vá lỗi cho phép ứng dụng độc hại truy cập trái phép vào dữ liệu hệ thống.</li>
            <li>Khắc phục lỗ hổng liên quan đến xử lý hình ảnh có thể bị khai thác từ xa.</li>
            <li>Cải thiện cơ chế xác thực khi đồng bộ dữ liệu qua iCloud.</li>
            <li>Sửa một số lỗi vặt ảnh hưởng đến thời lượng pin và kết nối Wi-Fi.</li>
            </ul>
            <h3>Vì sao nên cập nhật ngay</h3>
            <p>Các lỗ hổng bảo mật, đặc biệt là những lỗi có thể bị khai thác từ xa mà không cần người dùng tương tác, luôn tiềm ẩn rủi ro cao. Việc trì hoãn cập nhật đồng nghĩa với việc thiết bị của bạn vẫn tồn tại lỗ hổng mà tin tặc có thể lợi dụng.</p>
            <h3>Cách cập nhật</h3>
            <p>Người dùng có thể vào <strong>Cài đặt → Cài đặt chung → Cập nhật phần mềm</strong> để kiểm tra và tải bản cập nhật mới nhất. Nên sao lưu dữ liệu qua iCloud hoặc máy tính trước khi cập nhật, đồng thời đảm bảo pin còn trên 50% hoặc đang cắm sạc trong quá trình cài đặt.</p>
            <p>Nếu máy có dấu hiệu giật lag sau khi cập nhật, người dùng có thể mang đến các trung tâm bảo hành/sửa chữa uy tín để được kiểm tra và tối ưu lại thiết bị.</p>
            """),

        ["macbook-air-m4-co-gi-moi-so-sanh-m3"] = new NewsContent(
            "MacBook Air chuyển sang chip M4 với hiệu năng và thời lượng pin được cải thiện. Cùng so sánh nhanh những khác biệt so với thế hệ M3 trước đó.",
            """
            <p>Sau khi dòng MacBook Pro và iPad Pro đã lên đời chip M4, đến lượt MacBook Air được đồn đoán sẽ chuyển sang thế hệ chip mới này trong đợt cập nhật sắp tới, thay thế cho chip M3 hiện tại.</p>
            <h3>Hiệu năng CPU và GPU</h3>
            <p>Chip M4 được kỳ vọng mang lại mức cải thiện hiệu năng CPU đáng kể nhờ tiến trình sản xuất mới, đồng thời GPU cũng được nâng cấp giúp xử lý tốt hơn các tác vụ đồ họa, dựng video nhẹ hay chơi game có yêu cầu cấu hình vừa phải.</p>
            <h3>Thời lượng pin</h3>
            <p>Một trong những điểm mạnh xuyên suốt của dòng MacBook Air là thời lượng pin, và với M4 tối ưu hơn về tiêu thụ điện năng, người dùng có thể kỳ vọng thời gian sử dụng thực tế được kéo dài thêm so với thế hệ M3.</p>
            <h3>Bảng so sánh nhanh</h3>
            <table>
            <thead><tr><th>Tiêu chí</th><th>MacBook Air M3</th><th>MacBook Air M4 (dự kiến)</th></tr></thead>
            <tbody>
            <tr><td>Hiệu năng CPU</td><td>Tốt</td><td>Cải thiện rõ rệt</td></tr>
            <tr><td>Hiệu năng GPU</td><td>Tốt</td><td>Cao hơn</td></tr>
            <tr><td>Thời lượng pin</td><td>Lên đến 18 giờ</td><td>Kỳ vọng dài hơn</td></tr>
            <tr><td>Hỗ trợ AI trên máy</td><td>Có</td><td>Tối ưu hơn</td></tr>
            </tbody>
            </table>
            <h3>Có nên chờ M4 hay mua M3 ngay?</h3>
            <p>Với nhu cầu văn phòng, học tập thông thường, MacBook Air M3 hiện tại vẫn đáp ứng rất tốt và đang có mức giá dễ tiếp cận hơn. Nếu công việc của bạn liên quan nhiều đến xử lý đồ họa hoặc muốn dùng máy lâu dài trong nhiều năm tới, chờ đợi thế hệ M4 có thể là lựa chọn đáng cân nhắc.</p>
            """),

        ["5-meo-tiet-kiem-pin-iphone"] = new NewsContent(
            "Pin iPhone tụt nhanh khiến bạn khó chịu? Đây là 5 mẹo đơn giản giúp kéo dài thời lượng sử dụng pin mà ai cũng có thể áp dụng ngay.",
            """
            <p>Pin là một trong những mối quan tâm hàng đầu của người dùng iPhone, đặc biệt là với những chiếc máy đã sử dụng qua vài năm. Dưới đây là 5 mẹo đơn giản giúp bạn kéo dài thời lượng pin mỗi ngày.</p>
            <h3>1. Bật chế độ Tối ưu hóa sạc pin</h3>
            <p>Vào <strong>Cài đặt → Pin → Tình trạng pin &amp; Sạc pin</strong>, bật tùy chọn "Sạc pin được tối ưu hóa" giúp máy học thói quen sạc của bạn và giảm thời gian pin ở mức 100%, từ đó làm chậm quá trình chai pin.</p>
            <h3>2. Giảm độ sáng màn hình và bật tự động</h3>
            <p>Màn hình là bộ phận tiêu tốn pin nhiều nhất trên iPhone. Bật "Độ sáng tự động" và tắt bớt các hiệu ứng nền động sẽ giúp máy tiết kiệm pin đáng kể trong sử dụng hàng ngày.</p>
            <h3>3. Quản lý ứng dụng chạy nền</h3>
            <p>Vào <strong>Cài đặt → Cài đặt chung → Làm mới ứng dụng nền</strong> để tắt bớt các ứng dụng không cần thiết tự động cập nhật dữ liệu khi bạn không sử dụng.</p>
            <h3>4. Bật chế độ Tiết kiệm pin đúng lúc</h3>
            <p>Chế độ Low Power Mode không chỉ dùng khi pin yếu - bạn có thể bật thủ công bất cứ lúc nào để giảm hiệu năng nền, tắt tải xuống tự động và kéo dài thời gian sử dụng khi cần thiết.</p>
            <h3>5. Tránh để máy quá nóng hoặc quá lạnh</h3>
            <p>Nhiệt độ môi trường ảnh hưởng trực tiếp đến hiệu suất và tuổi thọ pin Lithium-ion. Hạn chế để máy dưới nắng gắt hoặc trong xe hơi nóng, đặc biệt là trong lúc sạc pin.</p>
            <p>Nếu đã áp dụng các mẹo trên mà pin vẫn tụt nhanh bất thường, có thể pin của máy đã chai và cần được kiểm tra, thay thế tại các trung tâm sửa chữa uy tín.</p>
            """),

        ["nen-mua-iphone-15-pro-max-cu-hay-iphone-16-moi"] = new NewsContent(
            "Cùng tầm giá gần nhau, nhiều khách hàng phân vân giữa iPhone 15 Pro Max cũ 99% và iPhone 16 mới. Bài viết so sánh giúp bạn chọn đúng máy phù hợp nhu cầu.",
            """
            <p>Đây là câu hỏi mà rất nhiều khách hàng đặt ra khi đến tư vấn tại Apple Store Uyên NTT: nên chọn <strong>iPhone 15 Pro Max cũ 99%</strong> hay <strong>iPhone 16 mới nguyên seal</strong> khi mức giá hai máy khá gần nhau? Câu trả lời phụ thuộc vào nhu cầu sử dụng cụ thể của từng người.</p>
            <h3>Về hiệu năng và camera</h3>
            <p>iPhone 15 Pro Max sở hữu chip A17 Pro cùng camera Telephoto tetraprism zoom quang 5x - vốn là tính năng cao cấp của dòng Pro Max, trong khi iPhone 16 (bản thường) dùng chip A18 nhưng camera không có ống kính zoom xa. Nếu bạn ưu tiên chụp ảnh xa, quay video chuyên nghiệp, 15 Pro Max vẫn có lợi thế.</p>
            <h3>Về độ mới và bảo hành</h3>
            <p>iPhone 16 mới nguyên seal có bảo hành chính hãng đầy đủ và không lo về pin/ngoại hình đã qua sử dụng. iPhone 15 Pro Max cũ 99%, nếu mua tại nơi uy tín có kiểm tra pin, ngoại hình minh bạch, vẫn đảm bảo trải nghiệm gần như máy mới với mức giá tốt hơn.</p>
            <h3>Gợi ý lựa chọn</h3>
            <ul>
            <li>Ưu tiên camera zoom xa, khung Titan cao cấp, chấp nhận máy đã qua sử dụng → chọn <strong>iPhone 15 Pro Max 99%</strong>.</li>
            <li>Ưu tiên máy mới nguyên seal, bảo hành dài, không quan trọng ống kính zoom xa → chọn <strong>iPhone 16</strong>.</li>
            </ul>
            <p>Dù chọn phương án nào, bạn cũng nên mua tại cửa hàng có chính sách kiểm tra máy minh bạch trước mặt khách và bảo hành rõ ràng để yên tâm sử dụng lâu dài.</p>
            """),

        ["apple-watch-theo-doi-suc-khoe-tim-mach"] = new NewsContent(
            "Từ đo nhịp tim, điện tâm đồ đến cảnh báo nhịp tim bất thường - Apple Watch đang trở thành trợ thủ đắc lực theo dõi sức khỏe tim mạch hàng ngày.",
            """
            <p>Không chỉ là một thiết bị đeo thông minh thông thường, Apple Watch ngày càng được đầu tư trở thành công cụ theo dõi sức khỏe tim mạch đáng tin cậy, với nhiều tính năng được phát triển cùng các chuyên gia y tế.</p>
            <h3>Đo nhịp tim liên tục</h3>
            <p>Cảm biến quang học ở mặt sau đồng hồ đo nhịp tim liên tục trong suốt cả ngày, giúp bạn theo dõi xu hướng nhịp tim khi nghỉ ngơi, khi vận động và trong lúc ngủ.</p>
            <h3>Điện tâm đồ ECG ngay trên cổ tay</h3>
            <p>Tính năng điện tâm đồ cho phép người dùng thực hiện đo ECG một đạo trình chỉ trong 30 giây bằng cách chạm ngón tay vào mặt kính đồng hồ, kết quả có thể xuất thành file PDF để chia sẻ với bác sĩ.</p>
            <h3>Cảnh báo nhịp tim bất thường</h3>
            <p>Apple Watch có thể phát hiện và cảnh báo khi nhịp tim quá cao hoặc quá thấp bất thường so với mức hoạt động hiện tại, cũng như cảnh báo nhịp tim không đều nghi ngờ rung nhĩ - một trong những nguyên nhân phổ biến dẫn đến đột quỵ.</p>
            <h3>Lưu ý khi sử dụng</h3>
            <p>Các tính năng theo dõi sức khỏe trên Apple Watch mang tính chất tham khảo, hỗ trợ phát hiện sớm dấu hiệu bất thường chứ không thay thế cho chẩn đoán y khoa. Nếu nhận được cảnh báo bất thường, người dùng nên đến cơ sở y tế để được thăm khám chính xác.</p>
            """),

        ["huong-dan-mua-dien-thoai-tra-gop-0-phan-tram"] = new NewsContent(
            "Mua điện thoại trả góp 0% lãi suất giúp giảm áp lực tài chính khi lên đời máy mới. Bài viết hướng dẫn thủ tục đơn giản để mua trả góp nhanh chóng.",
            """
            <p>Trả góp 0% lãi suất là hình thức mua sắm ngày càng phổ biến, giúp khách hàng sở hữu iPhone, MacBook, iPad đời mới mà không cần chuẩn bị toàn bộ số tiền ngay từ đầu. Vậy thủ tục mua trả góp đơn giản như thế nào?</p>
            <h3>Điều kiện mua trả góp</h3>
            <ul>
            <li>Có thẻ tín dụng của ngân hàng liên kết (Visa/Mastercard/JCB) còn hạn mức khả dụng, hoặc</li>
            <li>Đáp ứng điều kiện của công ty tài chính tiêu dùng (thường cần CMND/CCCD, hộ khẩu/tạm trú, chứng minh thu nhập tùy gói vay).</li>
            </ul>
            <h3>Các bước thực hiện</h3>
            <ol>
            <li><strong>Chọn sản phẩm và kỳ hạn trả góp</strong> phù hợp với khả năng chi trả hàng tháng, thường từ 3 đến 12 tháng.</li>
            <li><strong>Cung cấp giấy tờ tùy thân</strong> để nhân viên tư vấn kiểm tra điều kiện và lên hợp đồng trả góp.</li>
            <li><strong>Ký hợp đồng và thanh toán trước</strong> (nếu có) theo tỷ lệ quy định của ngân hàng/công ty tài chính.</li>
            <li><strong>Nhận máy ngay</strong> sau khi hợp đồng được duyệt, thường chỉ mất 15-30 phút với thẻ tín dụng.</li>
            </ol>
            <h3>Lưu ý để tránh phát sinh chi phí</h3>
            <p>Trả góp 0% lãi suất qua thẻ tín dụng thường miễn phí chuyển đổi nếu thanh toán đúng hạn hàng tháng, tuy nhiên một số ngân hàng có thể thu phí chuyển đổi trả góp (khoảng 2-3% giá trị đơn hàng). Khách hàng nên hỏi kỹ nhân viên tư vấn về tổng chi phí thực tế trước khi quyết định.</p>
            <p>Tại Apple Store Uyên NTT, khách hàng được hỗ trợ tư vấn trả góp 0% linh hoạt ngay tại quầy, thủ tục nhanh gọn và minh bạch về chi phí phát sinh (nếu có).</p>
            """),

        ["airpods-the-he-moi-do-nhip-tim"] = new NewsContent(
            "Tin đồn về AirPods tích hợp cảm biến đo nhịp tim đang thu hút sự chú ý lớn, mở ra hướng đi mới cho tai nghe không dây của Apple.",
            """
            <p>Bên cạnh Apple Watch, tai nghe AirPods cũng đang được đồn đoán sẽ trở thành thiết bị theo dõi sức khỏe tiếp theo trong hệ sinh thái Apple, với tin đồn tích hợp cảm biến đo nhịp tim ngay trên thân tai nghe.</p>
            <h3>Cảm biến đo nhịp tim hoạt động ra sao?</h3>
            <p>Theo các thông tin rò rỉ, Apple đang thử nghiệm cảm biến quang học tương tự công nghệ trên Apple Watch, đặt ở phần tiếp xúc với ống tai - vị trí được cho là đo nhịp tim chính xác hơn cả cổ tay nhờ gần với động mạch hơn.</p>
            <h3>Vì sao đây là bước đi hợp lý</h3>
            <ul>
            <li>AirPods được đeo liên tục trong lúc tập luyện thể thao, phù hợp để thu thập dữ liệu nhịp tim theo thời gian thực.</li>
            <li>Không cần đeo thêm Apple Watch, giảm bớt thiết bị mang theo khi vận động.</li>
            <li>Dữ liệu có thể đồng bộ trực tiếp vào ứng dụng Sức khỏe, kết hợp cùng dữ liệu từ iPhone.</li>
            </ul>
            <h3>Khi nào sẽ ra mắt?</h3>
            <p>Hiện Apple chưa xác nhận chính thức về tính năng này. Nếu đúng như tin đồn, khả năng cao tính năng đo nhịp tim sẽ xuất hiện trên các mẫu AirPods cao cấp trong tương lai gần, mở ra một hướng phát triển mới cho dòng tai nghe true wireless vốn đã rất phổ biến.</p>
            <p>Apple Store Uyên NTT sẽ cập nhật thông tin sớm nhất ngay khi Apple công bố chính thức các tính năng sức khỏe mới trên AirPods.</p>
            """),
    };

    // Bảng giá thu cũ đổi mới — nội dung dài chi tiết với bảng giá từ các model khác nhau.
    // KHÔNG mở đầu bằng <h2> trùng tiêu đề: view StaticPage.cshtml đã render Model.Title thành <h1>.
    private const string TradeInPriceTableHtml = """
        <p><strong>Nâng cấp lên thiết bị mới chưa bao giờ dễ dàng đến thế!</strong> Apple Store Uyên NTT nhận đánh giá và trợ giá thiết bị cũ của bạn ngay tại chỗ, giúp giảm đáng kể chi phí khi lên đời máy mới. Quy trình minh bạch, định giá công khai, không phí ẩn.</p>

        <h3>Tại sao nên thu cũ đổi mới tại Apple Store Uyên NTT?</h3>
        <ul>
          <li><strong>Giá công bằng:</strong> đánh giá dựa trên tình trạng thực tế máy (98%, 99%, 100%), so sánh thị trường khách quan.</li>
          <li><strong>Quy trình nhanh:</strong> kiểm tra trong 5 phút, báo giá ngay, không chờ đợi lâu.</li>
          <li><strong>Không phí tiền:</strong> khách thay ý định không mua máy mới vẫn không tính phí kiểm tra.</li>
          <li><strong>Trực tiếp tại cửa hàng:</strong> khách chứng kiến toàn bộ quá trình đánh giá, minh bạch từ đầu đến cuối.</li>
        </ul>

        <h3>Bảng giá tham khảo</h3>
        <p><em>Giá dưới đây là <strong>mức tham khảo</strong> cho dự án học tập, phụ thuộc tình trạng máy thực tế —
        những chiếc máy "chưa dùng bao giờ" (100%) hoặc có vết trầy xước đáng kể sẽ được điều chỉnh giá.</em></p>

        <h4>iPhone — Từ iPhone 11 đến iPhone 17</h4>
        <table>
          <thead>
            <tr>
              <th>Model</th>
              <th style="text-align: center;">Tình trạng 100%</th>
              <th style="text-align: center;">Tình trạng 99%</th>
              <th style="text-align: center;">Tình trạng 98%</th>
              <th>Ghi chú</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>iPhone 17 Pro Max 256GB</td>
              <td style="text-align: right;">18.500.000đ</td>
              <td style="text-align: right;">17.200.000đ</td>
              <td style="text-align: right;">15.900.000đ</td>
              <td>Máy mới nhất</td>
            </tr>
            <tr>
              <td>iPhone 17 Pro 128GB</td>
              <td style="text-align: right;">15.800.000đ</td>
              <td style="text-align: right;">14.600.000đ</td>
              <td style="text-align: right;">13.400.000đ</td>
              <td></td>
            </tr>
            <tr>
              <td>iPhone 16 Pro Max 256GB</td>
              <td style="text-align: right;">16.200.000đ</td>
              <td style="text-align: right;">15.100.000đ</td>
              <td style="text-align: right;">13.900.000đ</td>
              <td>Bán chạy</td>
            </tr>
            <tr>
              <td>iPhone 16 Pro 128GB</td>
              <td style="text-align: right;">13.900.000đ</td>
              <td style="text-align: right;">12.900.000đ</td>
              <td style="text-align: right;">11.900.000đ</td>
              <td></td>
            </tr>
            <tr>
              <td>iPhone 15 Pro Max 256GB</td>
              <td style="text-align: right;">13.500.000đ</td>
              <td style="text-align: right;">12.600.000đ</td>
              <td style="text-align: right;">11.600.000đ</td>
              <td>Giảm dần theo thế hệ</td>
            </tr>
            <tr>
              <td>iPhone 15 Pro 128GB</td>
              <td style="text-align: right;">11.200.000đ</td>
              <td style="text-align: right;">10.400.000đ</td>
              <td style="text-align: right;">9.500.000đ</td>
              <td></td>
            </tr>
            <tr>
              <td>iPhone 15 128GB</td>
              <td style="text-align: right;">9.800.000đ</td>
              <td style="text-align: right;">9.100.000đ</td>
              <td style="text-align: right;">8.300.000đ</td>
              <td>Màn hình 6.1 inch</td>
            </tr>
            <tr>
              <td>iPhone 14 Pro Max 256GB</td>
              <td style="text-align: right;">11.200.000đ</td>
              <td style="text-align: right;">10.400.000đ</td>
              <td style="text-align: right;">9.500.000đ</td>
              <td></td>
            </tr>
            <tr>
              <td>iPhone 13 128GB</td>
              <td style="text-align: right;">6.800.000đ</td>
              <td style="text-align: right;">6.200.000đ</td>
              <td style="text-align: right;">5.600.000đ</td>
              <td></td>
            </tr>
            <tr>
              <td>iPhone 12 64GB</td>
              <td style="text-align: right;">5.200.000đ</td>
              <td style="text-align: right;">4.700.000đ</td>
              <td style="text-align: right;">4.200.000đ</td>
              <td></td>
            </tr>
            <tr>
              <td>iPhone 11 64GB</td>
              <td style="text-align: right;">3.900.000đ</td>
              <td style="text-align: right;">3.500.000đ</td>
              <td style="text-align: right;">3.100.000đ</td>
              <td>Giá thấp hơn</td>
            </tr>
          </tbody>
        </table>

        <h4>MacBook — Air &amp; Pro</h4>
        <table>
          <thead>
            <tr>
              <th>Model</th>
              <th style="text-align: center;">Tình trạng 100%</th>
              <th style="text-align: center;">Tình trạng 99%</th>
              <th style="text-align: center;">Tình trạng 98%</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>MacBook Air M3 15 inch 16GB/256GB</td>
              <td style="text-align: right;">18.500.000đ</td>
              <td style="text-align: right;">17.300.000đ</td>
              <td style="text-align: right;">16.100.000đ</td>
            </tr>
            <tr>
              <td>MacBook Air M2 13 inch 8GB/256GB</td>
              <td style="text-align: right;">13.200.000đ</td>
              <td style="text-align: right;">12.300.000đ</td>
              <td style="text-align: right;">11.400.000đ</td>
            </tr>
            <tr>
              <td>MacBook Air M1 13 inch 8GB/256GB</td>
              <td style="text-align: right;">10.200.000đ</td>
              <td style="text-align: right;">9.500.000đ</td>
              <td style="text-align: right;">8.800.000đ</td>
            </tr>
            <tr>
              <td>MacBook Pro M4 14 inch 16GB/512GB</td>
              <td style="text-align: right;">22.500.000đ</td>
              <td style="text-align: right;">20.900.000đ</td>
              <td style="text-align: right;">19.500.000đ</td>
            </tr>
            <tr>
              <td>MacBook Pro M3 14 inch 8GB/512GB</td>
              <td style="text-align: right;">18.800.000đ</td>
              <td style="text-align: right;">17.500.000đ</td>
              <td style="text-align: right;">16.200.000đ</td>
            </tr>
          </tbody>
        </table>

        <h4>iPad — Pro, Air &amp; Mini</h4>
        <table>
          <thead>
            <tr>
              <th>Model</th>
              <th style="text-align: center;">Tình trạng 100%</th>
              <th style="text-align: center;">Tình trạng 99%</th>
              <th style="text-align: center;">Tình trạng 98%</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>iPad Pro 11 inch M4 WiFi 256GB</td>
              <td style="text-align: right;">15.200.000đ</td>
              <td style="text-align: right;">14.100.000đ</td>
              <td style="text-align: right;">13.000.000đ</td>
            </tr>
            <tr>
              <td>iPad Air 11 inch M2 WiFi 256GB</td>
              <td style="text-align: right;">10.500.000đ</td>
              <td style="text-align: right;">9.800.000đ</td>
              <td style="text-align: right;">9.100.000đ</td>
            </tr>
            <tr>
              <td>iPad Mini 7 WiFi 128GB</td>
              <td style="text-align: right;">7.800.000đ</td>
              <td style="text-align: right;">7.200.000đ</td>
              <td style="text-align: right;">6.600.000đ</td>
            </tr>
            <tr>
              <td>iPad 10.9 inch Gen 10 WiFi 64GB</td>
              <td style="text-align: right;">5.800.000đ</td>
              <td style="text-align: right;">5.400.000đ</td>
              <td style="text-align: right;">4.900.000đ</td>
            </tr>
          </tbody>
        </table>

        <h4>Apple Watch &amp; AirPods</h4>
        <table>
          <thead>
            <tr>
              <th>Model</th>
              <th style="text-align: center;">Tình trạng 100%</th>
              <th style="text-align: center;">Tình trạng 99%</th>
              <th style="text-align: center;">Tình trạng 98%</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Apple Watch Series 10 46mm GPS</td>
              <td style="text-align: right;">6.500.000đ</td>
              <td style="text-align: right;">6.000.000đ</td>
              <td style="text-align: right;">5.500.000đ</td>
            </tr>
            <tr>
              <td>Apple Watch Ultra 2 49mm</td>
              <td style="text-align: right;">11.500.000đ</td>
              <td style="text-align: right;">10.700.000đ</td>
              <td style="text-align: right;">9.900.000đ</td>
            </tr>
            <tr>
              <td>AirPods Pro 2 (USB-C)</td>
              <td style="text-align: right;">3.500.000đ</td>
              <td style="text-align: right;">3.200.000đ</td>
              <td style="text-align: right;">2.900.000đ</td>
            </tr>
            <tr>
              <td>AirPods 4</td>
              <td style="text-align: right;">2.200.000đ</td>
              <td style="text-align: right;">2.000.000đ</td>
              <td style="text-align: right;">1.800.000đ</td>
            </tr>
          </tbody>
        </table>

        <h3>Ví dụ: Lên đời từ iPhone 15 Pro Max lên iPhone 17 Pro Max</h3>
        <p>Bạn có chiếc <strong>iPhone 15 Pro Max tình trạng 99%</strong>, muốn lên đời lên <strong>iPhone 17 Pro Max</strong>.</p>
        <ol>
          <li>Giá gốc iPhone 17 Pro Max: <strong>37.990.000đ</strong></li>
          <li>Khách sở hữu iPhone 15 Pro Max 99%: shop trợ giá <strong>12.600.000đ</strong></li>
          <li><strong>Chi phí thực tế lên đời: 37.990.000 − 12.600.000 = 25.390.000đ</strong></li>
          <li>Có thể trả góp 0% qua thẻ tín dụng, giảm còn ~2 triệu/tháng</li>
        </ol>

        <h3>Quy trình đánh giá tại cửa hàng</h3>
        <ol>
          <li><strong>Kiểm tra ngoại hình:</strong> bề mặt kính, viền khung, kiểm tra vết trầy xước hay nứt vỡ.</li>
          <li><strong>Kiểm tra chức năng:</strong> màn hình, loa, mic, cảm biến, dung lượng pin còn lại.</li>
          <li><strong>Xác định tình trạng:</strong> 100% (chưa qua sử dụng) / 99% (dùng nhẹ, đẹp) / 98% (có vết trầy nhẹ).</li>
          <li><strong>Báo giá tại chỗ:</strong> đối chiếu bảng giá tham khảo và báo mức trợ giá cụ thể cho khách ngay lập tức.</li>
        </ol>

        <h3>Điều kiện &amp; Lưu ý</h3>
        <ul>
          <li><strong>Máy phải là hàng chính hãng Apple</strong> — không nhận máy dựng, hàng nhái, máy đã thay main.</li>
          <li><strong>Máy phải mở nguồn được, pin còn hoạt động</strong> — máy không lên nguồn hoặc pin chai nặng có mức trợ giá thấp hơn.</li>
          <li><strong>Không nhận máy vào nước nặng</strong> — vào nước dễ gây hỏng linh kiện, có thể bị từ chối hoặc giảm giá sâu.</li>
          <li><strong>Giá chốt sau khi ký xác nhận không thay đổi</strong> — khách nên kiểm tra kỹ càng ngay từ đầu để tránh tranh chấp.</li>
          <li><strong>Thanh toán bằng tiền mặt hoặc chuyển khoản</strong> — tiền trợ giá được trừ thẳng vào giá máy mới khi thanh toán.</li>
        </ul>

        <h3>Còn câu hỏi nào khác?</h3>
        <p>Liên hệ trực tiếp qua <strong>hotline 0900.000.000</strong> hoặc ghé cửa hàng để được tư vấn chi tiết.
        Chúng tôi hỗ trợ <strong>trả góp 0%</strong> lên máy mới, giúp bạn lên đời mà không cần bỏ quá nhiều tiền ngay.</p>
        """;
}
