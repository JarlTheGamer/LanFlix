using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lanflix.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating ApplicationDbContext instances during migrations
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Check if PostgreSQL connection string is provided via environment variable
        var postgresConnection = Environment.GetEnvironmentVariable("LANFLIX_POSTGRES_CONNECTION");
        
        if (!string.IsNullOrEmpty(postgresConnection))
        {
            // Use PostgreSQL for design-time (migrations)
            optionsBuilder.UseNpgsql(postgresConnection);
        }
        else
        {
            // Default to SQLite for design-time (migrations)
            optionsBuilder.UseSqlite("Data Source=lanflix.db");
        }

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
