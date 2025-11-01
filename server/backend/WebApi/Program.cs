using System.Threading.RateLimiting;
using Lanflix.Application;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Infrastructure;
using Lanflix.WebApi.Hubs;
using Lanflix.WebApi.Services;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/lanflix-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add output caching
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder
        .Expire(TimeSpan.FromMinutes(5))
        .Tag("api"));
    
    options.AddPolicy("library", builder => builder
        .Expire(TimeSpan.FromMinutes(10))
        .Tag("library")
        .SetVaryByQuery("Type", "PageNumber", "PageSize", "SearchTerm", "Genre", "SortBy", "SortDescending"));
    
    options.AddPolicy("content-details", builder => builder
        .Expire(TimeSpan.FromHours(1))
        .Tag("content"));
    
    options.AddPolicy("profiles", builder => builder
        .Expire(TimeSpan.FromMinutes(10))
        .Tag("profiles"));
});

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    // Global rate limiter
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
    
    // Streaming-specific rate limiter (max 3 concurrent streams per IP)
    options.AddPolicy("streaming", context =>
        RateLimitPartition.GetConcurrencyLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 3,
                QueueLimit = 0
            }));
});

// Add Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// SignalR for real-time communication
var signalRBuilder = builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
    options.MaximumParallelInvocationsPerClient = 1;
});

// Configure Redis backplane if enabled
var redisEnabled = builder.Configuration.GetValue<bool>("Lanflix:Cache:Redis:Enabled");
if (redisEnabled)
{
    var redisConnectionString = builder.Configuration["Lanflix:Cache:Redis:ConnectionString"];
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
        {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("lanflix:signalr:");
            options.Configuration.AbortOnConnectFail = false;
            options.Configuration.ConnectTimeout = 5000;
            options.Configuration.SyncTimeout = 5000;
            options.Configuration.KeepAlive = 60;
            options.Configuration.ConnectRetry = 3;
        });
        
        Log.Information("SignalR configured with Redis backplane: {ConnectionString}", redisConnectionString);
    }
}
else
{
    Log.Information("SignalR configured without Redis backplane (single-server mode)");
}

builder.Services.AddSingleton<IProgressBroadcaster, SignalRProgressBroadcaster>();

// CORS - SignalR requires credentials support
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:8080")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseOutputCache();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

try
{
    Log.Information("Starting Lanflix Server");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
