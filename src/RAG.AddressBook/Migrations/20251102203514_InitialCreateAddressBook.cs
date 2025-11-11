using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.AddressBook.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateAddressBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Position = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WorkPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MobilePhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactTags_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Email",
                table: "Contacts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_FirstName_LastName",
                table: "Contacts",
                columns: new[] { "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactTags_ContactId_TagName",
                table: "ContactTags",
                columns: new[] { "ContactId", "TagName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactTags");

            migrationBuilder.DropTable(
                name: "Contacts");
        }
    }
}
