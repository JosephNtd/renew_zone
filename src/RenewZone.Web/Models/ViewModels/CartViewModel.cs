using RenewZone.Domain.Models;
using RenewZone.Web.Services;

namespace RenewZone.Web.Models.ViewModels;

/// <summary>Dữ liệu trang /gio-hang.</summary>
public class CartViewModel
{
    public required Cart Cart { get; init; }

    /// <summary>Trần số lượng mỗi dòng — view dùng để khóa nút "+" của stepper.</summary>
    public int MaxQuantity => CartService.MaxQuantityPerItem;
}
