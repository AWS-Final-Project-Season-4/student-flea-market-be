using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminRoleGrant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RoleGrantedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoleGrantedByUserId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RoleRevokedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleGrantedByUserId",
                table: "Users",
                column: "RoleGrantedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_RoleGrantedByUserId",
                table: "Users",
                column: "RoleGrantedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_RoleGrantedByUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleGrantedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleGrantedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleGrantedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleRevokedAt",
                table: "Users");
        }
    }
}
