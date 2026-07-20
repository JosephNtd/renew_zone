using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionValue> builder)
    {
        builder.Property(x => x.Value).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PriceAdjustment).HasPrecision(18, 0);

        builder.HasOne(x => x.Group)
            .WithMany(x => x.Values)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
