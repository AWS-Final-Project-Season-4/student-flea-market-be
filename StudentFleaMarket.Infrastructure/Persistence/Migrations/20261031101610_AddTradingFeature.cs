using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTradingFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Transactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "ExchangeRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseNote",
                table: "ExchangeRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CreatedAt",
                table: "Transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Status",
                table: "Transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Type",
                table: "Transactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionRef",
                table: "Payments",
                column: "TransactionRef");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRequests_CreatedAt",
                table: "ExchangeRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRequests_Status",
                table: "ExchangeRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_CreatedAt",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Status",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Type",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TransactionRef",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeRequests_CreatedAt",
                table: "ExchangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeRequests_Status",
                table: "ExchangeRequests");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "ExchangeRequests");

            migrationBuilder.DropColumn(
                name: "ResponseNote",
                table: "ExchangeRequests");
        }
    }
}
