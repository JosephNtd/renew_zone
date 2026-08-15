using RenewZone.Domain.Entities;
using RenewZone.Domain.Models;

namespace RenewZone.Domain.Interfaces;

/// <summary>
/// Nạp nội dung dạng bài viết/trang tĩnh cho SlugController.
/// Tách khỏi catalog (Category/Product — Giai đoạn 3) vì hai nhóm này khác hẳn nhau về truy vấn.
/// KHÔNG cache: nội dung dài, ít lượt xem hơn trang chủ, và ISlugResolver đã chặn phần lớn request rác.
/// </summary>
public interface IContentQueryService
{
    Task<StaticPage?> GetStaticPageAsync(int id);

    Task<NewsDetail?> GetNewsDetailAsync(int id);

    /// <summary>
    /// Tăng lượt xem bài viết +1 (mỗi lần khách mở trang chi tiết). Chạy 1 câu UPDATE nguyên tử
    /// trên DB — giống <c>ICatalogQueryService.IncrementProductViewAsync</c>, không xóa cache.
    /// </summary>
    Task IncrementNewsViewAsync(int id);
}
