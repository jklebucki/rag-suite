using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.Orchestrator.Api.Migrations
{
    /// <inheritdoc />
    public partial class UseResponderEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "response_author_id",
                table: "feedback");

            migrationBuilder.AddColumn<string>(
                name: "response_author_email",
                table: "feedback",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "response_author_email",
                table: "feedback");

            migrationBuilder.AddColumn<string>(
                name: "response_author_id",
                table: "feedback",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);
        }
    }
}
