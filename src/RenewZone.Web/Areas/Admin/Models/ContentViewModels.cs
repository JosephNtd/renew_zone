using System.ComponentModel.DataAnnotations;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Models;
using RenewZone.Domain.Models.Admin;

namespace RenewZone.Web.Areas.Admin.Models;

// ===================== Bài viết =====================

public class NewsListViewModel
{
    public required PagedResult<NewsListItem> Result { get; init; }
    public required IReadOnlyList<NewsCategoryItem> Categories { get; init; }
    public int? CategoryId { get; init; }
    public bool? IsActive { get; init; }
    public string? Search { get; init; }

    public bool HasAnyFilter =>
        CategoryId is not null || IsActive is not null || !string.IsNullOrWhiteSpace(Search);

    public string BuildUrl(int? page = null)
    {
        var parts = new List<string>();

        if (CategoryId is not null) { parts.Add($"chuyenMuc={CategoryId}"); }
        if (IsActive is not null) { parts.Add($"trangThai={(IsActive.Value ? "hien" : "an")}"); }
        if (!string.IsNullOrWhiteSpace(Search)) { parts.Add($"q={Uri.EscapeDataString(Search)}"); }

        var effectivePage = page ?? Result.Page;
        if (effectivePage > 1) { parts.Add($"page={effectivePage}"); }

        return "/admin/bai-viet" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
    }
}

public class NewsFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(250, ErrorMessage = "Tiêu đề tối đa 250 ký tự.")]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập slug.")]
    [StringLength(250)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ gồm chữ thường không dấu, số và dấu gạch nối.")]
    public string Slug { get; set; } = string.Empty;

    [Display(Name = "Chuyên mục")]
    public int? CategoryId { get; set; }

    [StringLength(400)]
    [Display(Name = "Ảnh đại diện")]
    public string? ThumbnailUrl { get; set; }

    [StringLength(500, ErrorMessage = "Đoạn trích tối đa 500 ký tự.")]
    [Display(Name = "Đoạn trích")]
    public string? Excerpt { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung bài viết.")]
    [Display(Name = "Nội dung")]
    public string ContentHtml { get; set; } = string.Empty;

    [Display(Name = "Bài nổi bật")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Hiển thị")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Ngày đăng")]
    public DateTime PublishedAt { get; set; } = DateTime.Now;

    public IReadOnlyList<NewsCategoryItem> Categories { get; set; } = [];

    public bool IsEdit => Id is not null;

    public NewsInput ToInput() => new()
    {
        Id = Id,
        Title = Title,
        Slug = Slug,
        CategoryId = CategoryId == 0 ? null : CategoryId,
        ThumbnailUrl = ThumbnailUrl,
        Excerpt = Excerpt,
        ContentHtml = ContentHtml,
        IsFeatured = IsFeatured,
        IsActive = IsActive,
        PublishedAt = PublishedAt
    };

    public static NewsFormViewModel FromEntity(NewsArticle article) => new()
    {
        Id = article.Id,
        Title = article.Title,
        Slug = article.Slug,
        CategoryId = article.CategoryId,
        ThumbnailUrl = article.ThumbnailUrl,
        Excerpt = article.Excerpt,
        ContentHtml = article.ContentHtml,
        IsFeatured = article.IsFeatured,
        IsActive = article.IsActive,
        PublishedAt = article.PublishedAt
    };
}

// ===================== Chuyên mục tin =====================

