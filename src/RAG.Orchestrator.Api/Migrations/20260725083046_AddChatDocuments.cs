using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.Orchestrator.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChatDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_documents",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    user_message_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    markdown = table.Column<string>(type: "text", nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    token_count = table.Column<int>(type: "integer", nullable: false),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_chat_documents", x => x.id);
                    table.ForeignKey(
                        name: "f_k_chat_documents__chat_messages_user_message_id",
                        column: x => x.user_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chat_documents_created_at",
                table: "chat_documents",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_chat_documents_user_message_id",
                table: "chat_documents",
                column: "user_message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_documents");
        }
    }
}
