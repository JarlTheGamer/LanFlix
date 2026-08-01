using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lanflix.Modules.Realtime;

public sealed class SyncPlayConnectionRegistry
{
    private readonly ConcurrentDictionary<string, string> _rooms = new();

    public void Join(string connectionId, string code) => _rooms[connectionId] = code;
    public bool TryGetRoom(string connectionId, out string code) => _rooms.TryGetValue(connectionId, out code!);
    public bool Leave(string connectionId, out string code) => _rooms.TryRemove(connectionId, out code!);
}

internal sealed class SyncPlayRoomCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<SyncPlayRoomCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IRealtimeDbContext>();
            var removed = await db.SyncPlayRooms
                .Where(room => room.ExpiresAtUtc <= DateTime.UtcNow)
                .ExecuteDeleteAsync(stoppingToken);
            if (removed > 0) logger.LogInformation("Removed {Count} expired SyncPlay rooms", removed);
        }
    }
}

public static class RealtimeModule
{
    public static IServiceCollection AddRealtimeModule(this IServiceCollection services)
    {
        services.AddSingleton<SyncPlayConnectionRegistry>();
        services.AddHostedService<SyncPlayRoomCleanupService>();
        return services;
    }
}
