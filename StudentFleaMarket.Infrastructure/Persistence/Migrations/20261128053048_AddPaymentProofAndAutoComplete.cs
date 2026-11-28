using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentProofAndAutoComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AutoCompleteAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofImageUrl",
                table: "Payments",
                type: "character varying(350000)",
                maxLength: 350000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AutoCompleteAt",
                table: "Transactions",
                column: "AutoCompleteAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AutoCompleteAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AutoCompleteAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ProofImageUrl",
                table: "Payments");
        }
    }
}
