#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace RAG.Forum.Migrations
{
    /// <inheritdoc />
    public partial class InitialForum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "forum");

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "forum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "threads",
                schema: "forum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AuthorEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastPostAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_threads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_threads_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "forum",
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "posts",
                schema: "forum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AuthorEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsAnswer = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_posts_threads_ThreadId",
                        column: x => x.ThreadId,
                        principalSchema: "forum",
                        principalTable: "threads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "forum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NotifyOnReply = table.Column<bool>(type: "boolean", nullable: false),
                    SubscribedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriptions_threads_ThreadId",
                        column: x => x.ThreadId,
                        principalSchema: "forum",
                        principalTable: "threads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                schema: "forum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attachments_posts_PostId",
                        column: x => x.PostId,
                        principalSchema: "forum",
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attachments_threads_ThreadId",
                        column: x => x.ThreadId,
                        principalSchema: "forum",
                        principalTable: "threads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "badges",
                schema: "forum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    LastSeenPostId = table.Column<Guid>(type: "uuid", nullable: true),
                    HasUnreadReplies = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_badges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_badges_posts_LastSeenPostId",
                        column: x => x.LastSeenPostId,
                        principalSchema: "forum",
                        principalTable: "posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_badges_threads_ThreadId",
                        column: x => x.ThreadId,
                        principalSchema: "forum",
                        principalTable: "threads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attachments_post_id",
                schema: "forum",
                table: "attachments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_thread_id",
                schema: "forum",
                table: "attachments",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "ix_badges_last_seen_post_id",
                schema: "forum",
                table: "badges",
                column: "LastSeenPostId");

            migrationBuilder.CreateIndex(
                name: "ix_badges_thread_user",
                schema: "forum",
                table: "badges",
                columns: new[] { "ThreadId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_order",
                schema: "forum",
                table: "categories",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "ix_categories_slug",
                schema: "forum",
                table: "categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_posts_created_at",
                schema: "forum",
                table: "posts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_posts_thread_id",
                schema: "forum",
                table: "posts",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_thread_user",
                schema: "forum",
                table: "subscriptions",
                columns: new[] { "ThreadId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_threads_category_id",
                schema: "forum",
                table: "threads",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_threads_created_at",
                schema: "forum",
                table: "threads",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_threads_last_post_at",
                schema: "forum",
                table: "threads",
                column: "LastPostAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachments",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "badges",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "posts",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "threads",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "forum");
        }
    }
}
