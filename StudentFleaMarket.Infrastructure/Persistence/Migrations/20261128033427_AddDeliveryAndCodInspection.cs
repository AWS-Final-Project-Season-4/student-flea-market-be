using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAndCodInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryCarrier",
                table: "Transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryMethod",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "InspectionNote",
                table: "Transactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                table: "Transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryCarrier",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DeliveryMethod",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "InspectionNote",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                table: "Transactions");
        }
    }
}
