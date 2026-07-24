using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Enums;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>
/// Xem <see cref="IPageAdminService"/>. Gom 3 aggregate nhỏ (trang tĩnh, banner, đánh giá)
/// vào một service vì mỗi cái chỉ có CRUD phẳng, tách 3 file chỉ thêm chỗ để lạc.
/// </summary>
public class PageAdminService(AppDbContext db, ISlugService slugs) : IPageAdminService
{
    // Trang tĩnh
    public async Task<IReadOnlyList<StaticPageItem>> GetPagesAsync() =>
        await db.StaticPages.AsNoTracking()
            .OrderBy(p => p.Title)
            .Select(p => new StaticPageItem(p.Id, p.Title, p.Slug, p.IsActive, p.UpdatedAt))
            .ToListAsync();

    public async Task<StaticPage?> GetPageAsync(int id) =>
        await db.StaticPages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<AdminResult> SavePageAsync(StaticPageInput input)
    {
        var slug = slugs.Slugify(input.Slug);

        var check = await slugs.CheckAsync(slug, SlugTargetType.StaticPage, input.Id);
        if (!check.IsAvailable)
        {
            return AdminResult.Fail(check.Message!);
        }

        var page = input.Id is { } id
            ? await db.StaticPages.FirstOrDefaultAsync(p => p.Id == id)
            : null;

        if (input.Id is not null && page is null)
        {
            return AdminResult.Fail("Không tìm thấy trang.");
        }

        if (page is null)
        {
            page = new StaticPage();
            db.StaticPages.Add(page);
        }

        page.Title = input.Title.Trim();
        page.Slug = slug;
        page.ContentHtml = input.ContentHtml;
        page.IsActive = input.IsActive;

        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã lưu trang “{page.Title}”.", page.Id);
    }

    public async Task<AdminResult> DeletePageAsync(int id)
    {
        var page = await db.StaticPages.FirstOrDefaultAsync(p => p.Id == id);

        if (page is null)
        {
            return AdminResult.Fail("Không tìm thấy trang.");
        }

        // Trang tĩnh thường được menu trỏ tới ⇒ cảnh báo để admin đi sửa menu, tránh link 404
        var linkCount = await db.MenuItems.CountAsync(m => m.Url == "/" + page.Slug);

        db.StaticPages.Remove(page);
        await db.SaveChangesAsync();

        var note = linkCount > 0
            ? $" ⚠️ Có {linkCount} mục menu đang trỏ tới trang này — hãy sửa lại menu."
            : "";

        return AdminResult.Success($"Đã chuyển trang “{page.Title}” vào Thùng rác.{note}");
    }

    // Banner

    public async Task<IReadOnlyList<BannerItem>> GetBannersAsync() =>
        await db.Banners.AsNoTracking()
            .OrderBy(b => b.Position).ThenBy(b => b.DisplayOrder)
            .Select(b => new BannerItem(b.Id, b.Title, b.ImageUrl, b.LinkUrl,
                b.Position, b.DisplayOrder, b.IsActive))
            .ToListAsync();

