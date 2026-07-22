using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.Property(x => x.Text).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(300);
        builder.Property(x => x.Icon).HasMaxLength(60);

        // Restrict: còn mục con thì không cho xóa mục cha (tránh menu mồ côi)
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Layout luôn truy vấn theo nhóm rồi sắp xếp
        builder.HasIndex(x => new { x.Group, x.DisplayOrder });
    }
}
