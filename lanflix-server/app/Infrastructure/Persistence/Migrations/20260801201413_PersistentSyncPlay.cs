using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lanflix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistentSyncPlay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncPlayRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    HostAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    EpisodeId = table.Column<int>(type: "INTEGER", nullable: true),
                    PositionSeconds = table.Column<double>(type: "REAL", nullable: false),
                    IsPlaying = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlaybackRate = table.Column<double>(type: "REAL", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncPlayRooms", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncPlayRooms_Code",
                table: "SyncPlayRooms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncPlayRooms_ExpiresAtUtc",
                table: "SyncPlayRooms",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SyncPlayRooms_HostAccountId",
                table: "SyncPlayRooms",
                column: "HostAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncPlayRooms");
        }
    }
}
