using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Lanflix.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Lanflix.Modules.Identity;

public interface IPasswordHasher
{
    Task<string> HashAsync(string password, CancellationToken cancellationToken);
    Task<bool> VerifyAsync(string encodedHash, string password, CancellationToken cancellationToken);
}

public sealed class Argon2idPasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MemorySizeKb = 64 * 1024;
    private const int Iterations = 3;
    private const int Parallelism = 2;

    public async Task<string> HashAsync(string password, CancellationToken cancellationToken)
    {
        ValidatePassword(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = await DeriveAsync(password, salt, cancellationToken);
        return $"$argon2id$v=19$m={MemorySizeKb},t={Iterations},p={Parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public async Task<bool> VerifyAsync(string encodedHash, string password, CancellationToken cancellationToken)
    {
        try
        {
            var parts = encodedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5 || !string.Equals(parts[0], "argon2id", StringComparison.Ordinal))
                return false;

            var salt = Convert.FromBase64String(parts[3]);
            var expected = Convert.FromBase64String(parts[4]);
            var actual = await DeriveAsync(password, salt, cancellationToken);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static async Task<byte[]> DeriveAsync(string password, byte[] salt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = Parallelism,
            Iterations = Iterations,
            MemorySize = MemorySizeKb
        };
        return await argon2.GetBytesAsync(HashSize);
    }

    private static void ValidatePassword(string password)
    {
        if (password.Length < 10)
            throw new ArgumentException("Password must be at least 10 characters.", nameof(password));
        if (password.Length > 256)
            throw new ArgumentException("Password is too long.", nameof(password));
    }
}

public sealed record AccountDto(Guid Id, string Username, string DisplayName, string Role, bool IsAdministrator);
public sealed record AuthTokens(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc, AccountDto Account);
public sealed record SetupStatus(bool RequiresOwnerSetup);
public sealed record InvitationResult(Guid Id, string Code, string Role, DateTime ExpiresAtUtc);

public sealed class IdentityService(
    IIdentityDbContext db,
    IPasswordHasher passwordHasher,
    IClock clock,
    IConfiguration configuration)
{
    public async Task<bool> RequiresOwnerSetupAsync(CancellationToken cancellationToken)
        => !await db.Accounts.AsNoTracking().AnyAsync(cancellationToken);

    public async Task<AuthTokens> CreateOwnerAsync(string username, string displayName, string password, string? deviceName, CancellationToken cancellationToken)
    {
        if (await db.Accounts.AnyAsync(cancellationToken))
            throw new InvalidOperationException("Owner setup has already been completed.");

        var account = Account.CreateOwner(username, displayName, await passwordHasher.HashAsync(password, cancellationToken));
        db.Accounts.Add(account);
        db.AuditRecords.Add(AuditRecord.Create(account.Id, "identity.owner-created", account.Username, null));
        await db.SaveChangesAsync(cancellationToken);
        return await IssueSessionAsync(account, deviceName, cancellationToken);
    }

    public async Task<AuthTokens?> LoginAsync(string username, string password, string? deviceName, string? ipAddress, CancellationToken cancellationToken)
    {
        var normalized = Account.NormalizeUsername(username);
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.NormalizedUsername == normalized, cancellationToken);
        if (account is null)
            return null;

        var now = clock.UtcNow;
        if (account.IsLocked(now) || !await passwordHasher.VerifyAsync(account.PasswordHash, password, cancellationToken))
        {
            account.RecordFailedLogin(now);
            db.AuditRecords.Add(AuditRecord.Create(account.Id, "identity.login-failed", account.Username, ipAddress));
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        account.RecordSuccessfulLogin(now);
        db.AuditRecords.Add(AuditRecord.Create(account.Id, "identity.login", account.Username, ipAddress));
        await db.SaveChangesAsync(cancellationToken);
        return await IssueSessionAsync(account, deviceName, cancellationToken);
    }

    public async Task<AuthTokens?> RefreshAsync(string refreshToken, string? deviceName, string? ipAddress, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshToken);
        var existing = await db.RefreshSessions.Include(x => x.Account)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (existing is null)
            return null;

        var now = clock.UtcNow;
        if (!existing.IsActive(now))
        {
            if (existing.RevokedAtUtc is not null)
            {
                var sessions = await db.RefreshSessions
                    .Where(x => x.AccountId == existing.AccountId && x.RevokedAtUtc == null)
                    .ToListAsync(cancellationToken);
                foreach (var session in sessions) session.Revoke(now);
                db.AuditRecords.Add(AuditRecord.Create(existing.AccountId, "identity.refresh-reuse", existing.DeviceName, ipAddress));
                await db.SaveChangesAsync(cancellationToken);
            }
            return null;
        }

        var replacementToken = CreateOpaqueToken();
        var replacement = RefreshSession.Create(existing.AccountId, HashToken(replacementToken), deviceName ?? existing.DeviceName, now);
        existing.Revoke(now, replacement.Id);
        db.RefreshSessions.Add(replacement);
        await db.SaveChangesAsync(cancellationToken);
        return CreateTokenResponse(existing.Account, replacementToken, now);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = HashToken(refreshToken);
        var session = await db.RefreshSessions.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (session is null || session.RevokedAtUtc is not null) return;
        session.Revoke(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AccountDto?> GetAccountAsync(Guid id, CancellationToken cancellationToken)
        => await db.Accounts.AsNoTracking().Where(x => x.Id == id).Select(x => ToDto(x)).SingleOrDefaultAsync(cancellationToken);

    public async Task<InvitationResult> CreateInvitationAsync(Guid actorId, AccountRole role, CancellationToken cancellationToken)
    {
        if (role == AccountRole.Owner) throw new ArgumentException("Owner invitations are not allowed.");
        var code = $"LFX-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}";
        var invitation = Invitation.Create(HashToken(code), role, actorId, clock.UtcNow);
        db.Invitations.Add(invitation);
        db.AuditRecords.Add(AuditRecord.Create(actorId, "identity.invitation-created", invitation.Id.ToString(), null, role.ToString()));
        await db.SaveChangesAsync(cancellationToken);
        return new InvitationResult(invitation.Id, code, role.ToString(), invitation.ExpiresAtUtc);
    }

    public async Task<AuthTokens?> RegisterAsync(string invitationCode, string username, string displayName, string password,
        string? deviceName, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var hash = HashToken(invitationCode.Trim().ToUpperInvariant());
        var invitation = await db.Invitations.SingleOrDefaultAsync(x => x.CodeHash == hash, cancellationToken);
        if (invitation is null || !invitation.IsUsable(now)) return null;
        if (await db.Accounts.AnyAsync(x => x.NormalizedUsername == Account.NormalizeUsername(username), cancellationToken))
            throw new InvalidOperationException("Username is already in use.");
        var account = Account.Create(username, displayName, await passwordHasher.HashAsync(password, cancellationToken), invitation.Role);
        invitation.MarkUsed(now);
        db.Accounts.Add(account);
        db.AuditRecords.Add(AuditRecord.Create(account.Id, "identity.account-registered", account.Username, null, invitation.Id.ToString()));
        await db.SaveChangesAsync(cancellationToken);
        return await IssueSessionAsync(account, deviceName, cancellationToken);
    }

    public async Task<bool> ChangePasswordAsync(Guid accountId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken);
        if (account is null || !await passwordHasher.VerifyAsync(account.PasswordHash, currentPassword, cancellationToken)) return false;
        account.ChangePassword(await passwordHasher.HashAsync(newPassword, cancellationToken));
        var sessions = await db.RefreshSessions.Where(x => x.AccountId == accountId && x.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var session in sessions) session.Revoke(clock.UtcNow);
        db.AuditRecords.Add(AuditRecord.Create(accountId, "identity.password-changed", account.Username, null));
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<AuthTokens> IssueSessionAsync(Account account, string? deviceName, CancellationToken cancellationToken)
    {
        var refreshToken = CreateOpaqueToken();
        db.RefreshSessions.Add(RefreshSession.Create(account.Id, HashToken(refreshToken), deviceName, clock.UtcNow));
        await db.SaveChangesAsync(cancellationToken);
        return CreateTokenResponse(account, refreshToken, clock.UtcNow);
    }

    private AuthTokens CreateTokenResponse(Account account, string refreshToken, DateTime now)
    {
        var expiry = now.AddMinutes(15);
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT signing key is not configured.");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Username),
            new Claim(ClaimTypes.Role, account.Role.ToString()),
            new Claim("permission", account.Role is AccountRole.Owner or AccountRole.Administrator ? "server.manage" : "media.use")
        };
        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"] ?? "Lanflix",
            configuration["Jwt:Audience"] ?? "LanflixClient",
            claims,
            notBefore: now,
            expires: expiry,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));

        return new AuthTokens(new JwtSecurityTokenHandler().WriteToken(token), refreshToken, expiry, ToDto(account));
    }

    private static AccountDto ToDto(Account account)
        => new(account.Id, account.Username, account.DisplayName, account.Role.ToString(), account.Role is AccountRole.Owner or AccountRole.Administrator);

    private static string CreateOpaqueToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    internal static string HashToken(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        services.AddScoped<IdentityService>();
        return services;
    }
}
