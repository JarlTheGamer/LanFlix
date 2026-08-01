using Lanflix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lanflix.Host.Tests;

public sealed class DatabaseMigrationTests
{
    [Fact]
    public async Task Fresh_database_uses_versioned_migrations()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"lanflix-db-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "lanflix.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={path}")
                .Options;
            await using (var context = new ApplicationDbContext(options))
            {
                var migrator = new StartupDatabaseMigrator(context, NullLogger<StartupDatabaseMigrator>.Instance);
                await migrator.MigrateAsync();

                Assert.True(await context.Accounts.AnyAsync() is false);
                Assert.Contains(StartupDatabaseMigrator.BaselineMigrationId, await context.Database.GetAppliedMigrationsAsync());
                Assert.Contains("MusicTracks", await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table'").ToListAsync());
                Assert.Contains("MusicFavorites", await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table'").ToListAsync());
                Assert.Contains("SyncPlayRooms", await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table'").ToListAsync());
                Assert.Contains("SocialActivities", await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table'").ToListAsync());
                Assert.True(await context.Database.CanConnectAsync());
            }
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
