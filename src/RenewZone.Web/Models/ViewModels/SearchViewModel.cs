using RenewZone.Domain.Entities;
using RenewZone.Domain.Models;

namespace RenewZone.Web.Models.ViewModels;

/// <summary>Kết quả trang tìm kiếm /tim-kiem?q=...</summary>
public class SearchViewModel
{
    public string? Keyword { get; init; }
    public required PagedResult<Product> Results { get; init; }

    public string PageUrl(int page) => $"/tim-kiem?q={Uri.EscapeDataString(Keyword ?? "")}&page={page}";
}
