using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionMeetingCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MeetingLatitude",
                table: "Transactions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MeetingLongitude",
                table: "Transactions",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeetingLatitude",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "MeetingLongitude",
                table: "Transactions");
        }
    }
}
