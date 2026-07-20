using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class StaticPageConfiguration : IEntityTypeConfiguration<StaticPage>
{
    public void Configure(EntityTypeBuilder<StaticPage> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(250).IsRequired();
        builder.Property(x => x.ContentHtml).IsRequired();

        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
