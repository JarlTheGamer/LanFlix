using Lanflix.Infrastructure.Persistence;
using Lanflix.Modules.Realtime;
using Lanflix.Modules.Social;
using Lanflix.Modules.Playback;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Infrastructure.Adapters.Social;

internal sealed class SocialResourceDirectory(ApplicationDbContext db) : ISocialResourceDirectory
{
    public Task<bool> AccountExistsAsync(Guid accountId, CancellationToken cancellationToken) =>
        db.Accounts.AsNoTracking().AnyAsync(x => x.Id == accountId && !x.IsDisabled, cancellationToken);

    public Task<bool> MediaExistsAsync(int contentId, CancellationToken cancellationToken) =>
        db.Contents.AsNoTracking().AnyAsync(x => x.Id == contentId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, SocialAccountDto>> GetAccountsAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken)
    {
        var ids = accountIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, SocialAccountDto>();
        return await db.Accounts.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Select(x => new SocialAccountDto(x.Id, x.DisplayName, x.Role.ToString()))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }
}

internal sealed class SignalRSocialNotificationPublisher(IHubContext<NotificationHub> hub) : ISocialNotificationPublisher
{
    public Task PublishAsync(Guid accountId, SocialNotificationDto notification, CancellationToken cancellationToken) =>
        hub.Clients.Group(NotificationHub.AccountGroup(accountId)).SendAsync("NotificationReceived", notification, cancellationToken);
}

internal sealed class PlaybackActivityRecorder(ApplicationDbContext db) : IPlaybackActivityRecorder
{
    public async Task RecordCompletedAsync(Guid accountId, string kind, int mediaId, CancellationToken cancellationToken)
    {
        // Episodes store the series content id on the activity, so its detail
        // page shows all completed episodes together. Movies already use it.
        var contentId = kind == "episode"
            ? await db.Episodes.AsNoTracking().Where(x => x.Id == mediaId).Select(x => (int?)x.ContentId).SingleOrDefaultAsync(cancellationToken)
            : mediaId;
        if (contentId is null || contentId <= 0) return;
        if (await db.SocialActivities.AnyAsync(x => x.AccountId == accountId && x.Kind == "watch" && x.ContentId == contentId, cancellationToken)) return;
        db.SocialActivities.Add(SocialActivity.Watch(accountId, contentId.Value));
        await db.SaveChangesAsync(cancellationToken);
    }
}
