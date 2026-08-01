using Lanflix.Infrastructure.Persistence;
using Lanflix.Modules.Realtime;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lanflix.Host.Tests;

public sealed class SyncPlayPersistenceTests
{
    [Fact]
    public async Task Room_and_playback_state_survive_a_context_restart()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lanflix-syncplay-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite($"Data Source={path}").Options;
        var hostId = Guid.NewGuid();
        try
        {
            await using (var first = new ApplicationDbContext(options))
            {
                await first.Database.EnsureCreatedAsync();
                var room = SyncPlayRoom.Create("SYNC-ABC123", hostId, 42, "movie", null);
                room.Synchronize(125.5, true, 1.25);
                first.SyncPlayRooms.Add(room);
                await first.SaveChangesAsync();
            }

            await using (var restarted = new ApplicationDbContext(options))
            {
                var room = await restarted.SyncPlayRooms.AsNoTracking().SingleAsync();
                Assert.Equal("SYNC-ABC123", room.Code);
                Assert.Equal(hostId, room.HostAccountId);
                Assert.Equal(125.5, room.PositionSeconds);
                Assert.True(room.IsPlaying);
                Assert.Equal(1.25, room.PlaybackRate);
                Assert.True(room.ExpiresAtUtc > DateTime.UtcNow.AddHours(23));
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
