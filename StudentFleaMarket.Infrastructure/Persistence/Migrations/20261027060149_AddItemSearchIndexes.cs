using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Items_Condition",
                table: "Items",
                column: "Condition");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CreatedAt",
                table: "Items",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Price",
                table: "Items",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Status",
                table: "Items",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Title",
                table: "Items",
                column: "Title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_Condition",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_CreatedAt",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Price",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Status",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Title",
                table: "Items");
        }
    }
}
