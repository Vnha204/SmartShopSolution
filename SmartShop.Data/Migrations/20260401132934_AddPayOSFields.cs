using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayOSFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayOSCheckoutUrl",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PayOSOrderCode",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayOSPaymentLinkId",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayOSCheckoutUrl",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PayOSOrderCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PayOSPaymentLinkId",
                table: "Orders");
        }
    }
}
