using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(150);
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 0);

        builder.HasIndex(x => x.Code).IsUnique();
        // Admin lọc đơn theo trạng thái thường xuyên
        builder.HasIndex(x => x.Status);
    }
}
