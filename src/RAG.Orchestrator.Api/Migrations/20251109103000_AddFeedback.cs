using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.Orchestrator.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    response = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    response_author_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_feedback", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_feedback_created_at",
                table: "feedback",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_feedback_subject",
                table: "feedback",
                column: "subject");

            migrationBuilder.CreateIndex(
                name: "ix_feedback_user_id",
                table: "feedback",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feedback");
        }
    }
}

