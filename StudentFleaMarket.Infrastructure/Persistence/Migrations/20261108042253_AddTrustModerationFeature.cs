using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentFleaMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrustModerationFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_TransactionId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId",
                table: "Reports");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "Reviews",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "Reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Reviews",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "Reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoidedByReportId",
                table: "Reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoidedByUserId",
                table: "Reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Reports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "Reports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "Reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                table: "Reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HiddenAt",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HiddenByUserId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HiddenReason",
                table: "Items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Category",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "TransactionConfirmations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ItemReceivedOk = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ItemCodeVerified = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionConfirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionConfirmations_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionConfirmations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TransactionId_ReviewerId",
                table: "Reviews",
                columns: new[] { "TransactionId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_VoidedByReportId",
                table: "Reviews",
                column: "VoidedByReportId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reviews_Rating",
                table: "Reviews",
                sql: "\"Rating\" >= 1 AND \"Rating\" <= 5");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ConversationId",
                table: "Reports",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_CreatedAt",
                table: "Reports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId_ConversationId",
                table: "Reports",
                columns: new[] { "ReporterId", "ConversationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId_ItemId",
                table: "Reports",
                columns: new[] { "ReporterId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId_TransactionId",
                table: "Reports",
                columns: new[] { "ReporterId", "TransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Status",
                table: "Reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_TransactionId",
                table: "Reports",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_HiddenByUserId",
                table: "Items",
                column: "HiddenByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_IsActive",
                table: "Category",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionConfirmations_TransactionId_UserId",
                table: "TransactionConfirmations",
                columns: new[] { "TransactionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionConfirmations_UserId",
                table: "TransactionConfirmations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Users_HiddenByUserId",
                table: "Items",
                column: "HiddenByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Conversations_ConversationId",
                table: "Reports",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Transactions_TransactionId",
                table: "Reports",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Reports_VoidedByReportId",
                table: "Reviews",
                column: "VoidedByReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Users_HiddenByUserId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Conversations_ConversationId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Transactions_TransactionId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Reports_VoidedByReportId",
                table: "Reviews");

            migrationBuilder.DropTable(
                name: "TransactionConfirmations");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_TransactionId_ReviewerId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_VoidedByReportId",
                table: "Reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reviews_Rating",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ConversationId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_CreatedAt",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId_ConversationId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId_ItemId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId_TransactionId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Status",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_TransactionId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Items_HiddenByUserId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Category_IsActive",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "VoidedByReportId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "VoidedByUserId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "HiddenAt",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "HiddenByUserId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "HiddenReason",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Category");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "Reviews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Reports",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "AdminNote",
                table: "Reports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TransactionId",
                table: "Reviews",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId",
                table: "Reports",
                column: "ReporterId");
        }
    }
}
