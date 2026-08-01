using Lanflix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lanflix.Migrations;

/// <summary>
/// Design-time entry point used only by EF tooling. The production Host never
/// references this project, keeping Roslyn/MSBuild helpers out of its publish.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=lanflix.db;Foreign Keys=True;Pooling=True;Default Timeout=30")
            .Options;

        return new ApplicationDbContext(options);
    }
}
