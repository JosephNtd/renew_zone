using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Email).HasMaxLength(150);
        builder.Property(x => x.Subject).HasMaxLength(250);
        builder.Property(x => x.Content).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.AttachmentPath).HasMaxLength(500);

        // Admin lọc "liên hệ chưa đọc" ở dashboard
        builder.HasIndex(x => x.IsRead);
    }
}
