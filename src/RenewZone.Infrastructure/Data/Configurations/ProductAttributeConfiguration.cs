using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();

        // Không cho 2 thuộc tính trùng tên (collation SQL Server không phân biệt hoa thường)
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
