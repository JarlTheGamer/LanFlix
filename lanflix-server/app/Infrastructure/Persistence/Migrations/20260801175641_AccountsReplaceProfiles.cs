using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lanflix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountsReplaceProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StreamSessions");

            migrationBuilder.DropTable(
                name: "WatchHistories");

            migrationBuilder.DropTable(
                name: "Watchlists");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.CreateTable(
                name: "AccountWatchlist",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountWatchlist", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientDevices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ClientType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastIpAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    LastSeenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientDevices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountWatchlist_AccountId_ContentId",
                table: "AccountWatchlist",
                columns: new[] { "AccountId", "ContentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountWatchlist_AccountId_CreatedAtUtc",
                table: "AccountWatchlist",
                columns: new[] { "AccountId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientDevices_AccountId_LastSeenAtUtc",
                table: "ClientDevices",
                columns: new[] { "AccountId", "LastSeenAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientDevices_LastSeenAtUtc",
                table: "ClientDevices",
                column: "LastSeenAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountWatchlist");

            migrationBuilder.DropTable(
                name: "ClientDevices");

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvatarPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CanDownload = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanManageSettings = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsGuest = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsKidsProfile = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PinCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Preferences = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StreamSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    EpisodeId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientIpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ClientUserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CurrentPositionTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TargetAudioCodec = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TargetBitrate = table.Column<long>(type: "INTEGER", nullable: true),
                    TargetVideoCodec = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TranscodingProcessId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
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
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    EpisodeId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastWatchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PositionTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WatchedPercentage = table.Column<double>(type: "REAL", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Watchlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
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
    }
}
