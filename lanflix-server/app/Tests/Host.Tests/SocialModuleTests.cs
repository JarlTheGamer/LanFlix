using Lanflix.Infrastructure.Persistence;
using Lanflix.Modules.Social;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lanflix.Host.Tests;

public sealed class SocialModuleTests
{
    [Fact]
    public async Task Friend_visibility_requires_acceptance_and_blocks_override_it()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("Data Source=:memory:").Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
        var authorId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var activity = SocialActivity.Post(authorId, "A private-to-friends update", SocialVisibility.Friends);
        var friendship = SocialRelationship.Create(authorId, viewerId, RelationshipKind.Friend);
        db.SocialActivities.Add(activity);
        db.SocialRelationships.Add(friendship);
        await db.SaveChangesAsync();

        Assert.False(await SocialEndpointSupport.CanViewAsync(db, activity, viewerId, CancellationToken.None));
        friendship.Accept(viewerId);
        await db.SaveChangesAsync();
        Assert.True(await SocialEndpointSupport.CanViewAsync(db, activity, viewerId, CancellationToken.None));

        db.SocialBlocks.Add(SocialBlock.Create(viewerId, authorId));
        await db.SaveChangesAsync();
        Assert.False(await SocialEndpointSupport.CanViewAsync(db, activity, viewerId, CancellationToken.None));
    }

    [Fact]
    public async Task Reviews_reactions_notifications_and_reports_are_persisted_with_constraints()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("Data Source=:memory:").Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
        var authorId = Guid.NewGuid();
        var reactorId = Guid.NewGuid();
        var review = SocialReview.Create(authorId, 42, 5, "Excellent", SocialVisibility.Server);
        var activity = SocialActivity.Review(authorId, review);
        var reaction = SocialReaction.Create(activity.Id, reactorId, "love");
        var notification = SocialNotification.Create(authorId, reactorId, "reaction", "activity", activity.Id.ToString());
        var report = SocialReport.Create(reactorId, "review", review.Id.ToString(), "Needs moderator review");
        db.SocialReviews.Add(review); db.SocialActivities.Add(activity); db.SocialReactions.Add(reaction);
        db.SocialNotifications.Add(notification); db.SocialReports.Add(report);
        await db.SaveChangesAsync();

        notification.MarkRead();
        report.Resolve(Guid.NewGuid(), false, "Reviewed and retained");
        await db.SaveChangesAsync();

        Assert.Equal(5, (await db.SocialReviews.SingleAsync()).Rating);
        Assert.Equal("love", (await db.SocialReactions.SingleAsync()).Kind);
        Assert.NotNull((await db.SocialNotifications.SingleAsync()).ReadAtUtc);
        Assert.Equal(ReportStatus.Resolved, (await db.SocialReports.SingleAsync()).Status);
        Assert.Throws<ArgumentOutOfRangeException>(() => SocialReview.Create(authorId, 42, 6, "Invalid", SocialVisibility.Server));
    }
}
