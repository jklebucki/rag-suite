using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.Orchestrator.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "generated_artifacts",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    session_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    assistant_message_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_generated_artifacts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_generated_artifacts_expires_at",
                table: "generated_artifacts",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_generated_artifacts_session_id",
                table: "generated_artifacts",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_generated_artifacts_user_id",
                table: "generated_artifacts",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generated_artifacts");
        }
    }
}
