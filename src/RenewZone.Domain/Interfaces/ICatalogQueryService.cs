using RenewZone.Domain.Models;

namespace RenewZone.Domain.Interfaces;

/// <summary>Truy vấn catalog cho trang danh mục, chi tiết sản phẩm và tìm kiếm (Giai đoạn 3).</summary>
public interface ICatalogQueryService
{
    /// <summary>
    /// Dựng trang danh mục theo Id danh mục đã resolve từ slug.
    /// <paramref name="seriesSlug"/> lọc theo series con (null ⇒ tất cả).
    /// Vào thẳng slug của series con thì truyền categoryId của con — service tự nhận cha.
    /// </summary>
    Task<CategoryPage?> GetCategoryPageAsync(int categoryId, string? seriesSlug, int page, int pageSize);

    Task<ProductDetail?> GetProductDetailAsync(int productId);

    /// <summary>
    /// Tăng lượt xem sản phẩm +1 (mỗi lần khách mở trang chi tiết). Chạy 1 câu UPDATE nguyên tử
    /// trên DB (không load entity, không race). KHÔNG xóa cache — ViewCount chỉ hiển thị ở admin
    /// (đọc thẳng DB), nếu invalidate mỗi lượt xem sẽ phá hiệu năng Home/Slugs.
    /// </summary>
    Task IncrementProductViewAsync(int productId);

    /// <summary>Toàn bộ sản phẩm active, phân trang — cho trang "/san-pham".</summary>
    Task<PagedResult<Domain.Entities.Product>> GetAllProductsAsync(int page, int pageSize);

    /// <summary>Tìm theo tên sản phẩm (không dấu ở giao diện chưa cần — DB collation đã bỏ dấu).</summary>
    Task<PagedResult<Domain.Entities.Product>> SearchAsync(string? keyword, int page, int pageSize);

    /// <summary>
    /// Nạp sản phẩm (kèm ảnh chính + toàn bộ option groups/values) để CartService tính lại
    /// giá và mô tả biến thể Ở SERVER khi thêm giỏ — client chỉ gửi id, không gửi giá (Giai đoạn 4).
    /// Trả null nếu sản phẩm không tồn tại hoặc đã ẩn.
    /// </summary>
    Task<Domain.Entities.Product?> GetProductForCartAsync(int productId);
}
