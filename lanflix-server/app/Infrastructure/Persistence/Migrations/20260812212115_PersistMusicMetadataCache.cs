using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lanflix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistMusicMetadataCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MusicMetadataCaches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LookupKey = table.Column<string>(type: "TEXT", maxLength: 768, nullable: false),
                    ReleaseMusicBrainzId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AlbumArtist = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    TrackListJson = table.Column<string>(type: "TEXT", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicMetadataCaches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusicMetadataCaches_LookupKey",
                table: "MusicMetadataCaches",
                column: "LookupKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicMetadataCaches_ReleaseMusicBrainzId",
                table: "MusicMetadataCaches",
                column: "ReleaseMusicBrainzId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MusicMetadataCaches");
        }
    }
}
