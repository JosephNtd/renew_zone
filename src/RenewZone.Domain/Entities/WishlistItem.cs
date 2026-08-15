using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Một sản phẩm khách bấm ♡ để dành xem sau (Giai đoạn 9).
/// Unique index (UserId, ProductId) ⇒ bấm tim nhiều lần không sinh dòng trùng.
/// Xóa sản phẩm kéo theo xóa mục yêu thích (Cascade) — tim trỏ vào sản phẩm không còn
/// thì chẳng hiển thị được gì.
/// </summary>
public class WishlistItem : BaseEntity
{
    public string UserId { get; set; } = null!;

    public int ProductId { get; set; }

    /// <summary>Set tay trong service — <see cref="BaseEntity"/> không có hook audit.</summary>
    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
}
