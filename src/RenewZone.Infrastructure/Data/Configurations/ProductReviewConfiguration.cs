using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RenewZone.Domain.Entities;
using RenewZone.Infrastructure.Identity;

namespace RenewZone.Infrastructure.Data.Configurations;

public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.Property(x => x.AuthorName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30); // tùy chọn — SĐT VN + khoảng cách/dấu ngăn
        builder.Property(x => x.Content).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.AdminReply).HasMaxLength(2000);

        // PDP lọc "đánh giá đã duyệt của sản phẩm này"; admin lọc "chờ duyệt" ⇒ cùng 1 index phục vụ tốt
        builder.HasIndex(x => new { x.ProductId, x.Status });

        // Xóa sản phẩm kéo theo đánh giá (giống ProductImage/OptionGroup) — đánh giá không có ý nghĩa
        // độc lập với sản phẩm đã biến mất.
        builder.HasOne(x => x.Product)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tác giả đánh giá. SetNull chứ không Cascade: xóa tài khoản không được làm biến mất
        // đánh giá đã duyệt (điểm trung bình sản phẩm sẽ nhảy số vô cớ) — AuthorName là snapshot
        // nên nội dung vẫn hiển thị đầy đủ.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Kiểm "tài khoản này đã đánh giá sản phẩm đó chưa" + màn "Đánh giá của tôi"
        builder.HasIndex(x => new { x.UserId, x.ProductId });

        // Mỗi dòng đơn hàng chỉ được đánh giá MỘT lần. Lọc NULL vì đánh giá cũ (trước Giai đoạn 10)
        // không ghép được với đơn nào. Bản ghi đã xóa mềm vẫn giữ chỗ: admin xóa đánh giá vi phạm
        // thì khách không viết lại được cho cùng lần mua đó.
        builder.HasIndex(x => x.OrderItemId)
            .IsUnique()
            .HasFilter("[OrderItemId] IS NOT NULL");

        // Restrict (NO ACTION): dòng đơn hàng không bao giờ bị xóa riêng; để Cascade/SetNull ở đây
        // sẽ tạo nhiều đường xóa dây chuyền từ Products tới ProductReviews, SQL Server từ chối.
        builder.HasOne(x => x.OrderItem)
            .WithMany()
            .HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
