using RenewZone.Domain.Common;

namespace RenewZone.Domain.Entities;

/// <summary>
/// Khối nội dung lặp lại được gom theo <see cref="GroupKey"/>: dòng cam kết ở trang chi tiết
/// sản phẩm, dòng trấn an ở giỏ/checkout, các kênh liên hệ nhanh...
/// Group rỗng ⇒ view ẨN cả khối (không render khung trống) — xem CLAUDE.md mục 4.7.
/// </summary>
public class ContentBlock : BaseEntity
{
    /// <summary>Khóa nhóm dạng "product.commitments", "cart.assurance", "contact.channels".</summary>
    public string GroupKey { get; set; } = null!;

    /// <summary>Class icon Bootstrap Icons, vd "bi-patch-check-fill".</summary>
    public string? Icon { get; set; }

    public string Title { get; set; } = null!;
    public string? Subtitle { get; set; }
    public string? LinkUrl { get; set; }
    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Danh sách GroupKey chuẩn — dùng hằng để tránh gõ sai chuỗi ở view/seed.</summary>
public static class ContentBlockGroups
{
    public const string ProductCommitments = "product.commitments";
    public const string CartAssurance = "cart.assurance";
    public const string ContactChannels = "contact.channels";
}
