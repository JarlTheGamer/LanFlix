using Microsoft.EntityFrameworkCore;

namespace Lanflix.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets will be added here
    // public DbSet<Content> Contents => Set<Content>();
    // public DbSet<Profile> Profiles => Set<Profile>();
    // public DbSet<WatchHistory> WatchHistories => Set<WatchHistory>();
    // public DbSet<StreamSession> StreamSessions => Set<StreamSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
