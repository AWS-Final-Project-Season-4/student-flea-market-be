using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeMeetingSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoCancelled",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledConfirmedAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "ExchangeRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Type_Status_ScheduledAt",
                table: "Transactions",
                columns: new[] { "Type", "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRequests_ConversationId",
                table: "ExchangeRequests",
                column: "ConversationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExchangeRequests_Conversations_ConversationId",
                table: "ExchangeRequests",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExchangeRequests_Conversations_ConversationId",
                table: "ExchangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Type_Status_ScheduledAt",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeRequests_ConversationId",
                table: "ExchangeRequests");

            migrationBuilder.DropColumn(
                name: "AutoCancelled",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ScheduledConfirmedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "ExchangeRequests");
        }
    }
}
