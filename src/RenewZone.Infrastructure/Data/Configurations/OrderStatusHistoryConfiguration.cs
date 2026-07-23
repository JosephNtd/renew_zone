using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Configurations;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.Property(x => x.ChangedBy).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);

        // Xóa đơn (không bao giờ xảy ra — Order không xóa) thì nhật ký đi theo, không để rác mồ côi
        builder.HasOne(x => x.Order)
            .WithMany(x => x.StatusHistory)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Truy vấn timeline luôn lọc theo đơn + sắp theo thời gian ⇒ index đúng cặp cột đó
        builder.HasIndex(x => new { x.OrderId, x.ChangedAt });
    }
}
