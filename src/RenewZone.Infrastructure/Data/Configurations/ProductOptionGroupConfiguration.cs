using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class ProductOptionGroupConfiguration : IEntityTypeConfiguration<ProductOptionGroup>
{
    public void Configure(EntityTypeBuilder<ProductOptionGroup> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();

        builder.HasOne(x => x.Product)
            .WithMany(x => x.OptionGroups)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
