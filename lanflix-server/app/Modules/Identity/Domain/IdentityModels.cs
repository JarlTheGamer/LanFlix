using Lanflix.SharedKernel;

namespace Lanflix.Modules.Identity;

public enum AccountRole
{
    Child = 0,
    User = 1,
    Administrator = 2,
    Owner = 3
}

public sealed class Account : Entity<Guid>
{
    private Account() { }

    public string Username { get; private set; } = string.Empty;
    public string NormalizedUsername { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public AccountRole Role { get; private set; }
    public bool IsDisabled { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }

    public static Account CreateOwner(string username, string displayName, string passwordHash)
        => Create(username, displayName, passwordHash, AccountRole.Owner);

    public static Account Create(string username, string displayName, string passwordHash, AccountRole role)
    {
        var normalized = NormalizeUsername(username);
        if (normalized.Length is < 3 or > 64)
            throw new ArgumentException("Username must contain between 3 and 64 characters.", nameof(username));

        return new Account
        {
            Id = Guid.NewGuid(),
            Username = username.Trim(),
            NormalizedUsername = normalized,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? username.Trim() : displayName.Trim(),
            PasswordHash = passwordHash,
            Role = role
        };
    }

    public bool IsLocked(DateTime utcNow) => IsDisabled || LockedUntilUtc > utcNow;

    public void RecordSuccessfulLogin(DateTime utcNow)
    {
        FailedLoginCount = 0;
        LockedUntilUtc = null;
        LastLoginAtUtc = utcNow;
        MarkUpdated();
    }

    public void RecordFailedLogin(DateTime utcNow)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= 5)
            LockedUntilUtc = utcNow.AddMinutes(Math.Min(30, FailedLoginCount * 2));
        MarkUpdated();
    }

    public void UpdateAdministration(AccountRole role, bool disabled)
    {
        Role = role;
        IsDisabled = disabled;
        MarkUpdated();
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        FailedLoginCount = 0;
        LockedUntilUtc = null;
        MarkUpdated();
    }

    public static string NormalizeUsername(string username) => username.Trim().ToUpperInvariant();
}

public sealed class RefreshSession : Entity<Guid>
{
    private RefreshSession() { }

    public Guid AccountId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime AbsoluteExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedBySessionId { get; private set; }
    public Account Account { get; private set; } = null!;

    public bool IsActive(DateTime utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow && AbsoluteExpiresAtUtc > utcNow;

    public static RefreshSession Create(Guid accountId, string tokenHash, string? deviceName, DateTime utcNow)
        => new()
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            TokenHash = tokenHash,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "Unknown device" : deviceName.Trim(),
            ExpiresAtUtc = utcNow.AddDays(30),
            AbsoluteExpiresAtUtc = utcNow.AddDays(90)
        };

    public void Revoke(DateTime utcNow, Guid? replacementId = null)
    {
        RevokedAtUtc = utcNow;
        ReplacedBySessionId = replacementId;
        MarkUpdated();
    }
}

public sealed class Invitation : Entity<Guid>
{
    private Invitation() { }

    public string CodeHash { get; private set; } = string.Empty;
    public AccountRole Role { get; private set; }
    public Guid CreatedByAccountId { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsUsable(DateTime utcNow) => UsedAtUtc is null && RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    public static Invitation Create(string codeHash, AccountRole role, Guid createdByAccountId, DateTime utcNow)
        => new()
        {
            Id = Guid.NewGuid(),
            CodeHash = codeHash,
            Role = role,
            CreatedByAccountId = createdByAccountId,
            ExpiresAtUtc = utcNow.AddDays(7)
        };

    public void MarkUsed(DateTime utcNow)
    {
        UsedAtUtc = utcNow;
        MarkUpdated();
    }

    public void Revoke(DateTime utcNow)
    {
        RevokedAtUtc = utcNow;
        MarkUpdated();
    }
}

public sealed class AuditRecord : Entity<long>
{
    private AuditRecord() { }

    public Guid? AccountId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? Subject { get; private set; }
    public string? IpAddress { get; private set; }
    public string? DetailsJson { get; private set; }

    public static AuditRecord Create(Guid? accountId, string action, string? subject, string? ipAddress, string? detailsJson = null)
        => new()
        {
            AccountId = accountId,
            Action = action,
            Subject = subject,
            IpAddress = ipAddress,
            DetailsJson = detailsJson
        };
}
