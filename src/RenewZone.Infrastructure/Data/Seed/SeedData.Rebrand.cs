using Microsoft.EntityFrameworkCore;

namespace RenewZone.Infrastructure.Data.Seed;

/// <summary>
/// Đổi thương hiệu hiển thị "RenewZone" → "Apple Store Uyên NTT" trên DB ĐANG CHẠY.
///
/// Vì sao cần backfill mà không chỉ sửa <see cref="SeedData"/>: hàm seed gốc chỉ chạy khi bảng còn
/// trống (và <c>SeedSiteSettingsAsync</c> chỉ chèn key CÒN THIẾU — Lưu ý 6.13), nên mọi DB đã seed
/// từ trước (local + hosting) sẽ giữ nguyên tên cũ mãi mãi nếu không có hàm này.
///
/// Idempotent một cách TỰ NHIÊN: chỉ nạp những dòng còn chứa chuỗi cũ, thay xong thì lần chạy sau
/// bộ lọc không khớp nữa ⇒ không cần cột marker. Chỉ ghi khi chuỗi thực sự đổi.
///
/// Phạm vi CỐ Ý giới hạn ở nội dung hiển thị. KHÔNG đụng tới:
/// - namespace/tên project <c>RenewZone.*</c> (định danh code, người dùng không thấy);
/// - <c>Slug</c> của mọi bảng (đổi slug là gãy link đã chia sẻ + hỏng mục menu trỏ tới);
/// - URL chứa <c>renewzone</c> viết thường (vd host thật <c>renewzone.runasp.net</c>) — xem
///   <see cref="RebrandText"/>, các phép thay đều PHÂN BIỆT HOA THƯỜNG nên URL không bị chạm.
/// </summary>
public static partial class SeedData
{
    /// <summary>Chuỗi dùng để LỌC ở SQL. Collation mặc định của SQL Server không phân biệt hoa
    /// thường nên một chuỗi này bắt được cả "RENEWZONE", "renewzone".</summary>
    private const string OldBrandNeedle = "renewzone";

    /// <summary>
    /// Thay theo thứ tự DÀI TRƯỚC — nếu đổi "RenewZone" trước thì "RenewZone Demo" sẽ thành
    /// "Apple Store Uyên NTT Demo" (sót chữ "Demo"). Mọi phép thay đều phân biệt hoa thường để
    /// URL viết thường (renewzone.runasp.net) không bị đụng tới.
    /// </summary>
    private static string RebrandText(string value)
    {
        return value
            .Replace("CTY RENEWZONE DEMO", "APPLE STORE UYEN NTT", StringComparison.Ordinal)
            .Replace("RENEWZONE DEMO", "APPLE STORE UYEN NTT", StringComparison.Ordinal)
            .Replace("renewzone-demo.local", "applestoreuyenntt.local", StringComparison.Ordinal)
            .Replace("RenewZone Demo", "Apple Store Uyên NTT", StringComparison.Ordinal)
            .Replace("RenewZone Clone", "Apple Store Uyên NTT", StringComparison.Ordinal)
            .Replace("RenewZone", "Apple Store Uyên NTT", StringComparison.Ordinal)
            .Replace("RENEWZONE", "APPLE STORE UYEN NTT", StringComparison.Ordinal);
    }

    /// <summary>
    /// Đổi thương hiệu cho một trường và ghi lại qua <paramref name="setter"/>.
    /// Trả về true CHỈ KHI chuỗi thực sự khác — tránh đánh dấu entity là Modified vô ích
    /// (mỗi lần Modified là một lần <c>UpdatedAt</c> bị đổi + một vòng xóa cache thừa).
    /// Nhận <c>string?</c> nên dùng chung được cho cả cột NULL được lẫn cột NOT NULL.
    /// </summary>
    private static bool Apply(string? value, Action<string> setter)
    {
        if (string.IsNullOrEmpty(value)) return false;
        var updated = RebrandText(value);
        if (string.Equals(updated, value, StringComparison.Ordinal)) return false;
        setter(updated);
        return true;
    }

