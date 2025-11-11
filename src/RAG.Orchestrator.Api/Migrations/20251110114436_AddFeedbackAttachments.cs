using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.Orchestrator.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feedback_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    feedback_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    data = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_feedback_attachments", x => x.id);
                    table.ForeignKey(
                        name: "f_k_feedback_attachments_feedback_entries_feedback_id",
                        column: x => x.feedback_id,
                        principalTable: "feedback",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_feedback_attachments_feedback_id",
                table: "feedback_attachments",
                column: "feedback_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feedback_attachments");
        }
    }
}
