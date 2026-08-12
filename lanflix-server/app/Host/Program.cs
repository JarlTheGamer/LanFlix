using System.Text;
using System.Threading.RateLimiting;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application;
using Lanflix.Infrastructure.Services.FFmpeg;
using Lanflix.Host;
using Lanflix.Infrastructure.Adapters;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Infrastructure.Services.ExternalApis;
using Lanflix.Infrastructure.Services.Settings;
using Lanflix.Modules.Identity;
using Lanflix.Modules.Downloads;
using Lanflix.Modules.Devices;
using Lanflix.Modules.Administration;
using Lanflix.Modules.Library;
using Lanflix.Modules.Metadata;
using Lanflix.Modules.Playback;
using Lanflix.Modules.Discovery;
using Lanflix.Modules.Subtitles;
using Lanflix.Modules.Realtime;
using Lanflix.Modules.Music;
using Lanflix.Modules.LiveTV;
using Lanflix.Modules.Social;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

LanflixBanner.Write();
var builder = WebApplication.CreateBuilder(args);
var applicationDirectory = AppContext.BaseDirectory;
var configurationDirectory = Path.Combine(applicationDirectory, "config");
Directory.CreateDirectory(configurationDirectory);
var generatedConfiguration = MediaEnvironment.Ensure(applicationDirectory, configurationDirectory);
builder.Configuration
    .AddJsonFile(generatedConfiguration, optional: false, reloadOnChange: true)
    .AddJsonFile(PersistentSecretConfiguration.Ensure(configurationDirectory), optional: false, reloadOnChange: false);

builder.Host.UseSerilog((context, _, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Lanflix.Host"));

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddResponseCompression();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache(options => options.SizeLimit = 2_048);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=lanflix.db;Pooling=True";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString, sqlite => sqlite.CommandTimeout(30)));
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IIdentityDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IArtworkPaletteDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IPlaybackDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IDevicesDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<ILibraryDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IAdministrationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IMusicDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<ILiveTvDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IRealtimeDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<ISocialDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<StartupDatabaseMigrator>();
builder.Services.AddApplication();
builder.Services.AddScoped<IHardwareAccelerationDetector, EnhancedHardwareAccelerationDetector>();
// v2 playback uses the managed FFmpeg planner/session services registered by
// the infrastructure adapters; the legacy pipe-based pipeline is not exposed.
builder.Services.AddScoped<TranscodingSettingsProvider>();

builder.Services.AddIdentityModule();
builder.Services.AddMetadataModule();
builder.Services.AddPlaybackModule();
builder.Services.AddDevicesModule();
builder.Services.AddAdministrationModule();
builder.Services.AddLiveTvModule();
builder.Services.AddRealtimeModule();
builder.Services.AddSocialModule();
builder.Services.AddLanflixModuleAdapters();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddTransient<TmdbRateLimitHandler>();
builder.Services.AddHttpClient<ITmdbClient, TmdbClient>(client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.Timeout = TimeSpan.FromSeconds(20);
}).AddHttpMessageHandler<TmdbRateLimitHandler>();
builder.Services.AddHttpClient("LiveTvMetadata", client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpClient("LiveTvStream", client => client.Timeout = Timeout.InfiniteTimeSpan);

var signingKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT signing key is missing from the external secrets file.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Lanflix",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "LanflixClient",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Owner", "Administrator"));
    options.AddPolicy("ServerManage", policy => policy.RequireClaim("permission", "server.manage"));
});
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("strict", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            AutoReplenishment = true,
            QueueLimit = 0
        }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        (uri.IsLoopback || IsPrivateLan(uri.Host)))
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database");

var app = builder.Build();
app.UseExceptionHandler();
app.UseResponseCompression();
app.UseMiddleware<Lanflix.Host.Middleware.ETagMiddleware>();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapIdentityModule();
app.MapDownloadsModule();
app.MapDevicesModule();
app.MapAdministrationModule();
app.MapApplicationUpdatesModule();
app.MapLibraryModule();
app.MapPlaybackModule();
app.MapDiscoveryModule();
app.MapSubtitlesModule();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<SyncPlayHub>("/hubs/syncplay");
app.MapMusicModule();
app.MapLiveTvModule();
app.MapSocialModule();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<StartupDatabaseMigrator>().MigrateAsync();

await app.RunAsync();

static bool IsPrivateLan(string host)
{
    if (!System.Net.IPAddress.TryParse(host, out var address)) return host.EndsWith(".local", StringComparison.OrdinalIgnoreCase);
    var bytes = address.GetAddressBytes();
    return bytes.Length == 4 && (bytes[0] == 10 || bytes[0] == 127 ||
        (bytes[0] == 192 && bytes[1] == 168) ||
        (bytes[0] == 172 && bytes[1] is >= 16 and <= 31));
}

public partial class Program;
