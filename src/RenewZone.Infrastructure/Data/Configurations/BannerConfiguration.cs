using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(250);
        builder.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.LinkUrl).HasMaxLength(500);
    }
}
