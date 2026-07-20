namespace RenewZone.Web.Models.ViewModels;

/// <summary>Một mắt xích breadcrumb. Url null ⇒ là trang hiện tại (không phải link).</summary>
public sealed record Crumb(string Text, string? Url = null);

/// <summary>Dữ liệu cho partial _Breadcrumb. Luôn tự thêm "Trang chủ" ở đầu.</summary>
public class BreadcrumbViewModel
{
    public required IReadOnlyList<Crumb> Items { get; init; }

    public static BreadcrumbViewModel Of(params Crumb[] items) => new() { Items = items };
}
