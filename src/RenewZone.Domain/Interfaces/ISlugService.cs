using RenewZone.Domain.Enums;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Chuẩn hóa + kiểm tính duy nhất của slug XUYÊN 4 BẢNG (Category/Product/NewsArticle/StaticPage).
/// <para>
/// <b>Trả nợ kỹ thuật từ Giai đoạn 1</b> (CLAUDE.md Lưu ý 6.4): fallback route <c>{slug}</c> tra cả 4
/// bảng nhưng DB chỉ ép unique TRONG từng bảng. Không có service này thì admin đặt được slug sản phẩm
/// trùng slug danh mục, và <c>SlugResolver</c> sẽ trả về danh mục (theo thứ tự ưu tiên) —
/// sản phẩm biến mất một cách âm thầm, gần như không thể lần ra nguyên nhân.
/// </para>
/// </summary>
public interface ISlugService
{
    /// <summary>Chuẩn hóa tiếng Việt có dấu: "iPhone 16 Pro Max" → "iphone-16-pro-max".</summary>
    string Slugify(string input);

    /// <summary>
    /// Kiểm slug xuyên 4 bảng, KỂ CẢ bản ghi trong Thùng rác (chúng vẫn chiếm unique index của DB).
    /// <paramref name="excludeType"/> + <paramref name="excludeId"/> để form Sửa tự loại chính nó ra.
    /// </summary>
    Task<SlugCheckResult> CheckAsync(string slug, SlugTargetType? excludeType = null, int? excludeId = null);

    /// <summary>Trả về slug còn trống gần nhất: "iphone-16" đã có ⇒ "iphone-16-2", "iphone-16-3"...</summary>
    Task<string> MakeUniqueAsync(string baseSlug, SlugTargetType? excludeType = null, int? excludeId = null);
}
