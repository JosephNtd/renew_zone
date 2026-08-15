using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;
using RenewZone.Infrastructure.Identity;

namespace RenewZone.Infrastructure.Data.Configurations;

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(50);
        builder.Property(x => x.ReceiverName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        // Cùng độ dài với Order.Address để địa chỉ chép sang đơn không bao giờ bị cắt cụt
        builder.Property(x => x.AddressLine).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => x.UserId);

        // Cascade: xóa tài khoản thì sổ địa chỉ đi theo — đây là dữ liệu riêng tư của khách,
        // giữ lại vừa vô dụng vừa là rủi ro lộ thông tin. (Khác Order/ProductReview: sổ sách
        // và nội dung công khai thì phải sống sót.)
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
