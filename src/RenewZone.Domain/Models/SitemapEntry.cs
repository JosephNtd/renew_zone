namespace RenewZone.Domain.Models;

/// <summary>
/// Một URL trong sitemap.xml. <see cref="Path"/> là đường dẫn TƯƠNG ĐỐI (vd "/iphone-16") —
/// controller dựng thành absolute bằng scheme+host của request.
/// </summary>
public record SitemapEntry(
    string Path,
    DateTime? LastModified,
    string ChangeFreq,
    double Priority);
