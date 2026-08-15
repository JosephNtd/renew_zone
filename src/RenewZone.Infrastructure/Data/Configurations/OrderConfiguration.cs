using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;
using RenewZone.Infrastructure.Identity;

namespace RenewZone.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(150);
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 0);

        builder.HasIndex(x => x.Code).IsUnique();
        // Admin lọc đơn theo trạng thái thường xuyên
        builder.HasIndex(x => x.Status);

        // "Đơn hàng của tôi" luôn lọc theo user rồi sắp xếp mới-nhất-trước ⇒ index phủ đúng truy vấn đó
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });

        // SetNull (không Cascade): xóa tài khoản KHÔNG được kéo theo đơn hàng — sổ sách bán hàng
        // phải còn nguyên, và đơn vẫn đọc được nhờ snapshot CustomerName/Phone/Address.
        // Khai bằng HasOne<ApplicationUser>() không navigation vì Order (Domain) không biết kiểu này.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
