using Lanflix.Infrastructure.Persistence;
using Lanflix.Modules.Identity;
using Lanflix.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lanflix.Host.Tests;

public sealed class IdentityInvitationTests
{
    [Fact]
    public async Task Invitation_creates_one_account_once_and_issues_account_tokens()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("Data Source=:memory:").Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-only-signing-key-that-is-long-enough-for-hmac-sha256",
            ["Jwt:Issuer"] = "Lanflix.Tests",
            ["Jwt:Audience"] = "Lanflix.Tests.Client"
        }).Build();
        var service = new IdentityService(db, new Argon2idPasswordHasher(), new SystemClock(), configuration);
        var owner = await service.CreateOwnerAsync("owner", "Owner", "Owner password 123!", "Test", CancellationToken.None);
        var invitation = await service.CreateInvitationAsync(owner.Account.Id, AccountRole.User, CancellationToken.None);

        var registered = await service.RegisterAsync(invitation.Code.ToLowerInvariant(), "viewer", "Viewer",
            "Viewer password 123!", "Phone", CancellationToken.None);
        var reused = await service.RegisterAsync(invitation.Code, "another", "Another",
            "Another password 123!", "Phone", CancellationToken.None);

        Assert.NotNull(registered);
        Assert.Equal("User", registered!.Account.Role);
        Assert.False(string.IsNullOrWhiteSpace(registered.AccessToken));
        Assert.Null(reused);
        Assert.Equal(2, await db.Accounts.CountAsync());
        Assert.NotNull((await db.Invitations.SingleAsync()).UsedAtUtc);
    }
}
