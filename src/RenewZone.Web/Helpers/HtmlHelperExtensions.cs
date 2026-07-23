using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RenewZone.Web.Helpers;

public static class HtmlHelperExtensions
{
    /// <summary>
    /// Nhúng một khối JSON-LD an toàn. Trả <see cref="HtmlString"/> (ghi verbatim) nên thẻ
    /// <c>type="application/ld+json"</c> giữ nguyên dấu <c>+</c> — nếu viết thẳng thẻ &lt;script&gt;
    /// trong Razor thì <c>+</c> bị encode thành <c>&amp;#x2B;</c> (vẫn chạy nhưng bẩn).
    /// <para><paramref name="json"/> phải build bằng <see cref="StructuredData"/> (encoder mặc định
    /// escape <c>&lt; &gt; &amp;</c> ⇒ chuỗi <c>&lt;/script&gt;</c> trong dữ liệu không phá được thẻ).</para>
    /// </summary>
    public static IHtmlContent JsonLd(this IHtmlHelper html, string json)
        => new HtmlString($"<script type=\"application/ld+json\">{json}</script>");
}
