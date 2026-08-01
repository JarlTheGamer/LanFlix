using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lanflix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ArtworkPalettes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArtworkPalettes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Base = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    Depth = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    Glow = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    Accent = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    OnBackground = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    AlgorithmVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceLength = table.Column<long>(type: "INTEGER", nullable: false),
                    SourceLastWriteUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkPalettes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkPalettes_ContentId",
                table: "ArtworkPalettes",
                column: "ContentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtworkPalettes");
        }
    }
}
