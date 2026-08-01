using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lanflix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MusicFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MusicAlbums",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArtistId = table.Column<long>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    NormalizedTitle = table.Column<string>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: true),
                    ArtworkPath = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicAlbums", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MusicArtists",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    ArtworkPath = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicArtists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MusicPlaylists",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicPlaylists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MusicPlaylistTracks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlaylistId = table.Column<long>(type: "INTEGER", nullable: false),
                    TrackId = table.Column<long>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicPlaylistTracks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MusicTracks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArtistId = table.Column<long>(type: "INTEGER", nullable: false),
                    AlbumId = table.Column<long>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", nullable: false),
                    TrackNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    FileModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicTracks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusicAlbums_ArtistId_NormalizedTitle_Year",
                table: "MusicAlbums",
                columns: new[] { "ArtistId", "NormalizedTitle", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicArtists_NormalizedName",
                table: "MusicArtists",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlaylists_AccountId_Name",
                table: "MusicPlaylists",
                columns: new[] { "AccountId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlaylistTracks_PlaylistId_Position",
                table: "MusicPlaylistTracks",
                columns: new[] { "PlaylistId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlaylistTracks_PlaylistId_TrackId",
                table: "MusicPlaylistTracks",
                columns: new[] { "PlaylistId", "TrackId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicTracks_AlbumId_DiscNumber_TrackNumber",
                table: "MusicTracks",
                columns: new[] { "AlbumId", "DiscNumber", "TrackNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_MusicTracks_FilePath",
                table: "MusicTracks",
                column: "FilePath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MusicAlbums");

            migrationBuilder.DropTable(
                name: "MusicArtists");

            migrationBuilder.DropTable(
                name: "MusicPlaylists");

            migrationBuilder.DropTable(
                name: "MusicPlaylistTracks");

            migrationBuilder.DropTable(
                name: "MusicTracks");
        }
    }
}
