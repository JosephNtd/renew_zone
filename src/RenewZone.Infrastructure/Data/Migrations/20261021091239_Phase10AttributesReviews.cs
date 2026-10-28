using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenewZone.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Giai đoạn 10:
    /// <list type="bullet">
    /// <item>Danh sách thuộc tính chung (ProductAttributes / ProductAttributeValues). Tên nhóm và giá trị
    /// biến thể cũ (gõ tay) được CHUYỂN SANG danh sách mới rồi mới xóa cột chữ — không mất biến thể nào,
    /// Id của ProductOptionValues giữ nguyên nên giỏ hàng đang lưu vẫn dùng được.</item>
    /// <item>ProductReviews.OrderItemId: mỗi lần mua đánh giá một lần; đánh giá cũ được ghép lần lượt với
    /// các lần mua (đơn Hoàn thành) của cùng tài khoản + sản phẩm.</item>
    /// <item>DeliveryReviews (đánh giá giao hàng, 1–1 với đơn) và ContactMessages.UserId.</item>
    /// </list>
    /// ⚠️ Bản do EF sinh ra xóa cột Name/Value TRƯỚC khi thêm cột mới ⇒ mất dữ liệu. Đã viết lại tay.
    /// </summary>
    public partial class Phase10AttributesReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------------------------------------------------------------- 1. Danh sách thuộc tính
            migrationBuilder.CreateTable(
                name: "ProductAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttributeId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributeValues_ProductAttributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "ProductAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_Name",
                table: "ProductAttributes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_AttributeId_Value",
                table: "ProductAttributeValues",
                columns: new[] { "AttributeId", "Value" },
                unique: true);

            // ---------------------------------------------------------------- 2. Chuyển biến thể cũ sang danh sách
            // Thêm cột khóa ngoại dạng NULL trước để còn đổ dữ liệu, xong mới chuyển sang NOT NULL.
            migrationBuilder.AddColumn<int>(
                name: "AttributeId",
                table: "ProductOptionGroups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttributeValueId",
                table: "ProductOptionValues",
                type: "int",
                nullable: true);

            // Mỗi tên nhóm khác nhau (đã bỏ khoảng trắng thừa; collation không phân biệt hoa thường nên
            // "Màu sắc" và "màu sắc" gộp làm một) thành một thuộc tính, theo thứ tự xuất hiện.
            migrationBuilder.Sql(@"
INSERT INTO ProductAttributes (Name, DisplayOrder)
SELECT n.Name, ROW_NUMBER() OVER (ORDER BY n.FirstId) - 1
FROM (
    SELECT LTRIM(RTRIM(Name)) AS Name, MIN(Id) AS FirstId
    FROM ProductOptionGroups
    GROUP BY LTRIM(RTRIM(Name))
) n;

UPDATE g SET AttributeId = a.Id
FROM ProductOptionGroups g
JOIN ProductAttributes a ON a.Name = LTRIM(RTRIM(g.Name));

INSERT INTO ProductAttributeValues (AttributeId, Value, DisplayOrder)
SELECT x.AttributeId, x.Value,
       ROW_NUMBER() OVER (PARTITION BY x.AttributeId ORDER BY x.FirstOrder, x.FirstId) - 1
FROM (
    SELECT g.AttributeId, LTRIM(RTRIM(v.Value)) AS Value, MIN(v.DisplayOrder) AS FirstOrder, MIN(v.Id) AS FirstId
    FROM ProductOptionValues v
    JOIN ProductOptionGroups g ON g.Id = v.GroupId
    GROUP BY g.AttributeId, LTRIM(RTRIM(v.Value))
) x;

UPDATE v SET AttributeValueId = av.Id
FROM ProductOptionValues v
JOIN ProductOptionGroups g ON g.Id = v.GroupId
JOIN ProductAttributeValues av ON av.AttributeId = g.AttributeId AND av.Value = LTRIM(RTRIM(v.Value));
");

            migrationBuilder.AlterColumn<int>(
                name: "AttributeId",
                table: "ProductOptionGroups",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AttributeValueId",
                table: "ProductOptionValues",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ProductOptionGroups");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ProductOptionValues");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionGroups_AttributeId",
                table: "ProductOptionGroups",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_AttributeValueId",
                table: "ProductOptionValues",
                column: "AttributeValueId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOptionGroups_ProductAttributes_AttributeId",
                table: "ProductOptionGroups",
                column: "AttributeId",
                principalTable: "ProductAttributes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOptionValues_ProductAttributeValues_AttributeValueId",
                table: "ProductOptionValues",
                column: "AttributeValueId",
                principalTable: "ProductAttributeValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ---------------------------------------------------------------- 3. Đánh giá theo lần mua
            migrationBuilder.AddColumn<int>(
                name: "OrderItemId",
                table: "ProductReviews",
                type: "int",
                nullable: true);

            // Ghép đánh giá cũ với lần mua: đánh giá thứ k (theo thời gian) của một tài khoản cho một sản phẩm
            // ⇒ lần mua thứ k (đơn Hoàn thành = 3) của tài khoản đó. Đánh giá dư (viết nhiều hơn số lần mua)
            // giữ NULL, vẫn hiển thị bình thường như trước.
            migrationBuilder.Sql(@"
WITH r AS (
    SELECT Id, UserId, ProductId,
           ROW_NUMBER() OVER (PARTITION BY UserId, ProductId ORDER BY CreatedAt, Id) AS rn
    FROM ProductReviews
    WHERE UserId IS NOT NULL
),
i AS (
    SELECT oi.Id, o.UserId, oi.ProductId,
           ROW_NUMBER() OVER (PARTITION BY o.UserId, oi.ProductId ORDER BY o.CreatedAt, oi.Id) AS rn
    FROM OrderItems oi
    JOIN Orders o ON o.Id = oi.OrderId
    WHERE o.Status = 3 AND o.UserId IS NOT NULL AND oi.ProductId IS NOT NULL
)
UPDATE pr SET OrderItemId = i.Id
FROM ProductReviews pr
JOIN r ON r.Id = pr.Id
JOIN i ON i.UserId = r.UserId AND i.ProductId = r.ProductId AND i.rn = r.rn;
");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_OrderItemId",
                table: "ProductReviews",
                column: "OrderItemId",
                unique: true,
                filter: "[OrderItemId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReviews_OrderItems_OrderItemId",
                table: "ProductReviews",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ---------------------------------------------------------------- 4. Đánh giá giao hàng
            migrationBuilder.CreateTable(
                name: "DeliveryReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryReviews", x => x.Id);
                    table.CheckConstraint("CK_DeliveryReviews_Rating", "[Rating] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_DeliveryReviews_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryReviews_OrderId",
                table: "DeliveryReviews",
                column: "OrderId",
                unique: true);

            // ---------------------------------------------------------------- 5. Người gửi tin liên hệ
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ContactMessages",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_UserId",
                table: "ContactMessages",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactMessages_AspNetUsers_UserId",
                table: "ContactMessages",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactMessages_AspNetUsers_UserId",
                table: "ContactMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductOptionGroups_ProductAttributes_AttributeId",
                table: "ProductOptionGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductOptionValues_ProductAttributeValues_AttributeValueId",
                table: "ProductOptionValues");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductReviews_OrderItems_OrderItemId",
                table: "ProductReviews");

            // Trả lại cột chữ, chép tên/giá trị từ danh sách chung về trước khi xóa bảng
            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "ProductOptionValues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ProductOptionGroups",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
UPDATE g SET Name = a.Name
FROM ProductOptionGroups g JOIN ProductAttributes a ON a.Id = g.AttributeId;

UPDATE v SET Value = av.Value
FROM ProductOptionValues v JOIN ProductAttributeValues av ON av.Id = v.AttributeValueId;
");

            migrationBuilder.DropTable(
                name: "DeliveryReviews");

            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_OrderItemId",
                table: "ProductReviews");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptionValues_AttributeValueId",
                table: "ProductOptionValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptionGroups_AttributeId",
                table: "ProductOptionGroups");

            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_UserId",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "AttributeValueId",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "AttributeId",
                table: "ProductOptionGroups");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ContactMessages");

            migrationBuilder.DropTable(
                name: "ProductAttributeValues");

            migrationBuilder.DropTable(
                name: "ProductAttributes");
        }
    }
}
