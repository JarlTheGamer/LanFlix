using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lanflix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteMusicModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MusicAlbums_ArtistId_NormalizedTitle_Year",
                table: "MusicAlbums");

            migrationBuilder.AddColumn<int>(
                name: "BitrateKbps",
                table: "MusicTracks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Channels",
                table: "MusicTracks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codec",
                table: "MusicTracks",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GenresJson",
                table: "MusicTracks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MusicBrainzId",
                table: "MusicTracks",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SampleRateHz",
                table: "MusicTracks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MusicBrainzId",
                table: "MusicArtists",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MusicBrainzId",
                table: "MusicAlbums",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MusicFavorites",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrackId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicFavorites_MusicTracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "MusicTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicLyrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrackId = table.Column<long>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    IsSynchronized = table.Column<bool>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicLyrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicLyrics_MusicTracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "MusicTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicPlayHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrackId = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    Completed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlayedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicPlayHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicPlayHistory_MusicTracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "MusicTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicQueueItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrackId = table.Column<long>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicQueueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicQueueItems_MusicTracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "MusicTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusicTracks_ArtistId",
                table: "MusicTracks",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicTracks_MusicBrainzId",
                table: "MusicTracks",
                column: "MusicBrainzId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlaylistTracks_TrackId",
                table: "MusicPlaylistTracks",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicArtists_MusicBrainzId",
                table: "MusicArtists",
                column: "MusicBrainzId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicAlbums_ArtistId_NormalizedTitle",
                table: "MusicAlbums",
                columns: new[] { "ArtistId", "NormalizedTitle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicFavorites_AccountId_TrackId",
                table: "MusicFavorites",
                columns: new[] { "AccountId", "TrackId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicFavorites_TrackId",
                table: "MusicFavorites",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicLyrics_TrackId",
                table: "MusicLyrics",
                column: "TrackId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlayHistory_AccountId_PlayedAtUtc",
                table: "MusicPlayHistory",
                columns: new[] { "AccountId", "PlayedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlayHistory_TrackId",
                table: "MusicPlayHistory",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicQueueItems_AccountId_Position",
                table: "MusicQueueItems",
                columns: new[] { "AccountId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicQueueItems_TrackId",
                table: "MusicQueueItems",
                column: "TrackId");

            migrationBuilder.AddForeignKey(
                name: "FK_MusicAlbums_MusicArtists_ArtistId",
                table: "MusicAlbums",
                column: "ArtistId",
                principalTable: "MusicArtists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MusicPlaylistTracks_MusicPlaylists_PlaylistId",
                table: "MusicPlaylistTracks",
                column: "PlaylistId",
                principalTable: "MusicPlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MusicPlaylistTracks_MusicTracks_TrackId",
                table: "MusicPlaylistTracks",
                column: "TrackId",
                principalTable: "MusicTracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MusicTracks_MusicAlbums_AlbumId",
                table: "MusicTracks",
                column: "AlbumId",
                principalTable: "MusicAlbums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MusicTracks_MusicArtists_ArtistId",
                table: "MusicTracks",
                column: "ArtistId",
                principalTable: "MusicArtists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MusicAlbums_MusicArtists_ArtistId",
                table: "MusicAlbums");

            migrationBuilder.DropForeignKey(
                name: "FK_MusicPlaylistTracks_MusicPlaylists_PlaylistId",
                table: "MusicPlaylistTracks");

            migrationBuilder.DropForeignKey(
                name: "FK_MusicPlaylistTracks_MusicTracks_TrackId",
                table: "MusicPlaylistTracks");

            migrationBuilder.DropForeignKey(
                name: "FK_MusicTracks_MusicAlbums_AlbumId",
                table: "MusicTracks");

            migrationBuilder.DropForeignKey(
                name: "FK_MusicTracks_MusicArtists_ArtistId",
                table: "MusicTracks");

            migrationBuilder.DropTable(
                name: "MusicFavorites");

            migrationBuilder.DropTable(
                name: "MusicLyrics");

            migrationBuilder.DropTable(
                name: "MusicPlayHistory");

            migrationBuilder.DropTable(
                name: "MusicQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_MusicTracks_ArtistId",
                table: "MusicTracks");

            migrationBuilder.DropIndex(
                name: "IX_MusicTracks_MusicBrainzId",
                table: "MusicTracks");

            migrationBuilder.DropIndex(
                name: "IX_MusicPlaylistTracks_TrackId",
                table: "MusicPlaylistTracks");

            migrationBuilder.DropIndex(
                name: "IX_MusicArtists_MusicBrainzId",
                table: "MusicArtists");

            migrationBuilder.DropIndex(
                name: "IX_MusicAlbums_ArtistId_NormalizedTitle",
                table: "MusicAlbums");

            migrationBuilder.DropColumn(
                name: "BitrateKbps",
                table: "MusicTracks");

            migrationBuilder.DropColumn(
                name: "Channels",
                table: "MusicTracks");

            migrationBuilder.DropColumn(
                name: "Codec",
                table: "MusicTracks");

            migrationBuilder.DropColumn(
                name: "GenresJson",
                table: "MusicTracks");

            migrationBuilder.DropColumn(
                name: "MusicBrainzId",
                table: "MusicTracks");

            migrationBuilder.DropColumn(
                name: "SampleRateHz",
                table: "MusicTracks");

            migrationBuilder.DropColumn(
                name: "MusicBrainzId",
                table: "MusicArtists");

            migrationBuilder.DropColumn(
                name: "MusicBrainzId",
                table: "MusicAlbums");

            migrationBuilder.CreateIndex(
                name: "IX_MusicAlbums_ArtistId_NormalizedTitle_Year",
                table: "MusicAlbums",
                columns: new[] { "ArtistId", "NormalizedTitle", "Year" },
                unique: true);
        }
    }
}
