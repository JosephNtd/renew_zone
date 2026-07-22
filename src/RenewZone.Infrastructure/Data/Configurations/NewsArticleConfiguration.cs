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
        builder.Property(x => x.ContentHtml).IsRequired();

        // SetNull: xóa chuyên mục KHÔNG được kéo theo bài viết — bài chỉ về trạng thái chưa phân loại
        builder.HasOne(x => x.Category)
            .WithMany(c => c.Articles)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Slug).IsUnique();
        // Listing sắp xếp theo ngày đăng mới nhất
        builder.HasIndex(x => x.PublishedAt);
    }
}
