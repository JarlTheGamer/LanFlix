using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lanflix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LiveTvFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LiveTvSources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SourceUri = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    GuideUri = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    MaxTuners = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastRefreshedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveTvSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiveTvChannels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceId = table.Column<long>(type: "INTEGER", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LogoUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    StreamUri = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    GroupName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveTvChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveTvChannels_LiveTvSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "LiveTvSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiveTvFavorites",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveTvFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveTvFavorites_LiveTvChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "LiveTvChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiveTvPrograms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    EpisodeTitle = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ArtworkUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    StartsAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveTvPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveTvPrograms_LiveTvChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "LiveTvChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiveTvTunerLeases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveTvTunerLeases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveTvTunerLeases_LiveTvChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "LiveTvChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LiveTvTunerLeases_LiveTvSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "LiveTvSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiveTvChannels_Enabled_Number",
                table: "LiveTvChannels",
                columns: new[] { "Enabled", "Number" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveTvChannels_SourceId_ExternalId",
                table: "LiveTvChannels",
                columns: new[] { "SourceId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiveTvFavorites_AccountId_ChannelId",
                table: "LiveTvFavorites",
                columns: new[] { "AccountId", "ChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiveTvFavorites_ChannelId",
                table: "LiveTvFavorites",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveTvPrograms_ChannelId_ExternalId_StartsAtUtc",
                table: "LiveTvPrograms",
                columns: new[] { "ChannelId", "ExternalId", "StartsAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiveTvPrograms_ChannelId_StartsAtUtc_EndsAtUtc",
                table: "LiveTvPrograms",
                columns: new[] { "ChannelId", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveTvTunerLeases_AccountId",
                table: "LiveTvTunerLeases",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveTvTunerLeases_ChannelId",
                table: "LiveTvTunerLeases",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveTvTunerLeases_SourceId_ExpiresAtUtc",
                table: "LiveTvTunerLeases",
                columns: new[] { "SourceId", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveTvFavorites");

            migrationBuilder.DropTable(
                name: "LiveTvPrograms");

            migrationBuilder.DropTable(
                name: "LiveTvTunerLeases");

            migrationBuilder.DropTable(
                name: "LiveTvChannels");

            migrationBuilder.DropTable(
                name: "LiveTvSources");
        }
    }
}
