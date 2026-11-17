using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddForgotPasswordAndGoogleAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerificationCodes_Email_CodeHash",
                table: "VerificationCodes");

            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                table: "VerificationCodes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "Users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_Email_Purpose_CodeHash",
                table: "VerificationCodes",
                columns: new[] { "Email", "Purpose", "CodeHash" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleId",
                table: "Users",
                column: "GoogleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerificationCodes_Email_Purpose_CodeHash",
                table: "VerificationCodes");

            migrationBuilder.DropIndex(
                name: "IX_Users_GoogleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "VerificationCodes");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_Email_CodeHash",
                table: "VerificationCodes",
                columns: new[] { "Email", "CodeHash" });
        }
    }
}
