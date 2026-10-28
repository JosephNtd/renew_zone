using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class DeliveryReviewConfiguration : IEntityTypeConfiguration<DeliveryReview>
{
    public void Configure(EntityTypeBuilder<DeliveryReview> builder)
    {
        builder.Property(x => x.Comment).HasMaxLength(1000);

        // Chặn ở cả DB: số sao chỉ từ 1 đến 5
        builder.ToTable(t => t.HasCheckConstraint("CK_DeliveryReviews_Rating", "[Rating] BETWEEN 1 AND 5"));

        // Quan hệ 1–1 với đơn hàng: OrderId unique ⇒ mỗi đơn chỉ đánh giá giao hàng một lần.
        builder.HasOne(x => x.Order)
            .WithOne(x => x.DeliveryReview)
            .HasForeignKey<DeliveryReview>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
