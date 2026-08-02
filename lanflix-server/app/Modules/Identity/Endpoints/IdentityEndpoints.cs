using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Modules.Identity;

public sealed record OwnerSetupRequest(string Username, string DisplayName, string Password, string? DeviceName);
public sealed record LoginRequest(string Username, string Password, string? DeviceName);
public sealed record RefreshRequest(string RefreshToken, string? DeviceName);
public sealed record LogoutRequest(string RefreshToken);
public sealed record InvitationRegistrationRequest(string InvitationCode, string Username, string DisplayName, string Password, string? DeviceName);
public sealed record CreateInvitationRequest(AccountRole Role);
public sealed record UpdateAccountAdministrationRequest(AccountRole Role, bool Disabled);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityModule(this IEndpointRouteBuilder endpoints)
    {
        var setup = endpoints.MapGroup("/api/v2/setup").WithTags("Setup");
        setup.MapGet("/status", async (IdentityService identity, CancellationToken ct)
            => Results.Ok(new SetupStatus(await identity.RequiresOwnerSetupAsync(ct))));
        setup.MapPost("/owner", CreateOwnerAsync).RequireRateLimiting("strict");

        var auth = endpoints.MapGroup("/api/v2/auth").WithTags("Authentication");
        auth.MapPost("/login", LoginAsync).RequireRateLimiting("strict");
        auth.MapPost("/refresh", RefreshAsync).RequireRateLimiting("strict");
        auth.MapPost("/logout", async (LogoutRequest request, IdentityService identity, CancellationToken ct) =>
        {
            await identity.RevokeAsync(request.RefreshToken, ct);
            return Results.NoContent();
        });
        auth.MapPost("/register", RegisterAsync).RequireRateLimiting("strict");

        endpoints.MapGet("/api/v2/accounts/me", GetCurrentAccountAsync)
            .WithTags("Accounts")
            .RequireAuthorization();
        endpoints.MapPost("/api/v2/accounts/me/password", ChangePasswordAsync).WithTags("Accounts").RequireAuthorization();
        endpoints.MapGet("/api/v2/accounts/me/sessions", ListSessionsAsync).WithTags("Accounts").RequireAuthorization();
        endpoints.MapDelete("/api/v2/accounts/me/sessions/{id:guid}", RevokeSessionAsync).WithTags("Accounts").RequireAuthorization();
        endpoints.MapPost("/api/v2/accounts/me/avatar", UploadAvatarAsync).WithTags("Accounts").RequireAuthorization().DisableAntiforgery();
        endpoints.MapPost("/api/v2/accounts/me/backdrop", UploadBackdropAsync).WithTags("Accounts").RequireAuthorization().DisableAntiforgery();
        endpoints.MapGet("/api/v2/accounts/{id:guid}/avatar", ServeAvatarAsync).WithTags("Accounts").AllowAnonymous();
        endpoints.MapGet("/api/v2/accounts/{id:guid}/backdrop", ServeBackdropAsync).WithTags("Accounts").AllowAnonymous();

        var admin = endpoints.MapGroup("/api/v2/admin/identity").WithTags("Account administration").RequireAuthorization("AdminOnly");
        admin.MapGet("/accounts", ListAccountsAsync);
        admin.MapPut("/accounts/{id:guid}", UpdateAccountAsync);
        admin.MapGet("/invitations", ListInvitationsAsync);
        admin.MapPost("/invitations", CreateInvitationAsync);
        admin.MapDelete("/invitations/{id:guid}", RevokeInvitationAsync);
        admin.MapGet("/audit", GetAuditAsync);
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(InvitationRegistrationRequest request, IdentityService identity, CancellationToken ct)
    {
        try
        {
            var result = await identity.RegisterAsync(request.InvitationCode, request.Username, request.DisplayName, request.Password, request.DeviceName, ct);
            return result is null ? Results.Problem(statusCode: 400, title: "Invitation is invalid or expired") : Results.Ok(result);
        }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: "Invalid account", detail: exception.Message); }
        catch (InvalidOperationException exception) { return Results.Problem(statusCode: 409, title: exception.Message); }
    }

    private static async Task<IResult> ChangePasswordAsync(ChangePasswordRequest request, ClaimsPrincipal user, IdentityService identity, CancellationToken ct)
    {
        try
        {
            return await identity.ChangePasswordAsync(AccountId(user), request.CurrentPassword, request.NewPassword, ct)
                ? Results.NoContent() : Results.Problem(statusCode: 400, title: "Current password is incorrect");
        }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
    }

    private static async Task<IResult> ListSessionsAsync(ClaimsPrincipal user, IIdentityDbContext db, CancellationToken ct)
    {
        var id = AccountId(user);
        return Results.Ok(await db.RefreshSessions.AsNoTracking().Where(x => x.AccountId == id && x.RevokedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc).Select(x => new { x.Id, x.DeviceName, x.CreatedAtUtc, x.ExpiresAtUtc, x.AbsoluteExpiresAtUtc }).ToListAsync(ct));
    }

    private static async Task<IResult> RevokeSessionAsync(Guid id, ClaimsPrincipal user, IIdentityDbContext db, CancellationToken ct)
    {
        var session = await db.RefreshSessions.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == AccountId(user), ct);
        if (session is null) return Results.NotFound(); session.Revoke(DateTime.UtcNow); await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> UploadAvatarAsync(HttpContext context, ClaimsPrincipal user, CancellationToken ct)
    {
        var id = AccountId(user);
        var dir = Path.Combine(AppContext.BaseDirectory, "config", "avatars");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{id:N}_pfp.jpg");

        if (context.Request.HasFormContentType && context.Request.Form.Files.Count > 0)
        {
            var file = context.Request.Form.Files[0];
            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream, ct);
        }
        else
        {
            await using var stream = File.Create(filePath);
            await context.Request.Body.CopyToAsync(stream, ct);
        }
        return Results.Ok(new { avatarUrl = $"/api/v2/accounts/{id}/avatar?t={DateTime.UtcNow.Ticks}" });
    }

    private static async Task<IResult> UploadBackdropAsync(HttpContext context, ClaimsPrincipal user, CancellationToken ct)
    {
        var id = AccountId(user);
        var dir = Path.Combine(AppContext.BaseDirectory, "config", "avatars");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{id:N}_backdrop.jpg");

        if (context.Request.HasFormContentType && context.Request.Form.Files.Count > 0)
        {
            var file = context.Request.Form.Files[0];
            await using var stream = File.Create(filePath);
            await file.CopyToAsync(stream, ct);
        }
        else
        {
            await using var stream = File.Create(filePath);
            await context.Request.Body.CopyToAsync(stream, ct);
        }
        return Results.Ok(new { backdropUrl = $"/api/v2/accounts/{id}/backdrop?t={DateTime.UtcNow.Ticks}" });
    }

    private static IResult ServeAvatarAsync(Guid id, HttpContext context)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "config", "avatars", $"{id:N}_pfp.jpg");
        if (!File.Exists(filePath)) return Results.NotFound();
        var fileInfo = new FileInfo(filePath);
        context.Response.Headers.ETag = $"\"{fileInfo.Length:x}-{fileInfo.LastWriteTimeUtc.Ticks:x}\"";
        return Results.File(filePath, "image/jpeg");
    }

    private static IResult ServeBackdropAsync(Guid id, HttpContext context)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "config", "avatars", $"{id:N}_backdrop.jpg");
        if (!File.Exists(filePath)) return Results.NotFound();
        var fileInfo = new FileInfo(filePath);
        context.Response.Headers.ETag = $"\"{fileInfo.Length:x}-{fileInfo.LastWriteTimeUtc.Ticks:x}\"";
        return Results.File(filePath, "image/jpeg");
    }

    private static async Task<IResult> ListAccountsAsync(IIdentityDbContext db, CancellationToken ct) => Results.Ok(
        await db.Accounts.AsNoTracking().OrderBy(x => x.DisplayName).Select(x => new
        { x.Id, x.Username, x.DisplayName, role = x.Role.ToString(), x.IsDisabled, x.LastLoginAtUtc, x.CreatedAtUtc }).ToListAsync(ct));

    private static async Task<IResult> UpdateAccountAsync(Guid id, UpdateAccountAdministrationRequest request,
        ClaimsPrincipal user, IIdentityDbContext db, CancellationToken ct)
    {
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (account is null) return Results.NotFound();
        if (account.Role == AccountRole.Owner && (request.Role != AccountRole.Owner || request.Disabled))
            return Results.Problem(statusCode: 409, title: "The owner account cannot be disabled or demoted");
        if (id == AccountId(user) && request.Disabled) return Results.Problem(statusCode: 409, title: "You cannot disable your own account");
        account.UpdateAdministration(request.Role, request.Disabled);
        if (request.Disabled)
        {
            var sessions = await db.RefreshSessions.Where(x => x.AccountId == id && x.RevokedAtUtc == null).ToListAsync(ct);
            foreach (var session in sessions) session.Revoke(DateTime.UtcNow);
        }
        db.AuditRecords.Add(AuditRecord.Create(AccountId(user), "identity.account-updated", id.ToString(), null, $"{request.Role}:{request.Disabled}"));
        await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> ListInvitationsAsync(IIdentityDbContext db, CancellationToken ct) => Results.Ok(
        await db.Invitations.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(200).Select(x => new
        { x.Id, role = x.Role.ToString(), x.CreatedByAccountId, x.ExpiresAtUtc, x.UsedAtUtc, x.RevokedAtUtc, x.CreatedAtUtc }).ToListAsync(ct));

    private static async Task<IResult> CreateInvitationAsync(CreateInvitationRequest request, ClaimsPrincipal user, IdentityService identity, CancellationToken ct)
    {
        try { return Results.Ok(await identity.CreateInvitationAsync(AccountId(user), request.Role, ct)); }
        catch (ArgumentException exception) { return Results.Problem(statusCode: 400, title: exception.Message); }
    }

    private static async Task<IResult> RevokeInvitationAsync(Guid id, ClaimsPrincipal user, IIdentityDbContext db, CancellationToken ct)
    {
        var invitation = await db.Invitations.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (invitation is null) return Results.NotFound(); invitation.Revoke(DateTime.UtcNow);
        db.AuditRecords.Add(AuditRecord.Create(AccountId(user), "identity.invitation-revoked", id.ToString(), null));
        await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> GetAuditAsync(int? offset, int? limit, IIdentityDbContext db, CancellationToken ct) => Results.Ok(
        await db.AuditRecords.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Skip(Math.Max(offset ?? 0, 0)).Take(Math.Clamp(limit ?? 100, 1, 500))
            .Select(x => new { x.Id, x.AccountId, x.Action, x.Subject, x.IpAddress, x.DetailsJson, x.CreatedAtUtc }).ToListAsync(ct));

    private static Guid AccountId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    private static async Task<IResult> CreateOwnerAsync(OwnerSetupRequest request, IdentityService identity, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await identity.CreateOwnerAsync(request.Username, request.DisplayName, request.Password, request.DeviceName, ct));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(statusCode: 400, title: "Invalid owner account", detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(statusCode: 409, title: "Setup already completed", detail: exception.Message);
        }
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, HttpContext context, IdentityService identity, CancellationToken ct)
    {
        var result = await identity.LoginAsync(request.Username, request.Password, request.DeviceName, context.Connection.RemoteIpAddress?.ToString(), ct);
        return result is null
            ? Results.Problem(statusCode: 401, title: "Authentication failed", detail: "The username or password is invalid, or the account is locked.")
            : Results.Ok(result);
    }

    private static async Task<IResult> RefreshAsync(RefreshRequest request, HttpContext context, IdentityService identity, CancellationToken ct)
    {
        var result = await identity.RefreshAsync(request.RefreshToken, request.DeviceName, context.Connection.RemoteIpAddress?.ToString(), ct);
        return result is null
            ? Results.Problem(statusCode: 401, title: "Session expired", detail: "Sign in again to continue.")
            : Results.Ok(result);
    }

    [Authorize]
    private static async Task<IResult> GetCurrentAccountAsync(ClaimsPrincipal user, IdentityService identity, CancellationToken ct)
    {
        var rawId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(rawId, out var accountId)) return Results.Unauthorized();
        var account = await identity.GetAccountAsync(accountId, ct);
        return account is null ? Results.NotFound() : Results.Ok(account);
    }
}
