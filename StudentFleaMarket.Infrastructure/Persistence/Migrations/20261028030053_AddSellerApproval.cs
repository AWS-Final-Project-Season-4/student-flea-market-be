using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SellerApprovedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SellerApprovedByUserId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerRejectionReason",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SellerRequestedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SellerStatus",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SellerApprovedByUserId",
                table: "Users",
                column: "SellerApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_SellerApprovedByUserId",
                table: "Users",
                column: "SellerApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_SellerApprovedByUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SellerApprovedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SellerApprovedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SellerApprovedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SellerRejectionReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SellerRequestedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SellerStatus",
                table: "Users");
        }
    }
}
