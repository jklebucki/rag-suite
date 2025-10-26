using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.CyberPanel.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageToQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Quizzes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "Quizzes");
        }
    }
}