    private static async Task BackfillRebrandAsync(AppDbContext db)
    {
        var changed = false;

        // Cấu hình site — nguồn của tên shop/email/tên tài khoản NH hiển thị khắp layout.
        // IgnoreQueryFilters không cần: SiteSetting không xóa mềm (mục 4.9).
        var settings = await db.SiteSettings
            .Where(s => s.Value != null && EF.Functions.Like(s.Value, $"%{OldBrandNeedle}%"))
            .ToListAsync();
        foreach (var setting in settings)
        {
            changed |= Apply(setting.Value, v => setting.Value = v);
        }

        // Các bảng nội dung. IgnoreQueryFilters() để bản ghi trong Thùng rác cũng được đổi —
        // admin khôi phục sau này sẽ không lòi ra tên cũ.
        var products = await db.Products.IgnoreQueryFilters()
            .Where(p => EF.Functions.Like(p.Name, $"%{OldBrandNeedle}%")
                     || (p.ShortDescription != null && EF.Functions.Like(p.ShortDescription, $"%{OldBrandNeedle}%"))
                     || (p.DescriptionHtml != null && EF.Functions.Like(p.DescriptionHtml, $"%{OldBrandNeedle}%"))
                     || (p.SpecsHtml != null && EF.Functions.Like(p.SpecsHtml, $"%{OldBrandNeedle}%")))
            .ToListAsync();
        foreach (var p in products)
        {
            changed |= Apply(p.Name, v => p.Name = v);
            changed |= Apply(p.ShortDescription, v => p.ShortDescription = v);
            changed |= Apply(p.DescriptionHtml, v => p.DescriptionHtml = v);
            changed |= Apply(p.SpecsHtml, v => p.SpecsHtml = v);
        }

        var articles = await db.NewsArticles.IgnoreQueryFilters()
            .Where(a => EF.Functions.Like(a.Title, $"%{OldBrandNeedle}%")
                     || (a.Excerpt != null && EF.Functions.Like(a.Excerpt, $"%{OldBrandNeedle}%"))
                     || EF.Functions.Like(a.ContentHtml, $"%{OldBrandNeedle}%"))
            .ToListAsync();
        foreach (var a in articles)
        {
            changed |= Apply(a.Title, v => a.Title = v);
            changed |= Apply(a.Excerpt, v => a.Excerpt = v);
            changed |= Apply(a.ContentHtml, v => a.ContentHtml = v);
        }

        var pages = await db.StaticPages.IgnoreQueryFilters()
            .Where(s => EF.Functions.Like(s.Title, $"%{OldBrandNeedle}%")
                     || EF.Functions.Like(s.ContentHtml, $"%{OldBrandNeedle}%"))
            .ToListAsync();
        foreach (var s in pages)
        {
            changed |= Apply(s.Title, v => s.Title = v);
            changed |= Apply(s.ContentHtml, v => s.ContentHtml = v);
        }

        var blocks = await db.ContentBlocks.IgnoreQueryFilters()
            .Where(b => EF.Functions.Like(b.Title, $"%{OldBrandNeedle}%")
                     || (b.Subtitle != null && EF.Functions.Like(b.Subtitle, $"%{OldBrandNeedle}%")))
            .ToListAsync();
        foreach (var b in blocks)
        {
            changed |= Apply(b.Title, v => b.Title = v);
            changed |= Apply(b.Subtitle, v => b.Subtitle = v);
        }

        var testimonials = await db.Testimonials.IgnoreQueryFilters()
            .Where(t => EF.Functions.Like(t.Content, $"%{OldBrandNeedle}%")
                     || EF.Functions.Like(t.CustomerName, $"%{OldBrandNeedle}%"))
            .ToListAsync();
        foreach (var t in testimonials)
        {
            changed |= Apply(t.Content, v => t.Content = v);
            changed |= Apply(t.CustomerName, v => t.CustomerName = v);
        }

        var categories = await db.Categories.IgnoreQueryFilters()
            .Where(c => EF.Functions.Like(c.Name, $"%{OldBrandNeedle}%"))
            .ToListAsync();
        foreach (var c in categories)
        {
            changed |= Apply(c.Name, v => c.Name = v);
        }

        // Chỉ đổi CHỮ hiển thị của mục menu, KHÔNG đổi Url (đổi Url là gãy điều hướng).
        var menuItems = await db.MenuItems.IgnoreQueryFilters()
            .Where(m => EF.Functions.Like(m.Text, $"%{OldBrandNeedle}%"))
            .ToListAsync();
        foreach (var m in menuItems)
        {
            changed |= Apply(m.Text, v => m.Text = v);
        }

        if (changed)
        {
            // Đi qua SaveChanges (không phải ExecuteUpdate) nên CacheInvalidator tự xóa cache
            // Home/Slugs/SiteContent — web khách hiện tên mới ngay, không phải đợi hết TTL 5 phút.
            await db.SaveChangesAsync();
        }
    }
}
