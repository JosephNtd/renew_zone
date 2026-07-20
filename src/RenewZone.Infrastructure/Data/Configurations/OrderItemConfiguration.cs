using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(x => x.ProductName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.VariantDescription).HasMaxLength(500);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 0);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Xóa sản phẩm ⇒ FK về null, item vẫn còn nhờ snapshot ProductName/UnitPrice
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
