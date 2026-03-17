using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductThumbnailPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPublicId",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailPublicId",
                table: "Products");
        }
    }
}
