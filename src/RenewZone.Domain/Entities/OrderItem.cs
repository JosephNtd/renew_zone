using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Dòng hàng trong đơn — LƯU SNAPSHOT tên/giá/biến thể tại thời điểm đặt.
/// ProductId nullable + DeleteBehavior.SetNull: admin xóa sản phẩm thì lịch sử đơn vẫn nguyên vẹn.
/// </summary>
public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int? ProductId { get; set; }

    /// <summary>Snapshot tên sản phẩm lúc đặt.</summary>
    public string ProductName { get; set; } = null!;

    /// <summary>Snapshot biến thể khách chọn, vd "256GB, Titan Sa Mạc, 99%, LL/A".</summary>
    public string? VariantDescription { get; set; }

    /// <summary>Đơn giá đã gồm điều chỉnh của các option.</summary>
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;

    public Order Order { get; set; } = null!;
    public Product? Product { get; set; }
}
