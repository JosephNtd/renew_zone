using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Common;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Infrastructure.Identity;

namespace RenewZone.Infrastructure.Data;

/// <summary>
/// DbContext duy nhất của hệ thống: gộp Identity (quản trị) + các bảng nghiệp vụ.
/// Cấu hình chi tiết từng entity đặt trong Data/Configurations (IEntityTypeConfiguration).
/// <para>
/// SaveChanges làm 3 việc tập trung để code nghiệp vụ không phải nhớ: gán audit,
/// biến xóa cứng thành xóa mềm, và xóa cache web khách sau khi ghi thành công.
/// </para>
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICacheInvalidator? _cacheInvalidator;

    /// <summary>
    /// <paramref name="cacheInvalidator"/> để nullable vì <c>dotnet ef</c> (migrations) dựng DbContext
    /// qua constructor này mà không có DI container đầy đủ — thiếu nó thì chỉ mất phần xóa cache,
    /// không ảnh hưởng schema.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options, ICacheInvalidator? cacheInvalidator = null)
        : base(options)
    {
        _cacheInvalidator = cacheInvalidator;
    }

    // Catalog
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductOptionGroup> ProductOptionGroups => Set<ProductOptionGroup>();
    public DbSet<ProductOptionValue> ProductOptionValues => Set<ProductOptionValue>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    // Bán hàng
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    // Nội dung
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<NewsCategory> NewsCategories => Set<NewsCategory>();
    public DbSet<StaticPage> StaticPages => Set<StaticPage>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();

    // Điều hướng & khối nội dung (Giai đoạn 5 — khử hardcode)
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<ContentBlock> ContentBlocks => Set<ContentBlock>();

    // Tương tác & cấu hình
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    // Tài khoản khách hàng (Giai đoạn 9) — dữ liệu RIÊNG của từng user, không xóa mềm:
    // khách xóa địa chỉ/bỏ tim là mất thật, Thùng rác của admin không chứa dữ liệu riêng tư.
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<UserCart> UserCarts => Set<UserCart>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tự quét toàn bộ IEntityTypeConfiguration trong assembly này
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplySoftDeleteQueryFilters(builder);
    }

    /// <summary>
    /// Gắn global query filter <c>e =&gt; !e.IsDeleted</c> cho MỌI entity cài
    /// <see cref="ISoftDeletable"/> — dựng biểu thức bằng reflection thay vì gõ tay từng bảng,
    /// nên thêm entity xóa mềm ở giai đoạn sau không cần sửa file này.
    /// <para>
    /// Hệ quả: web khách, danh sách admin và cả số đếm Dashboard đều tự động bỏ qua bản ghi
    /// trong Thùng rác. Muốn nhìn thấy chúng (màn Thùng rác) phải gọi <c>IgnoreQueryFilters()</c>.
    /// </para>
    /// </summary>
    private static void ApplySoftDeleteQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes()
                     .Where(t => typeof(ISoftDeletable).IsAssignableFrom(t.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Not(
                Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));

            builder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        ApplySoftDelete();

        // Gom loại entity bị đụng TRƯỚC khi save — sau khi save ChangeTracker đã reset state
        var touchedTypes = CollectTouchedEntityTypes();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Chỉ xóa cache khi ghi THÀNH CÔNG: save lỗi thì dữ liệu không đổi, cache cũ vẫn đúng
        _cacheInvalidator?.OnEntitiesChanged(touchedTypes);

        return result;
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        ApplySoftDelete();

        var touchedTypes = CollectTouchedEntityTypes();

        var result = base.SaveChanges();

        _cacheInvalidator?.OnEntitiesChanged(touchedTypes);

        return result;
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

    /// <summary>
    /// Biến <c>Remove()</c> trên entity nội dung thành UPDATE cờ <c>IsDeleted</c>.
    /// Đặt chốt chặn tại đây (chứ không trông chờ mỗi service tự nhớ) nên KHÔNG ai
    /// xóa cứng nhầm được — muốn xóa thật phải cố ý dùng <c>ExecuteDeleteAsync()</c>.
    /// </summary>
    private void ApplySoftDelete()
    {
        var now = DateTime.Now;
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
                     .Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
        }
    }

    private List<Type> CollectTouchedEntityTypes() =>
        ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.GetType())
            .Distinct()
            .ToList();
}
