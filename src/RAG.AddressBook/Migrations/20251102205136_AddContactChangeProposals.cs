using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.AddressBook.Migrations
{
    /// <inheritdoc />
    public partial class AddContactChangeProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactChangeProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposalType = table.Column<int>(type: "integer", nullable: false),
                    ProposedData = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProposedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ProposedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProposedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReviewedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactChangeProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactChangeProposals_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactChangeProposals_ContactId_Status",
                table: "ContactChangeProposals",
                columns: new[] { "ContactId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactChangeProposals_ProposedByUserId",
                table: "ContactChangeProposals",
                column: "ProposedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactChangeProposals_Status",
                table: "ContactChangeProposals",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactChangeProposals");
        }
    }
}
