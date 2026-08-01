using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lanflix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountPlaybackProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlaybackProgress",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaKind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    MediaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    DurationMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    Completed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybackProgress", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaybackProgress_AccountId_MediaKind_MediaId",
                table: "PlaybackProgress",
                columns: new[] { "AccountId", "MediaKind", "MediaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaybackProgress_AccountId_UpdatedAtUtc",
                table: "PlaybackProgress",
                columns: new[] { "AccountId", "UpdatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaybackProgress");
        }
    }
}
