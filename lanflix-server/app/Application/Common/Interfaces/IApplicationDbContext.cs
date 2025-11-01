using Lanflix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Content> Contents { get; }
    DbSet<Episode> Episodes { get; }
    DbSet<Profile> Profiles { get; }
    DbSet<WatchHistory> WatchHistories { get; }
    DbSet<Watchlist> Watchlists { get; }
    DbSet<StreamSession> StreamSessions { get; }
    DbSet<ServerSetting> ServerSettings { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
