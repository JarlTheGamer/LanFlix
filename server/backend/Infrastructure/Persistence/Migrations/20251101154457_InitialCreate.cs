using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lanflix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TmdbId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OriginalTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Overview = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PosterPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BackdropPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Rating = table.Column<double>(type: "REAL", nullable: true),
                    Genres = table.Column<string>(type: "TEXT", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MediaInfo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AvatarPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsKidsProfile = table.Column<bool>(type: "INTEGER", nullable: false),
                    PinCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Preferences = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Episodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    TmdbId = table.Column<int>(type: "INTEGER", nullable: true),
                    SeasonNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    EpisodeNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Overview = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    AirDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StillPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MediaInfo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Episodes_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Watchlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Watchlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Watchlists_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Watchlists_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StreamSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    EpisodeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    TranscodingProcessId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClientIpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ClientUserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CurrentPositionTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TargetBitrate = table.Column<long>(type: "INTEGER", nullable: true),
                    TargetVideoCodec = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TargetAudioCodec = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamSessions_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StreamSessions_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StreamSessions_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WatchHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    EpisodeId = table.Column<int>(type: "INTEGER", nullable: true),
                    PositionTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastWatchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WatchedPercentage = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WatchHistories_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WatchHistories_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WatchHistories_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contents_AddedAt",
                table: "Contents",
                column: "AddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Contents_IsDeleted",
                table: "Contents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Contents_Title",
                table: "Contents",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Contents_TmdbId",
                table: "Contents",
                column: "TmdbId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contents_Type",
                table: "Contents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Contents_Type_AddedAt",
                table: "Contents",
                columns: new[] { "Type", "AddedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_ContentId",
                table: "Episodes",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_ContentId_SeasonNumber_EpisodeNumber",
                table: "Episodes",
                columns: new[] { "ContentId", "SeasonNumber", "EpisodeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_IsDeleted",
                table: "Episodes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_TmdbId",
                table: "Episodes",
                column: "TmdbId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_IsDefault",
                table: "Profiles",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Name",
                table: "Profiles",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_StreamSessions_ContentId",
                table: "StreamSessions",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamSessions_EpisodeId",
                table: "StreamSessions",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamSessions_IsActive",
                table: "StreamSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_StreamSessions_LastActivityAt",
                table: "StreamSessions",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_StreamSessions_ProfileId",
                table: "StreamSessions",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamSessions_ProfileId_IsActive",
                table: "StreamSessions",
                columns: new[] { "ProfileId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_StreamSessions_SessionId",
                table: "StreamSessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StreamSessions_StartedAt",
                table: "StreamSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_ContentId",
                table: "WatchHistories",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_EpisodeId",
                table: "WatchHistories",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_LastWatchedAt",
                table: "WatchHistories",
                column: "LastWatchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_ProfileId",
                table: "WatchHistories",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_ProfileId_ContentId_EpisodeId",
                table: "WatchHistories",
                columns: new[] { "ProfileId", "ContentId", "EpisodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_ProfileId_LastWatchedAt",
                table: "WatchHistories",
                columns: new[] { "ProfileId", "LastWatchedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_AddedAt",
                table: "Watchlists",
                column: "AddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_ContentId",
                table: "Watchlists",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_ProfileId",
                table: "Watchlists",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_ProfileId_AddedAt",
                table: "Watchlists",
                columns: new[] { "ProfileId", "AddedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_ProfileId_ContentId",
                table: "Watchlists",
                columns: new[] { "ProfileId", "ContentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StreamSessions");

            migrationBuilder.DropTable(
                name: "WatchHistories");

            migrationBuilder.DropTable(
                name: "Watchlists");

            migrationBuilder.DropTable(
                name: "Episodes");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "Contents");
        }
    }
}
