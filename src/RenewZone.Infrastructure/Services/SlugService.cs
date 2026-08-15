using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>
/// Xem <see cref="ISlugService"/>. KHÔNG cache: slug được kiểm ngay lúc admin đang gõ form,
/// đọc dữ liệu cũ ở đây sẽ cho phép lưu trùng rồi vỡ ở tầng DB với thông báo khó hiểu.
/// </summary>
public partial class SlugService(AppDbContext db) : ISlugService
{
    /// <summary>Số hậu tố tối đa khi tự tìm slug rảnh — quá ngưỡng này thì gắn chuỗi ngẫu nhiên.</summary>
    private const int MaxSuffixAttempts = 100;

    /// <summary>
    /// Các đoạn đường dẫn ĐÃ thuộc về route cố định của website. Route tĩnh (literal) luôn được
    /// ưu tiên hơn fallback <c>{slug}</c> (CLAUDE.md mục 4.3), nên bản ghi mang slug này lưu thì
    /// vẫn lưu được mà mở ra thì ra trang khác — hỏng âm thầm, admin không thể tự đoán vì sao.
    /// <para>
    /// ⚠️ Thêm route 1 đoạn mới ở tầng Web thì PHẢI thêm một dòng vào đây.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        // Khu quản trị + hạ tầng
        "admin", "loi", "sitemap.xml", "robots.txt",
        // Catalog & tìm kiếm (Giai đoạn 3)
        "san-pham", "tim-kiem",
        // Mua hàng (Giai đoạn 4)
        "gio-hang", "dat-hang", "dat-hang-thanh-cong",
        // Nội dung (Giai đoạn 5)
        "tin-tuc", "lien-he",
        // Đánh giá (Giai đoạn 8)
        "danh-gia",
        // Tài khoản khách hàng (Giai đoạn 9)
        "dang-nhap", "dang-ky", "dang-xuat", "tai-khoan"
    };

    public string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // đ/Đ KHÔNG phải chữ d + dấu (không tách được bằng FormD) ⇒ phải map tay TRƯỚC khi bỏ dấu
        var text = input.Replace('đ', 'd').Replace('Đ', 'D');

        // Tách chữ cái và dấu thanh thành 2 ký tự riêng rồi bỏ hết dấu: "ế" → "e" + dấu → "e"
        text = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        text = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        text = NonSlugChars().Replace(text, "-"); // mọi thứ không phải a-z0-9 thành gạch nối
        text = MultipleDashes().Replace(text, "-");

        return text.Trim('-');
    }

    public async Task<SlugCheckResult> CheckAsync(
        string slug, SlugTargetType? excludeType = null, int? excludeId = null)
    {
        slug = (slug ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(slug))
        {
            return SlugCheckResult.Available(); // rỗng để [Required] của ViewModel bắt, không phải việc ở đây
        }

        // Kiểm TRƯỚC 4 bảng: đây là loại trùng mà không query nào phát hiện được (không bảng nào
        // giữ slug đó cả) nhưng lại hỏng nặng nhất — trang lưu xong vĩnh viễn không mở ra được.
        if (ReservedSlugs.Contains(slug))
        {
            return SlugCheckResult.Reserved();
        }

        // IgnoreQueryFilters: bản ghi trong Thùng rác vẫn giữ chỗ trong unique index của DB.
        // Bỏ qua chúng ở đây ⇒ admin được báo "slug hợp lệ" rồi bấm Lưu là dính lỗi DB thô.
        var category = await db.Categories.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.Slug == slug)
            .Select(c => new { c.Id, c.Name, c.IsDeleted })
            .FirstOrDefaultAsync();

        if (category is not null && !IsSelf(SlugTargetType.Category, category.Id))
        {
            return new SlugCheckResult(false, SlugTargetType.Category, category.Name, category.IsDeleted);
        }

        var product = await db.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.Slug == slug)
            .Select(p => new { p.Id, p.Name, p.IsDeleted })
            .FirstOrDefaultAsync();

        if (product is not null && !IsSelf(SlugTargetType.Product, product.Id))
        {
            return new SlugCheckResult(false, SlugTargetType.Product, product.Name, product.IsDeleted);
        }

        var article = await db.NewsArticles.IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.Slug == slug)
            .Select(a => new { a.Id, Name = a.Title, a.IsDeleted })
            .FirstOrDefaultAsync();

        if (article is not null && !IsSelf(SlugTargetType.NewsArticle, article.Id))
        {
            return new SlugCheckResult(false, SlugTargetType.NewsArticle, article.Name, article.IsDeleted);
        }

        var page = await db.StaticPages.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.Slug == slug)
            .Select(s => new { s.Id, Name = s.Title, s.IsDeleted })
            .FirstOrDefaultAsync();

        if (page is not null && !IsSelf(SlugTargetType.StaticPage, page.Id))
        {
            return new SlugCheckResult(false, SlugTargetType.StaticPage, page.Name, page.IsDeleted);
        }

        return SlugCheckResult.Available();

        // Bản ghi tìm được chính là bản ghi đang sửa ⇒ không tính là trùng
        bool IsSelf(SlugTargetType type, int id) => excludeType == type && excludeId == id;
    }

    public async Task<string> MakeUniqueAsync(
        string baseSlug, SlugTargetType? excludeType = null, int? excludeId = null)
    {
        baseSlug = Slugify(baseSlug);

        if (string.IsNullOrEmpty(baseSlug))
        {
            baseSlug = "muc-moi";
        }

        if ((await CheckAsync(baseSlug, excludeType, excludeId)).IsAvailable)
        {
            return baseSlug;
        }

        for (var i = 2; i <= MaxSuffixAttempts; i++)
        {
            var candidate = $"{baseSlug}-{i}";
            if ((await CheckAsync(candidate, excludeType, excludeId)).IsAvailable)
            {
                return candidate;
            }
        }

        // Rơi vào đây nghĩa là có > 100 bản ghi cùng gốc slug — cực hiếm, nhưng thà trả slug xấu
        // còn hơn lặp vô hạn hoặc ném lỗi giữa lúc admin đang lưu
        return $"{baseSlug}-{Guid.NewGuid().ToString("N")[..8]}";
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugChars();

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultipleDashes();
}
