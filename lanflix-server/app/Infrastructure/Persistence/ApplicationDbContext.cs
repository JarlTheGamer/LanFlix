using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using Lanflix.Domain.Interfaces;
using Lanflix.Modules.Identity;
using Lanflix.Modules.Metadata;
using Lanflix.Modules.Playback;
using Lanflix.Modules.Devices;
using Lanflix.Modules.Library;
using Lanflix.Modules.Administration;
using Lanflix.Modules.Music;
using Lanflix.Modules.LiveTV;
using Lanflix.Modules.Realtime;
using Lanflix.Modules.Social;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext, IIdentityDbContext, IArtworkPaletteDbContext, IPlaybackDbContext, IDevicesDbContext, ILibraryDbContext, IAdministrationDbContext, IMusicDbContext, ILiveTvDbContext, IRealtimeDbContext, ISocialDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Content> Contents => Set<Content>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<ServerSetting> ServerSettings => Set<ServerSetting>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();
    public DbSet<ArtworkPalette> ArtworkPalettes => Set<ArtworkPalette>();
    public DbSet<PlaybackProgress> PlaybackProgress => Set<PlaybackProgress>();
    public DbSet<ClientDevice> ClientDevices => Set<ClientDevice>();
    public DbSet<AccountWatchlistItem> AccountWatchlist => Set<AccountWatchlistItem>();
    public DbSet<BackgroundJobRun> BackgroundJobRuns => Set<BackgroundJobRun>();
    public DbSet<MusicArtist> MusicArtists => Set<MusicArtist>();
    public DbSet<MusicAlbum> MusicAlbums => Set<MusicAlbum>();
    public DbSet<MusicTrack> MusicTracks => Set<MusicTrack>();
    public DbSet<MusicPlaylist> MusicPlaylists => Set<MusicPlaylist>();
    public DbSet<MusicPlaylistTrack> MusicPlaylistTracks => Set<MusicPlaylistTrack>();
    public DbSet<MusicFavorite> MusicFavorites => Set<MusicFavorite>();
    public DbSet<MusicPlayHistory> MusicPlayHistory => Set<MusicPlayHistory>();
    public DbSet<MusicQueueItem> MusicQueueItems => Set<MusicQueueItem>();
    public DbSet<MusicLyrics> MusicLyrics => Set<MusicLyrics>();
    public DbSet<LiveTvSource> LiveTvSources => Set<LiveTvSource>();
    public DbSet<LiveTvChannel> LiveTvChannels => Set<LiveTvChannel>();
    public DbSet<LiveTvProgram> LiveTvPrograms => Set<LiveTvProgram>();
    public DbSet<LiveTvFavorite> LiveTvFavorites => Set<LiveTvFavorite>();
    public DbSet<LiveTvTunerLease> LiveTvTunerLeases => Set<LiveTvTunerLease>();
    public DbSet<SyncPlayRoom> SyncPlayRooms => Set<SyncPlayRoom>();
    public DbSet<SocialRelationship> SocialRelationships => Set<SocialRelationship>();
    public DbSet<SocialReview> SocialReviews => Set<SocialReview>();
    public DbSet<SocialActivity> SocialActivities => Set<SocialActivity>();
    public DbSet<SocialComment> SocialComments => Set<SocialComment>();
    public DbSet<SocialReaction> SocialReactions => Set<SocialReaction>();
    public DbSet<SocialBlock> SocialBlocks => Set<SocialBlock>();
    public DbSet<SocialMute> SocialMutes => Set<SocialMute>();
    public DbSet<SocialPrivacy> SocialPrivacy => Set<SocialPrivacy>();
    public DbSet<SocialNotification> SocialNotifications => Set<SocialNotification>();
    public DbSet<SocialReport> SocialReports => Set<SocialReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountConfiguration).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArtworkPaletteConfiguration).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlaybackProgressConfiguration).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClientDeviceConfiguration).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountWatchlistItemConfiguration).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BackgroundJobRunConfiguration).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MusicArtistConfiguration).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LiveTvSourceConfiguration).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SyncPlayRoomConfiguration).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SocialRelationshipConfiguration).Assembly);

        // Apply global query filters for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var filter = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false)),
                    parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit timestamps
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        // Handle soft deletes
        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
