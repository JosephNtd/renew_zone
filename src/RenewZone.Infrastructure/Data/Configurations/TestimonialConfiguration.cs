using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class TestimonialConfiguration : IEntityTypeConfiguration<Testimonial>
{
    public void Configure(EntityTypeBuilder<Testimonial> builder)
    {
        builder.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);
        builder.Property(x => x.Content).HasMaxLength(2000).IsRequired();
    }
}
