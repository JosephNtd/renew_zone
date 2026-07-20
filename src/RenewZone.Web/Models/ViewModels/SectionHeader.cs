namespace RenewZone.Web.Models.ViewModels;

/// <summary>
/// Tham số cho partial _SectionHeader (tiêu đề khối trang chủ, DESIGN mục 3.7).
/// LinkUrl null ⇒ không hiện link "Xem tất cả".
/// </summary>
public record SectionHeader(string Kicker, string Title, string? LinkUrl = null, string LinkText = "Xem tất cả");
