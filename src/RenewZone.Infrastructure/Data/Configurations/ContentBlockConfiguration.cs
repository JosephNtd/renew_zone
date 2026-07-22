using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class ContentBlockConfiguration : IEntityTypeConfiguration<ContentBlock>
{
    public void Configure(EntityTypeBuilder<ContentBlock> builder)
    {
        builder.Property(x => x.GroupKey).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(60);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Subtitle).HasMaxLength(300);
        builder.Property(x => x.LinkUrl).HasMaxLength(300);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);

        // Truy vấn luôn là "lấy cả nhóm, sắp theo thứ tự"
        builder.HasIndex(x => new { x.GroupKey, x.DisplayOrder });
    }
}
