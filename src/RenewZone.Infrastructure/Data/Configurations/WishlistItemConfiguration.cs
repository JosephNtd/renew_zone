using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;
using RenewZone.Infrastructure.Identity;

namespace RenewZone.Infrastructure.Data.Configurations;

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.Property(x => x.UserId).IsRequired();

        // Chốt chặn ở tầng DB cho thao tác "bật/tắt tim": bấm nhanh 2 lần (hoặc 2 tab cùng lúc)
        // không thể sinh 2 dòng trùng, kể cả khi kiểm tra ở service bị chen giữa.
        builder.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Xóa sản phẩm ⇒ mục yêu thích trỏ vào nó cũng đi (không có gì để hiển thị nữa).
        // WithMany() không navigation: Product không cần biết ai đã thích nó.
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