public class NewsCategoryFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên chuyên mục.")]
    [StringLength(100)]
    [Display(Name = "Tên chuyên mục")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập slug.")]
    [StringLength(120)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ gồm chữ thường không dấu, số và dấu gạch nối.")]
    public string Slug { get; set; } = string.Empty;

    [Range(0, 9999)]
    [Display(Name = "Thứ tự")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hiển thị")]
    public bool IsActive { get; set; } = true;

    public NewsCategoryInput ToInput() => new()
    {
        Id = Id, Name = Name, Slug = Slug, DisplayOrder = DisplayOrder, IsActive = IsActive
    };
}

// ===================== Trang tĩnh =====================

public class StaticPageFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề trang.")]
    [StringLength(200)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập slug.")]
    [StringLength(200)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ gồm chữ thường không dấu, số và dấu gạch nối.")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung trang.")]
    [Display(Name = "Nội dung")]
    public string ContentHtml { get; set; } = string.Empty;

    [Display(Name = "Hiển thị")]
    public bool IsActive { get; set; } = true;

    public bool IsEdit => Id is not null;

    /// <summary>Mục menu đang trỏ tới trang này — cảnh báo trước khi admin đổi slug.</summary>
    public int LinkedMenuCount { get; set; }

    public StaticPageInput ToInput() => new()
    {
        Id = Id, Title = Title, Slug = Slug, ContentHtml = ContentHtml, IsActive = IsActive
    };

    public static StaticPageFormViewModel FromEntity(StaticPage page) => new()
    {
        Id = page.Id,
        Title = page.Title,
        Slug = page.Slug,
        ContentHtml = page.ContentHtml,
        IsActive = page.IsActive
    };
}

// ===================== Banner =====================

public class BannerFormViewModel
{
    public int? Id { get; set; }

    [StringLength(200)]
    [Display(Name = "Tiêu đề (nội bộ)")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Banner bắt buộc phải có ảnh.")]
    [StringLength(400)]
    [Display(Name = "Ảnh banner")]
    public string ImageUrl { get; set; } = string.Empty;

    [StringLength(400)]
    [Display(Name = "Link khi bấm vào")]
    public string? LinkUrl { get; set; }

    [Display(Name = "Vị trí")]
    public BannerPosition Position { get; set; }

    [Range(0, 9999)]
    [Display(Name = "Thứ tự")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hiển thị")]
    public bool IsActive { get; set; } = true;

    public bool IsEdit => Id is not null;

    public BannerInput ToInput() => new()
    {
        Id = Id, Title = Title, ImageUrl = ImageUrl, LinkUrl = LinkUrl,
        Position = Position, DisplayOrder = DisplayOrder, IsActive = IsActive
    };

    public static BannerFormViewModel FromEntity(Banner banner) => new()
    {
        Id = banner.Id,
        Title = banner.Title,
        ImageUrl = banner.ImageUrl,
        LinkUrl = banner.LinkUrl,
        Position = banner.Position,
        DisplayOrder = banner.DisplayOrder,
        IsActive = banner.IsActive
    };
}

// ===================== Đánh giá =====================

public class TestimonialFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên khách hàng.")]
    [StringLength(120)]
    [Display(Name = "Tên khách hàng")]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(400)]
    [Display(Name = "Ảnh đại diện")]
    public string? AvatarUrl { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
    [StringLength(1000, ErrorMessage = "Nội dung tối đa 1000 ký tự.")]
    [Display(Name = "Nội dung")]
    public string Content { get; set; } = string.Empty;

    [Range(0, 9999)]
    [Display(Name = "Thứ tự")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hiển thị")]
    public bool IsActive { get; set; } = true;

    public bool IsEdit => Id is not null;

    public TestimonialInput ToInput() => new()
    {
        Id = Id, CustomerName = CustomerName, AvatarUrl = AvatarUrl,
        Content = Content, DisplayOrder = DisplayOrder, IsActive = IsActive
    };

    public static TestimonialFormViewModel FromEntity(Testimonial t) => new()
    {
        Id = t.Id,
        CustomerName = t.CustomerName,
        AvatarUrl = t.AvatarUrl,
        Content = t.Content,
        DisplayOrder = t.DisplayOrder,
        IsActive = t.IsActive
    };
}

// ===================== Menu =====================

public class MenuIndexViewModel
{
    public required MenuGroup Group { get; init; }
    public required IReadOnlyList<MenuTreeNode> Tree { get; init; }
    public required IReadOnlyDictionary<MenuGroup, int> Counts { get; init; }

    /// <summary>Số mục có URL trỏ tới slug không tồn tại — hiện cảnh báo ở đầu màn.</summary>
    public int BrokenCount => Tree.Sum(n => (n.IsBroken == true ? 1 : 0)
                                            + n.Children.Count(c => c.IsBroken == true));
}

public class MenuFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Nhóm menu")]
    public MenuGroup Group { get; set; }

    [Display(Name = "Kiểu mục")]
    public MenuItemType Type { get; set; } = MenuItemType.Link;

    [Required(ErrorMessage = "Vui lòng nhập tên hiển thị.")]
    [StringLength(100)]
    [Display(Name = "Tên hiển thị")]
    public string Text { get; set; } = string.Empty;

    [StringLength(400)]
    [Display(Name = "Đường dẫn")]
    public string? Url { get; set; }

    [StringLength(60)]
    [Display(Name = "Icon")]
    public string? Icon { get; set; }

    [Display(Name = "Mục cha")]
    public int? ParentId { get; set; }

    [Range(0, 9999)]
    [Display(Name = "Thứ tự")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hiển thị")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Mở tab mới")]
    public bool OpenInNewTab { get; set; }

    public IReadOnlyList<MenuItem> ParentOptions { get; set; } = [];

    public bool IsEdit => Id is not null;

    public MenuItemInput ToInput() => new()
    {
        Id = Id, Group = Group, Type = Type, Text = Text, Url = Url, Icon = Icon,
        ParentId = ParentId, DisplayOrder = DisplayOrder, IsActive = IsActive,
        OpenInNewTab = OpenInNewTab
    };

    public static MenuFormViewModel FromEntity(MenuItem item) => new()
    {
        Id = item.Id,
        Group = item.Group,
        Type = item.Type,
        Text = item.Text,
        Url = item.Url,
        Icon = item.Icon,
        ParentId = item.ParentId,
        DisplayOrder = item.DisplayOrder,
        IsActive = item.IsActive,
        OpenInNewTab = item.OpenInNewTab
    };
}

// ===================== Khối nội dung =====================

public class ContentBlockIndexViewModel
{
    public required string GroupKey { get; init; }
    public required IReadOnlyList<ContentBlock> Blocks { get; init; }

    /// <summary>Nhóm hiển thị bằng tên tiếng Việt + mô tả CHỖ NÀO trên web khách đang dùng nó.</summary>
    public static readonly (string Key, string Name, string Where, string Icon)[] Groups =
    [
        (ContentBlockGroups.ProductCommitments, "Cam kết bán hàng",
            "Hiện ở trang chi tiết sản phẩm, ngay dưới nút mua hàng.", "bi-patch-check"),
        (ContentBlockGroups.CartAssurance, "Dòng trấn an giỏ hàng",
            "Hiện ở khối tóm tắt đơn tại trang Giỏ hàng và Đặt hàng.", "bi-shield-check"),
        (ContentBlockGroups.ContactChannels, "Kênh liên hệ nhanh",
            "4 ô lớn ở đầu trang Liên hệ.", "bi-headset")
    ];

    public static (string Name, string Where, string Icon) Describe(string key)
    {
        var found = Groups.FirstOrDefault(g => g.Key == key);
        return found.Key is null ? (key, "", "bi-grid") : (found.Name, found.Where, found.Icon);
    }
}

public class ContentBlockFormViewModel
{
    public int? Id { get; set; }

    [Required]
    public string GroupKey { get; set; } = string.Empty;

    [StringLength(60)]
    [Display(Name = "Icon")]
    public string? Icon { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(200)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Mô tả phụ")]
    public string? Subtitle { get; set; }

    [StringLength(400)]
    [Display(Name = "Đường dẫn")]
    public string? LinkUrl { get; set; }

    [StringLength(400)]
    [Display(Name = "Ảnh")]
    public string? ImageUrl { get; set; }

    [Range(0, 9999)]
    [Display(Name = "Thứ tự")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hiển thị")]
    public bool IsActive { get; set; } = true;

    public bool IsEdit => Id is not null;

    public ContentBlockInput ToInput() => new()
    {
        Id = Id, GroupKey = GroupKey, Icon = Icon, Title = Title, Subtitle = Subtitle,
        LinkUrl = LinkUrl, ImageUrl = ImageUrl, DisplayOrder = DisplayOrder, IsActive = IsActive
    };

    public static ContentBlockFormViewModel FromEntity(ContentBlock block) => new()
    {
        Id = block.Id,
        GroupKey = block.GroupKey,
        Icon = block.Icon,
        Title = block.Title,
        Subtitle = block.Subtitle,
        LinkUrl = block.LinkUrl,
        ImageUrl = block.ImageUrl,
        DisplayOrder = block.DisplayOrder,
        IsActive = block.IsActive
    };
}

// ===================== Hộp thư =====================

public class ContactListViewModel
{
    public required PagedResult<ContactListItem> Result { get; init; }
    public required IReadOnlyList<string> Subjects { get; init; }
    public bool? IsRead { get; init; }
    public string? Subject { get; init; }
    public string? Search { get; init; }

    /// <summary>Tin được mở sẵn trong drawer (đến từ link ở Dashboard).</summary>
    public int? OpenId { get; init; }

    public bool HasAnyFilter =>
        IsRead is not null || !string.IsNullOrWhiteSpace(Subject) || !string.IsNullOrWhiteSpace(Search);

    public string BuildUrl(int? page = null)
    {
        var parts = new List<string>();

        if (IsRead is not null) { parts.Add($"trangThai={(IsRead.Value ? "daDoc" : "chuaDoc")}"); }
        if (!string.IsNullOrWhiteSpace(Subject)) { parts.Add($"chuDe={Uri.EscapeDataString(Subject)}"); }
        if (!string.IsNullOrWhiteSpace(Search)) { parts.Add($"q={Uri.EscapeDataString(Search)}"); }

        var effectivePage = page ?? Result.Page;
        if (effectivePage > 1) { parts.Add($"page={effectivePage}"); }

        return "/admin/lien-he" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
    }
}

// ===================== Cấu hình =====================

/// <param name="Multiline">true ⇒ render textarea thay vì input.</param>
/// <param name="Placeholder">
/// CHÍNH LÀ giá trị fallback trong code — admin nhìn placeholder là biết ngay
/// "để trống thì web hiện gì".
/// </param>
public record SettingField(
    string Key, string Label, string? Hint = null,
    bool Multiline = false, string? Placeholder = null, bool IsUrl = false);

public record SettingGroup(string Name, string Icon, IReadOnlyList<SettingField> Fields);

public class SettingViewModel
{
    public required IReadOnlyDictionary<string, string?> Values { get; init; }

    public string ValueOf(string key) => Values.GetValueOrDefault(key) ?? "";

    /// <summary>
    /// Nhóm theo CHỦ ĐỀ với nhãn tiếng Việt, không phải bảng key-value trần —
    /// chủ shop không cần biết "BankTransferNoteTemplate" nghĩa là gì.
    /// </summary>
    public static readonly SettingGroup[] Groups =
    [
        new("Thông tin cửa hàng", "bi-shop",
        [
            new(SettingKeys.StoreName, "Tên cửa hàng", Placeholder: "RenewZone"),
            new(SettingKeys.Tagline, "Câu khẩu hiệu", "Hiện ở topbar và thẻ mô tả trang."),
            new(SettingKeys.Hotline, "Hotline", "Hiện ở header, nút gọi và trang liên hệ.",
                Placeholder: "0900.000.000"),
            new(SettingKeys.Email, "Email"),
            new(SettingKeys.Address, "Địa chỉ", Multiline: true),
            new(SettingKeys.OpenHours, "Giờ mở cửa",
                "Trang liên hệ tự nhận diện chuỗi dạng “8h30 - 21h30” để hiện chấm Đang mở cửa."),
            new(SettingKeys.TaxCode, "Mã số thuế")
        ]),

        new("Mạng xã hội & bản đồ", "bi-globe2",
        [
            new(SettingKeys.FacebookUrl, "Facebook", "Để trống thì ẩn hẳn nút.", IsUrl: true),
            new(SettingKeys.MessengerUrl, "Messenger", "Dùng cho nút chat nổi.", IsUrl: true),
            new(SettingKeys.ZaloUrl, "Zalo", "Dùng cho nút chat nổi.", IsUrl: true),
            new(SettingKeys.MapUrl, "Link Google Maps", IsUrl: true),
            new(SettingKeys.MapEmbedUrl, "Link nhúng bản đồ",
                "Địa chỉ trong thuộc tính src của mã nhúng Google Maps. Để trống thì ẩn cả khối bản đồ.",
                Multiline: true)
        ]),

        new("Thanh toán chuyển khoản", "bi-bank",
        [
            new(SettingKeys.BankName, "Tên ngân hàng"),
            new(SettingKeys.BankAccountNumber, "Số tài khoản"),
            new(SettingKeys.BankAccountName, "Chủ tài khoản"),
            new(SettingKeys.BankTransferNoteTemplate, "Mẫu nội dung chuyển khoản",
                "Dùng {0} ở chỗ muốn chèn mã đơn hàng. Ví dụ: “Thanh toan don {0}”.")
        ]),

        new("Nội dung các trang", "bi-file-text",
        [
            new(SettingKeys.ShippingNote, "Ghi chú phí vận chuyển",
                "Hiện ở khối tóm tắt đơn tại giỏ hàng và trang đặt hàng."),
            new(SettingKeys.FooterAbout, "Giới thiệu ở chân trang", Multiline: true),
            new(SettingKeys.FooterDisclaimer, "Dòng bản quyền chân trang", Multiline: true),
            new(SettingKeys.NewsPageTitle, "Tiêu đề trang tin tức"),
            new(SettingKeys.NewsPageIntro, "Lời dẫn trang tin tức", Multiline: true),
            new(SettingKeys.ContactIntro, "Lời mời ở trang liên hệ", Multiline: true),
            new(SettingKeys.ContactSubjects, "Chủ đề form liên hệ",
                "Mỗi chủ đề một dòng.", Multiline: true)
        ])
    ];
}

// ===================== Thùng rác =====================

public class TrashViewModel
{
    public required TrashType Type { get; init; }
    public required IReadOnlyList<TrashItem> Items { get; init; }
    public required IReadOnlyDictionary<TrashType, int> Counts { get; init; }

    public static string LabelOf(TrashType type) => type switch
    {
        TrashType.Product => "Sản phẩm",
        TrashType.Category => "Danh mục",
        TrashType.NewsArticle => "Bài viết",
        TrashType.NewsCategory => "Chuyên mục tin",
        TrashType.StaticPage => "Trang tĩnh",
        TrashType.Banner => "Banner",
        TrashType.Testimonial => "Đánh giá",
        TrashType.MenuItem => "Mục menu",
        TrashType.ContentBlock => "Khối nội dung",
        TrashType.ContactMessage => "Tin liên hệ",
        _ => type.ToString()
    };

    public int TotalCount => Counts.Values.Sum();
}
