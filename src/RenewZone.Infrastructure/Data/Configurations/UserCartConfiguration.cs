using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;
using RenewZone.Infrastructure.Identity;

namespace RenewZone.Infrastructure.Data.Configurations;

public class UserCartConfiguration : IEntityTypeConfiguration<UserCart>
{
    public void Configure(EntityTypeBuilder<UserCart> builder)
    {
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ItemsJson).IsRequired(); // nvarchar(max) — giỏ tối đa vài KB

        // Mỗi tài khoản đúng MỘT giỏ
        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
