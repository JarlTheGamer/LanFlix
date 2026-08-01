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
        
        optionsBuilder.UseSqlite("Data Source=lanflix.db;Foreign Keys=True;Pooling=True;Default Timeout=30");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
