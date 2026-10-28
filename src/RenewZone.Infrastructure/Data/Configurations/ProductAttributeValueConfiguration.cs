using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.Property(x => x.Value).HasMaxLength(100).IsRequired();

        // Trong cùng một thuộc tính không có 2 giá trị trùng nhau
        builder.HasIndex(x => new { x.AttributeId, x.Value }).IsUnique();

        // Xóa thuộc tính kéo theo các giá trị của nó (service chỉ cho xóa khi chưa sản phẩm nào dùng)
        builder.HasOne(x => x.Attribute)
            .WithMany(x => x.Values)
            .HasForeignKey(x => x.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
