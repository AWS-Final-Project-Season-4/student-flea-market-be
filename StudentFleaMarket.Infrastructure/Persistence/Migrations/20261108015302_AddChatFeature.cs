using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_BuyerId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_ItemId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_SellerId",
                table: "Conversations");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuyerUnreadCount",
                table: "Conversations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SellerUnreadCount",
                table: "Conversations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_SentAt",
                table: "Messages",
                columns: new[] { "ConversationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_BuyerId_LastMessageAt",
                table: "Conversations",
                columns: new[] { "BuyerId", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ItemId_BuyerId",
                table: "Conversations",
                columns: new[] { "ItemId", "BuyerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_SellerId_LastMessageAt",
                table: "Conversations",
                columns: new[] { "SellerId", "LastMessageAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ConversationId_SentAt",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_BuyerId_LastMessageAt",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_ItemId_BuyerId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_SellerId_LastMessageAt",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "BuyerUnreadCount",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "SellerUnreadCount",
                table: "Conversations");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_BuyerId",
                table: "Conversations",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ItemId",
                table: "Conversations",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_SellerId",
                table: "Conversations",
                column: "SellerId");
        }
    }
}
