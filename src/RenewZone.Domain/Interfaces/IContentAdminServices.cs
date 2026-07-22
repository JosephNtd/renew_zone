using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Domain.Interfaces;

/// <summary>Bài viết + chuyên mục tin.</summary>
public interface INewsAdminService
{
    Task<PagedResult<NewsListItem>> GetListAsync(NewsListRequest request);
    Task<NewsArticle?> GetAsync(int id);
    Task<AdminResult> CreateAsync(NewsInput input);
    Task<AdminResult> UpdateAsync(NewsInput input);
    Task<AdminResult> DeleteAsync(int id);
    Task<AdminResult> ToggleActiveAsync(int id);

    // ----- Chuyên mục -----
    Task<IReadOnlyList<NewsCategoryItem>> GetCategoriesAsync();
    Task<NewsCategory?> GetCategoryAsync(int id);
    Task<AdminResult> SaveCategoryAsync(NewsCategoryInput input);

    /// <summary>Xóa mềm chuyên mục. Bài viết thuộc chuyên mục KHÔNG mất, chỉ chuyển thành chưa phân loại.</summary>
    Task<AdminResult> DeleteCategoryAsync(int id);

    Task<AdminResult> ReorderCategoriesAsync(int[] orderedIds);
}

/// <summary>Trang tĩnh · Banner · Đánh giá — 3 aggregate nhỏ, gom vào một service cho gọn.</summary>
public interface IPageAdminService
{
    // ----- Trang tĩnh -----
    Task<IReadOnlyList<StaticPageItem>> GetPagesAsync();
    Task<StaticPage?> GetPageAsync(int id);
    Task<AdminResult> SavePageAsync(StaticPageInput input);
    Task<AdminResult> DeletePageAsync(int id);

    // ----- Banner -----
    Task<IReadOnlyList<BannerItem>> GetBannersAsync();
    Task<Banner?> GetBannerAsync(int id);
    Task<AdminResult> SaveBannerAsync(BannerInput input);
    Task<AdminResult> DeleteBannerAsync(int id);
    Task<AdminResult> ToggleBannerAsync(int id);
    Task<AdminResult> ReorderBannersAsync(int[] orderedIds);

    // ----- Đánh giá -----
    Task<IReadOnlyList<TestimonialItem>> GetTestimonialsAsync();
    Task<Testimonial?> GetTestimonialAsync(int id);
    Task<AdminResult> SaveTestimonialAsync(TestimonialInput input);
    Task<AdminResult> DeleteTestimonialAsync(int id);
    Task<AdminResult> ToggleTestimonialAsync(int id);
}

/// <summary>Menu 2 cấp (3 nhóm: menu chính, footer chính sách, footer hỗ trợ).</summary>
public interface INavigationAdminService
{
    /// <summary>Cây của một nhóm, kèm cờ cảnh báo URL trỏ tới slug không tồn tại.</summary>
    Task<IReadOnlyList<MenuTreeNode>> GetTreeAsync(MenuGroup group);

    Task<MenuItem?> GetAsync(int id);

    /// <summary>Danh sách chọn mục cha trong cùng nhóm (đã loại chính nó và các mục đang có con).</summary>
    Task<IReadOnlyList<MenuItem>> GetParentOptionsAsync(MenuGroup group, int? excludeId = null);

    Task<AdminResult> SaveAsync(MenuItemInput input);

    /// <summary>Chặn xóa mục còn mục con (đúng ràng buộc Restrict của DB).</summary>
    Task<AdminResult> DeleteAsync(int id);

    Task<AdminResult> ToggleActiveAsync(int id);
    Task<AdminResult> ReorderAsync(int[] orderedIds);
}

/// <summary>Khối nội dung gom theo GroupKey.</summary>
public interface IContentBlockAdminService
{
    Task<IReadOnlyList<ContentBlock>> GetByGroupAsync(string groupKey);
    Task<ContentBlock?> GetAsync(int id);
    Task<AdminResult> SaveAsync(ContentBlockInput input);
    Task<AdminResult> DeleteAsync(int id);
    Task<AdminResult> ToggleActiveAsync(int id);
    Task<AdminResult> ReorderAsync(int[] orderedIds);
}

/// <summary>Hộp thư liên hệ.</summary>
public interface IContactAdminService
{
    Task<PagedResult<ContactListItem>> GetListAsync(ContactListRequest request);
    Task<ContactListItem?> GetAsync(int id);

    /// <summary>Đánh dấu đã đọc (gọi tự động khi admin mở chi tiết).</summary>
    Task<AdminResult> MarkReadAsync(int id, bool isRead = true);

    Task<AdminResult> DeleteAsync(int id);

    /// <summary>Các chủ đề đang có trong hộp thư — để dựng dropdown lọc từ dữ liệu THẬT.</summary>
    Task<IReadOnlyList<string>> GetSubjectsAsync();
}

/// <summary>Cấu hình site (SiteSettings).</summary>
public interface ISettingAdminService
{
    Task<IReadOnlyDictionary<string, string?>> GetAllAsync();

    /// <summary>
    /// Lưu nhiều setting một lần. CHỈ ghi key nằm trong whitelist <c>SettingKeys</c> —
    /// không thì một form bị sửa tay có thể chèn hàng loạt key rác vào bảng cấu hình.
    /// </summary>
    Task<AdminResult> SaveAsync(IDictionary<string, string?> values);
}

/// <summary>Thùng rác — nơi duy nhất nhìn thấy và xóa hẳn bản ghi đã xóa mềm.</summary>
public interface ITrashService
{
    Task<IReadOnlyDictionary<TrashType, int>> GetCountsAsync();
    Task<IReadOnlyList<TrashItem>> GetItemsAsync(TrashType type);
    Task<AdminResult> RestoreAsync(TrashType type, int id);

    /// <summary>Xóa vĩnh viễn — KHÔNG hoàn tác được.</summary>
    Task<AdminResult> PurgeAsync(TrashType type, int id);
}
