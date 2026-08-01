using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Modules.Social;

internal static class SocialEndpointSupport
{
    public static Guid AccountId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(value, out var accountId) ? accountId : throw new UnauthorizedAccessException();
    }

    public static async Task<bool> CanInteractAsync(ISocialDbContext db, Guid actorId, Guid targetId, CancellationToken ct) =>
        actorId != targetId && !await db.SocialBlocks.AnyAsync(block =>
            (block.AccountId == actorId && block.BlockedAccountId == targetId) ||
            (block.AccountId == targetId && block.BlockedAccountId == actorId), ct);

    public static async Task<bool> CanViewAsync(ISocialDbContext db, SocialActivity activity, Guid viewerId, CancellationToken ct)
    {
        if (activity.AccountId == viewerId) return true;
        if (!await CanInteractAsync(db, viewerId, activity.AccountId, ct)) return false;
        return activity.Visibility switch
        {
            SocialVisibility.Private => false,
            SocialVisibility.Server or SocialVisibility.Household => true,
            SocialVisibility.Friends => await db.SocialRelationships.AnyAsync(rel => rel.Kind == RelationshipKind.Friend
                && rel.Status == RelationshipStatus.Accepted
                && ((rel.SourceAccountId == viewerId && rel.TargetAccountId == activity.AccountId)
                    || (rel.SourceAccountId == activity.AccountId && rel.TargetAccountId == viewerId)), ct),
            _ => false
        };
    }

    public static SocialAuthorDto Author(Guid id, IReadOnlyDictionary<Guid, SocialAccountDto> accounts) =>
        accounts.TryGetValue(id, out var account) ? new(account.Id, account.DisplayName) : new(id, "Unknown account");

    public static async Task<SocialNotificationDto> NotifyAsync(
        ISocialDbContext db, ISocialResourceDirectory directory, ISocialNotificationPublisher publisher,
        Guid accountId, Guid actorId, string kind, string resourceType, string resourceId, CancellationToken ct)
    {
        var entity = SocialNotification.Create(accountId, actorId, kind, resourceType, resourceId);
        db.SocialNotifications.Add(entity);
        await db.SaveChangesAsync(ct);
        var accounts = await directory.GetAccountsAsync([actorId], ct);
        var dto = new SocialNotificationDto(entity.Id, Author(actorId, accounts), kind, resourceType, resourceId, false, entity.CreatedAtUtc);
        await publisher.PublishAsync(accountId, dto, ct);
        return dto;
    }
}
