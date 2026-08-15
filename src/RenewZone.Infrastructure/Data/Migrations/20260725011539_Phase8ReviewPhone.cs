using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenewZone.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase8ReviewPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "ProductReviews",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Phone",
                table: "ProductReviews");
        }
    }
}