    public async Task<Banner?> GetBannerAsync(int id) =>
        await db.Banners.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);

    public async Task<AdminResult> SaveBannerAsync(BannerInput input)
    {
        if (string.IsNullOrWhiteSpace(input.ImageUrl))
        {
            return AdminResult.Fail("Banner bắt buộc phải có ảnh.");
        }

        var banner = input.Id is { } id
            ? await db.Banners.FirstOrDefaultAsync(b => b.Id == id)
            : null;

        if (input.Id is not null && banner is null)
        {
            return AdminResult.Fail("Không tìm thấy banner.");
        }

        if (banner is null)
        {
            banner = new Banner();
            db.Banners.Add(banner);
        }

        banner.Title = input.Title;
        banner.ImageUrl = input.ImageUrl;
        banner.LinkUrl = string.IsNullOrWhiteSpace(input.LinkUrl) ? null : input.LinkUrl.Trim();
        banner.Position = input.Position;
        banner.DisplayOrder = input.DisplayOrder;
        banner.IsActive = input.IsActive;

        await db.SaveChangesAsync();

        return AdminResult.Success("Đã lưu banner.", banner.Id);
    }

    public async Task<AdminResult> DeleteBannerAsync(int id)
    {
        var banner = await db.Banners.FirstOrDefaultAsync(b => b.Id == id);

        if (banner is null)
        {
            return AdminResult.Fail("Không tìm thấy banner.");
        }

        db.Banners.Remove(banner);
        await db.SaveChangesAsync();

        return AdminResult.Success("Đã chuyển banner vào Thùng rác.");
    }

    public async Task<AdminResult> ToggleBannerAsync(int id)
    {
        var banner = await db.Banners.FirstOrDefaultAsync(b => b.Id == id);

        if (banner is null)
        {
            return AdminResult.Fail("Không tìm thấy banner.");
        }

        banner.IsActive = !banner.IsActive;
        await db.SaveChangesAsync();

        return AdminResult.Success(banner.IsActive ? "Đã hiện banner." : "Đã ẩn banner.");
    }

    public async Task<AdminResult> ReorderBannersAsync(int[] orderedIds)
    {
        var banners = await db.Banners.Where(b => orderedIds.Contains(b.Id)).ToListAsync();

        for (var i = 0; i < orderedIds.Length; i++)
        {
            var banner = banners.FirstOrDefault(b => b.Id == orderedIds[i]);
            if (banner is not null)
            {
                banner.DisplayOrder = i;
            }
        }

        await db.SaveChangesAsync();

        return AdminResult.Success("Đã lưu thứ tự banner.");
    }

    // Đánh giá

    public async Task<IReadOnlyList<TestimonialItem>> GetTestimonialsAsync() =>
        await db.Testimonials.AsNoTracking()
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new TestimonialItem(t.Id, t.CustomerName, t.AvatarUrl, t.Content,
                t.DisplayOrder, t.IsActive))
            .ToListAsync();

    public async Task<Testimonial?> GetTestimonialAsync(int id) =>
        await db.Testimonials.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

    public async Task<AdminResult> SaveTestimonialAsync(TestimonialInput input)
    {
        var testimonial = input.Id is { } id
            ? await db.Testimonials.FirstOrDefaultAsync(t => t.Id == id)
            : null;

        if (input.Id is not null && testimonial is null)
        {
            return AdminResult.Fail("Không tìm thấy đánh giá.");
        }

        if (testimonial is null)
        {
            testimonial = new Testimonial();
            db.Testimonials.Add(testimonial);
        }

        testimonial.CustomerName = input.CustomerName.Trim();
        testimonial.AvatarUrl = input.AvatarUrl;
        testimonial.Content = input.Content.Trim();
        testimonial.DisplayOrder = input.DisplayOrder;
        testimonial.IsActive = input.IsActive;

        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã lưu đánh giá của “{testimonial.CustomerName}”.", testimonial.Id);
    }

    public async Task<AdminResult> DeleteTestimonialAsync(int id)
    {
        var testimonial = await db.Testimonials.FirstOrDefaultAsync(t => t.Id == id);

        if (testimonial is null)
        {
            return AdminResult.Fail("Không tìm thấy đánh giá.");
        }

        db.Testimonials.Remove(testimonial);
        await db.SaveChangesAsync();

        return AdminResult.Success("Đã chuyển đánh giá vào Thùng rác.");
    }

    public async Task<AdminResult> ToggleTestimonialAsync(int id)
    {
        var testimonial = await db.Testimonials.FirstOrDefaultAsync(t => t.Id == id);

        if (testimonial is null)
        {
            return AdminResult.Fail("Không tìm thấy đánh giá.");
        }

        testimonial.IsActive = !testimonial.IsActive;
        await db.SaveChangesAsync();

        return AdminResult.Success(testimonial.IsActive ? "Đã hiện đánh giá." : "Đã ẩn đánh giá.");
    }
}
