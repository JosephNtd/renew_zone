using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.Property(x => x.AuthorName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Content).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.AdminReply).HasMaxLength(2000);

        // PDP lọc "đánh giá đã duyệt của sản phẩm này"; admin lọc "chờ duyệt" ⇒ cùng 1 index phục vụ tốt
        builder.HasIndex(x => new { x.ProductId, x.Status });

        // Xóa sản phẩm kéo theo đánh giá (giống ProductImage/OptionGroup) — đánh giá không có ý nghĩa
        // độc lập với sản phẩm đã biến mất.
        builder.HasOne(x => x.Product)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
