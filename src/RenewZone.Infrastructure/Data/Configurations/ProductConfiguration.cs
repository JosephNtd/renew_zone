using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(250).IsRequired();
        builder.Property(x => x.ShortDescription).HasMaxLength(500);

        // VND không có phần thập phân
        builder.Property(x => x.OriginalPrice).HasPrecision(18, 0);
        builder.Property(x => x.SalePrice).HasPrecision(18, 0);

        // Điểm trung bình đánh giá: 1 chữ số thập phân là đủ (vd 4.8)
        builder.Property(x => x.AvgRating).HasPrecision(3, 2);

        builder.HasIndex(x => x.Slug).IsUnique();
        // Trang danh mục luôn lọc theo CategoryId + IsActive
        builder.HasIndex(x => new { x.CategoryId, x.IsActive });

        // Không cho xóa danh mục còn sản phẩm — buộc admin chuyển sản phẩm sang danh mục khác trước
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
