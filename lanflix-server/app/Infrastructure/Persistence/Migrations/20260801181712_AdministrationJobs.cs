using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lanflix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdministrationJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackgroundJobRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Result = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Error = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobRuns_Name_CreatedAtUtc",
                table: "BackgroundJobRuns",
                columns: new[] { "Name", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobRuns_Status",
                table: "BackgroundJobRuns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundJobRuns");
        }
    }
}
