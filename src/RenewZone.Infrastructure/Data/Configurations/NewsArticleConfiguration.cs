using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class NewsArticleConfiguration : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(250).IsRequired();
        builder.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        builder.Property(x => x.Excerpt).HasMaxLength(500);
        builder.Property(x => x.CategoryTag).HasMaxLength(100);
        builder.Property(x => x.ContentHtml).IsRequired();

        builder.HasIndex(x => x.Slug).IsUnique();
        // Listing sắp xếp theo ngày đăng mới nhất
        builder.HasIndex(x => x.PublishedAt);
    }
}
