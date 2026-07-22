using System.Text.Json.Serialization;

namespace RenewZone.Domain.Models;

/// <summary>
/// Giỏ hàng của khách — sống trong Session (guest checkout, không cần tài khoản).
/// Chỉ trở thành <see cref="Entities.Order"/> trong DB khi khách bấm đặt hàng.
/// </summary>
public class Cart
{
    public List<CartItem> Items { get; set; } = [];

    /// <summary>Tổng số lượng — số hiển thị trên badge giỏ ở header.</summary>
    [JsonIgnore]
    public int TotalQuantity => Items.Sum(i => i.Quantity);

    [JsonIgnore]
    public decimal Subtotal => Items.Sum(i => i.LineTotal);

    [JsonIgnore]
    public bool IsEmpty => Items.Count == 0;

    public CartItem? Find(string key) =>
        Items.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.Ordinal));
}
