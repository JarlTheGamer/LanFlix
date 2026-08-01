using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lanflix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SocialFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SocialActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReviewId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Body = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BlockedAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialBlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialMutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MutedAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialMutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialPrivacy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DefaultVisibility = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ActivityEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialPrivacy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialRelationships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReporterAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Resolution = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ModeratedByAccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Visibility = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActivityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialComments_SocialActivities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "SocialActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActivityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialReactions_SocialActivities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "SocialActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SocialActivities_AccountId",
                table: "SocialActivities",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialActivities_CreatedAtUtc",
                table: "SocialActivities",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SocialActivities_ReviewId",
                table: "SocialActivities",
                column: "ReviewId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialBlocks_AccountId_BlockedAccountId",
                table: "SocialBlocks",
                columns: new[] { "AccountId", "BlockedAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialBlocks_BlockedAccountId",
                table: "SocialBlocks",
                column: "BlockedAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialComments_AccountId",
                table: "SocialComments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialComments_ActivityId_CreatedAtUtc",
                table: "SocialComments",
                columns: new[] { "ActivityId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SocialMutes_AccountId_MutedAccountId",
                table: "SocialMutes",
                columns: new[] { "AccountId", "MutedAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialNotifications_AccountId_ReadAtUtc_CreatedAtUtc",
                table: "SocialNotifications",
                columns: new[] { "AccountId", "ReadAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SocialPrivacy_AccountId",
                table: "SocialPrivacy",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialReactions_ActivityId_AccountId",
                table: "SocialReactions",
                columns: new[] { "ActivityId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_SourceAccountId_TargetAccountId_Kind",
                table: "SocialRelationships",
                columns: new[] { "SourceAccountId", "TargetAccountId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_TargetAccountId_Kind_Status",
                table: "SocialRelationships",
                columns: new[] { "TargetAccountId", "Kind", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_Status_CreatedAtUtc",
                table: "SocialReports",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SocialReviews_AccountId_ContentId",
                table: "SocialReviews",
                columns: new[] { "AccountId", "ContentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialReviews_ContentId",
                table: "SocialReviews",
                column: "ContentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SocialBlocks");

            migrationBuilder.DropTable(
                name: "SocialComments");

            migrationBuilder.DropTable(
                name: "SocialMutes");

            migrationBuilder.DropTable(
                name: "SocialNotifications");

            migrationBuilder.DropTable(
                name: "SocialPrivacy");

            migrationBuilder.DropTable(
                name: "SocialReactions");

            migrationBuilder.DropTable(
                name: "SocialRelationships");

            migrationBuilder.DropTable(
                name: "SocialReports");

            migrationBuilder.DropTable(
                name: "SocialReviews");

            migrationBuilder.DropTable(
                name: "SocialActivities");
        }
    }
}
