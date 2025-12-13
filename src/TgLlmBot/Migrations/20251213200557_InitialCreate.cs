using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TgLlmBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    MessageThreadId = table.Column<int>(type: "integer", nullable: true),
                    ReplyToMessageId = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FromUserId = table.Column<long>(type: "bigint", nullable: true),
                    FromUsername = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    FromFirstName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FromLastName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Text = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    Caption = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    IsLlmReplyToMessage = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatSystemPrompts",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prompt = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSystemPrompts", x => x.ChatId);
                });

            migrationBuilder.CreateTable(
                name: "KickedUsers",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KickedUsers", x => new { x.ChatId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "Limits",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Limit = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Limits", x => new { x.ChatId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "PersonalChatSystemPrompts",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalChatSystemPrompts", x => new { x.ChatId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "Usage",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Usage = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usage", x => new { x.Date, x.ChatId, x.UserId });
                });

            migrationBuilder.CreateIndex(
                name: "idx_chathistory_chatid_date_desc",
                table: "ChatHistory",
                columns: new[] { "ChatId", "Date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_chathistory_chatid_messageid_date",
                table: "ChatHistory",
                columns: new[] { "ChatId", "MessageId", "Date" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_chathistory_messageid_chatid",
                table: "ChatHistory",
                columns: new[] { "MessageId", "ChatId" });

            migrationBuilder.CreateIndex(
                name: "IX_Usage_Date",
                table: "Usage",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatHistory");

            migrationBuilder.DropTable(
                name: "ChatSystemPrompts");

            migrationBuilder.DropTable(
                name: "KickedUsers");

            migrationBuilder.DropTable(
                name: "Limits");

            migrationBuilder.DropTable(
                name: "PersonalChatSystemPrompts");

            migrationBuilder.DropTable(
                name: "Usage");
        }
    }
}
