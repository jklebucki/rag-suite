using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.Orchestrator.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_sessions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_chat_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    session_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sources = table.Column<string>(type: "jsonb", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    ollama_context = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "f_k_chat_messages__chat_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_session_id",
                table: "chat_messages",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_session_timestamp",
                table: "chat_messages",
                columns: new[] { "session_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_timestamp",
                table: "chat_messages",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_chat_sessions_updated_at",
                table: "chat_sessions",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_chat_sessions_user_id",
                table: "chat_sessions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "chat_sessions");
        }
    }
}
