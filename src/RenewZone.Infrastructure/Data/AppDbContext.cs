using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Common;
using RenewZone.Domain.Entities;
using RenewZone.Infrastructure.Identity;

namespace RenewZone.Infrastructure.Data;

/// <summary>
/// DbContext duy nhất của hệ thống: gộp Identity (quản trị) + các bảng nghiệp vụ.
/// Cấu hình chi tiết từng entity đặt trong Data/Configurations (IEntityTypeConfiguration).
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // ----- Catalog -----
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductOptionGroup> ProductOptionGroups => Set<ProductOptionGroup>();
    public DbSet<ProductOptionValue> ProductOptionValues => Set<ProductOptionValue>();

    // ----- Bán hàng -----
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    // ----- Nội dung -----
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<NewsCategory> NewsCategories => Set<NewsCategory>();
    public DbSet<StaticPage> StaticPages => Set<StaticPage>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();

    // ----- Điều hướng & khối nội dung (Giai đoạn 5 — khử hardcode) -----
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<ContentBlock> ContentBlocks => Set<ContentBlock>();

    // ----- Tương tác & cấu hình -----
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tự quét toàn bộ IEntityTypeConfiguration trong assembly này
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    /// <summary>Gán CreatedAt/UpdatedAt tập trung cho mọi AuditableEntity.</summary>
    private void ApplyAuditInfo()
    {
        var now = DateTime.Now; // giờ local VN — toàn hệ thống chạy 1 múi giờ
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}
