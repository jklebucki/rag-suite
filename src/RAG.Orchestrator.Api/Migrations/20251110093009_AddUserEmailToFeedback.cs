using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.Orchestrator.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEmailToFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "p_k_feedback",
                table: "feedback");

            migrationBuilder.AddColumn<string>(
                name: "user_email",
                table: "feedback",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "p_k_feedback_entries",
                table: "feedback",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "p_k_feedback_entries",
                table: "feedback");

            migrationBuilder.DropColumn(
                name: "user_email",
                table: "feedback");

            migrationBuilder.AddPrimaryKey(
                name: "p_k_feedback",
                table: "feedback",
                column: "id");
        }
    }
}
