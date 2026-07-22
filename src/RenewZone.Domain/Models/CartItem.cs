using System.Text.Json.Serialization;

namespace RenewZone.Domain.Models;

/// <summary>
/// Một dòng trong giỏ hàng (Giai đoạn 4). Là POCO thuần để serialize/deserialize xuống Session
/// bằng System.Text.Json — vì vậy MỌI thuộc tính phải có setter và class phải có ctor rỗng.
/// Giá ở đây LUÔN do server tính (DisplayPrice + tổng PriceAdjustment), không nhận từ client.
/// </summary>
public class CartItem
{
    /// <summary>Định danh dòng giỏ = product + bộ option đã chọn (xem <see cref="BuildKey"/>).</summary>
    public string Key { get; set; } = null!;

    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string ImageUrl { get; set; } = null!;

    /// <summary>Đơn giá ĐÃ gồm điều chỉnh của các option đã chọn.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Mô tả biến thể để hiển thị + snapshot vào OrderItem, vd "256GB · Titan Đen · 99%".</summary>
    public string? VariantDescription { get; set; }

    /// <summary>Id các ProductOptionValue đã chọn — giữ lại để dựng key và đối chiếu về sau.</summary>
    public List<int> OptionValueIds { get; set; } = [];

    public int Quantity { get; set; }

    [JsonIgnore]
    public decimal LineTotal => UnitPrice * Quantity;

    /// <summary>
    /// Sinh key TẤT ĐỊNH, không phụ thuộc thứ tự khách bấm chọn: cùng sản phẩm + cùng bộ option
    /// ⇒ cùng key ⇒ cộng dồn số lượng; khác option ⇒ tách thành dòng mới.
    /// </summary>
    public static string BuildKey(int productId, IEnumerable<int> optionValueIds)
    {
        var ids = optionValueIds.Distinct().Order().ToList();
        return ids.Count == 0 ? productId.ToString() : $"{productId}-{string.Join('-', ids)}";
    }
}
