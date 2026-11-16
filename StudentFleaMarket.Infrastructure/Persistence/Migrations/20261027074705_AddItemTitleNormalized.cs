using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemTitleNormalized : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TitleNormalized",
                table: "Items",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Items_TitleNormalized",
                table: "Items",
                column: "TitleNormalized");

            // Ghi chú: các tin đã tồn tại sẽ có TitleNormalized = "" sau khi thêm cột.
            // Việc backfill (bỏ dấu + chuyển chữ thường) được thực hiện bằng C# trong
            // ItemService/TitleNormalizerBackfill — chạy được trên cả PostgreSQL
            // lẫn InMemory provider, không phụ thuộc extension unaccent của Postgres.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_TitleNormalized",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "TitleNormalized",
                table: "Items");
        }
    }
}
