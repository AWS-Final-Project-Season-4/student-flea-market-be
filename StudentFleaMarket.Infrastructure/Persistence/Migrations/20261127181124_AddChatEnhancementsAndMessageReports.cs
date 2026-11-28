using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatEnhancementsAndMessageReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId_ConversationId",
                table: "Reports");

            migrationBuilder.AddColumn<string>(
                name: "MessageEvidence",
                table: "Reports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MessageId",
                table: "Reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PinnedAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PinnedById",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplyToMessageId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_MessageId",
                table: "Reports",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId_ConversationId",
                table: "Reports",
                columns: new[] { "ReporterId", "ConversationId" },
                unique: true,
                filter: "\"MessageId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId_MessageId",
                table: "Reports",
                columns: new[] { "ReporterId", "MessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_IsPinned_PinnedAt",
                table: "Messages",
                columns: new[] { "ConversationId", "IsPinned", "PinnedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_PinnedById",
                table: "Messages",
                column: "PinnedById");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReplyToMessageId",
                table: "Messages",
                column: "ReplyToMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Messages_ReplyToMessageId",
                table: "Messages",
                column: "ReplyToMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_PinnedById",
                table: "Messages",
                column: "PinnedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Messages_MessageId",
                table: "Reports",
                column: "MessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Messages_ReplyToMessageId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_PinnedById",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Messages_MessageId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_MessageId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId_ConversationId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId_MessageId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ConversationId_IsPinned_PinnedAt",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_PinnedById",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ReplyToMessageId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MessageEvidence",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PinnedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PinnedById",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageId",
                table: "Messages");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId_ConversationId",
                table: "Reports",
                columns: new[] { "ReporterId", "ConversationId" },
                unique: true);
        }
    }
}
