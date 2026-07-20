using RenewZone.Domain.Models;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Cung cấp dữ liệu động cho trang chủ. Tách khỏi ISiteContentService (menu/settings cấp layout)
/// vì đây là dữ liệu cấp trang, admin bật/tắt IsFeatured/IsBestSeller thường xuyên hơn ⇒ cache TTL ngắn hơn.
/// Implementation phải cache — Index gọi mỗi lần vào trang chủ.
/// </summary>
public interface IHomeQueryService
{
    Task<HomeContent> GetHomeContentAsync();

    /// <summary>Xóa cache — gọi khi Admin sửa sản phẩm/banner/tin (Giai đoạn 6).</summary>
    void Invalidate();
}
