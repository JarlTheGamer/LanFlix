using System.Text;
using System.Threading.RateLimiting;
using Lanflix.Application;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Infrastructure;
using Lanflix.Infrastructure.Services.Authentication;
using Lanflix.Infrastructure.Telemetry;
using Lanflix.WebApi.Authentication;
using Lanflix.WebApi.Authorization;
using Lanflix.WebApi.Helpers;
using Lanflix.WebApi.Hubs;
using Lanflix.WebApi.Middleware;
using Lanflix.WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

// Extract embedded config files on first run (Minecraft-style)
EmbeddedResourceExtractor.ExtractConfigFiles();

var builder = WebApplication.CreateBuilder(args);

// Add persistent configuration file (survives updates)
var configDir = Path.Combine(AppContext.BaseDirectory, "config");
Directory.CreateDirectory(configDir);
builder.Configuration.AddJsonFile(Path.Combine(configDir, "lanflix.json"), optional: true, reloadOnChange: true);

// Configure Kestrel for HTTP/2 and HTTP/3 support
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Enable HTTP/2
    serverOptions.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
    });
    
    // Configure limits for optimal performance
    serverOptions.Limits.MaxConcurrentConnections = 1000;
    serverOptions.Limits.MaxConcurrentUpgradedConnections = 1000;
    serverOptions.Limits.MaxRequestBodySize = 2_147_483_648; // 2GB for large file uploads
    serverOptions.Limits.MinRequestBodyDataRate = new Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate(
        bytesPerSecond: 100,
        gracePeriod: TimeSpan.FromSeconds(10));
    serverOptions.Limits.MinResponseDataRate = new Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate(
        bytesPerSecond: 100,
        gracePeriod: TimeSpan.FromSeconds(10));
    
    // HTTP/2 specific settings
    serverOptions.Limits.Http2.MaxStreamsPerConnection = 100;
    serverOptions.Limits.Http2.HeaderTableSize = 4096;
    serverOptions.Limits.Http2.MaxFrameSize = 16384;
    serverOptions.Limits.Http2.MaxRequestHeaderFieldSize = 8192;
    serverOptions.Limits.Http2.InitialConnectionWindowSize = 131072;
    serverOptions.Limits.Http2.InitialStreamWindowSize = 98304;
    
    // Keep-alive settings
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// Configure Serilog with structured logging and sensitive data redaction
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "Lanflix.Server")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .Enrich.With<Lanflix.Infrastructure.Logging.SensitiveDataRedactionEnricher>()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure OpenTelemetry
var serviceName = "Lanflix.Server";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["host.name"] = Environment.MachineName
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = (httpContext) =>
            {
                // Don't trace health check endpoints
                return !httpContext.Request.Path.StartsWithSegments("/health");
            };
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.SetDbStatementForStoredProcedure = true;
        })
        .AddSource(LanflixActivitySource.Streaming.Name)
        .AddSource(LanflixActivitySource.Transcoding.Name)
        .AddSource(LanflixActivitySource.Library.Name)
        // Add OTLP exporter for production (e.g., to Jaeger, Zipkin, or Application Insights)
        //.AddOtlpExporter(options =>
        //{
        //    options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");
        //})
    )
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Lanflix.Streaming")
        .AddMeter("Lanflix.Caching")
        // Add OTLP exporter for production
        //.AddOtlpExporter(options =>
        //{
        //    options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");
        //})
    );

// Register custom metrics
builder.Services.AddSingleton<StreamingMetrics>();
builder.Services.AddSingleton<CachingMetrics>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Lanflix.Infrastructure.Persistence.ApplicationDbContext>("database")
    .AddCheck<Lanflix.Infrastructure.HealthChecks.FFmpegHealthCheck>("ffmpeg")
    .AddCheck<Lanflix.Infrastructure.HealthChecks.DiskSpaceHealthCheck>("disk-space");

// Add Redis health check if enabled
if (builder.Configuration.GetValue<bool>("Lanflix:Cache:Redis:Enabled"))
{
    var redisConnectionString = builder.Configuration["Lanflix:Cache:Redis:ConnectionString"];
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        builder.Services.AddHealthChecks()
            .AddRedis(redisConnectionString, name: "redis", tags: new[] { "cache" });
    }
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

// Response compression for improved performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    
    // MIME types to compress
    options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
        new[]
        {
            "application/json",
            "application/xml",
            "text/plain",
            "text/css",
            "text/html",
            "application/javascript",
            "text/javascript",
            "image/svg+xml"
        });
});

// Configure compression levels
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// Register legacy token service for backward compatibility
builder.Services.AddSingleton<ILegacyTokenService, LegacyTokenService>();

// Authentication & Authorization with hybrid JWT support
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddScheme<JwtBearerOptions, HybridJwtBearerHandler>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] 
                    ?? throw new InvalidOperationException("JWT Key not configured"))),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        
        // Configure JWT authentication for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("ProfileOwner", policy =>
        policy.Requirements.Add(new ProfileOwnerRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, ProfileAuthorizationHandler>();

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
    // Global rate limiter - 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
    
    // Streaming-specific rate limiter (max 3 concurrent streams per user/IP)
    options.AddPolicy("streaming", context =>
    {
        var partitionKey = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("ProfileId")?.Value ?? "anonymous"
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
        return RateLimitPartition.GetConcurrencyLimiter(
            partitionKey: partitionKey,
            factory: _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 3,
                QueueLimit = 0
            });
    });
    
    // Per-user rate limiter for API calls - 200 requests per minute per authenticated user
    options.AddPolicy("per-user", context =>
    {
        var partitionKey = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("ProfileId")?.Value ?? "anonymous"
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1)
            });
    });
    
    // Strict rate limiter for sensitive operations (e.g., login, admin operations)
    options.AddPolicy("strict", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
    
    // Configure rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        
        double? retryAfterSeconds = null;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = retryAfter.TotalSeconds;
            context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString();
        }
        
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            statusCode = 429,
            message = "Too many requests. Please try again later.",
            retryAfter = retryAfterSeconds
        }, cancellationToken);
    };
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
        try
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
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to configure Redis backplane for SignalR. Falling back to single-server mode.");
        }
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
    // Default policy for development
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Lanflix:Cors:AllowedOrigins").Get<string[]>()
            ?? new[] 
            { 
                "http://localhost:3000",      // React dev server
                "http://localhost:5173",      // Vite dev server
                "http://localhost:8080",      // Vue dev server
                "http://localhost:4200"       // Angular dev server
            };
        
        // In development, allow local network access
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                
                // Allow configured origins
                if (allowedOrigins.Contains(origin)) return true;
                
                // Allow any localhost with any port
                if (origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:")) return true;
                
                // Allow local network IPs (192.168.x.x, 10.x.x.x, 172.16-31.x.x)
                try
                {
                    var uri = new Uri(origin);
                    var host = uri.Host;
                    
                    if (System.Net.IPAddress.TryParse(host, out var ip))
                    {
                        var bytes = ip.GetAddressBytes();
                        if (bytes.Length == 4) // IPv4
                        {
                            // 192.168.x.x
                            if (bytes[0] == 192 && bytes[1] == 168) return true;
                            // 10.x.x.x
                            if (bytes[0] == 10) return true;
                            // 172.16.x.x - 172.31.x.x
                            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                        }
                    }
                }
                catch
                {
                    // Invalid URI, deny
                }
                
                return false;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition", "X-Pagination");
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()             // Required for SignalR and JWT cookies
                  .WithExposedHeaders("Content-Disposition", "X-Pagination"); // Expose custom headers
        }
    });
    
    // Strict policy for production (can be used with [EnableCors("Production")])
    options.AddPolicy("Production", policy =>
    {
        var productionOrigins = builder.Configuration.GetSection("Lanflix:Cors:ProductionOrigins").Get<string[]>()
            ?? Array.Empty<string>();
        
        if (productionOrigins.Length > 0)
        {
            policy.WithOrigins(productionOrigins)
                  .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
                  .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                  .AllowCredentials()
                  .WithExposedHeaders("Content-Disposition", "X-Pagination")
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        }
        else
        {
            // If no production origins configured, allow all (not recommended for production)
            Log.Warning("No production CORS origins configured. Allowing all origins.");
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
    
    // Policy for public endpoints (no credentials)
    options.AddPolicy("Public", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Exception handling middleware (must be first)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// API version detection removed - no longer needed

// app.UseHttpsRedirection(); // Disabled - using HTTP only

// Response compression (before static files and routing)
app.UseResponseCompression();

// Serve static files from wwwroot (frontend build output)
app.UseStaticFiles();

// Note: Posters and backdrops are stored in media folders, not in a separate cache
// They will be served via the /media endpoint below

// Serve media files directly from media folders
var moviesPath = builder.Configuration["Lanflix:MediaPaths:Movies"];
var seriesPath = builder.Configuration["Lanflix:MediaPaths:Series"];

// Create a combined media root if either path is configured
if (!string.IsNullOrEmpty(moviesPath) || !string.IsNullOrEmpty(seriesPath))
{
    // Find common parent directory or use the first available path
    var mediaRoot = !string.IsNullOrEmpty(moviesPath) && Directory.Exists(moviesPath)
        ? Path.GetDirectoryName(moviesPath) ?? moviesPath
        : !string.IsNullOrEmpty(seriesPath) && Directory.Exists(seriesPath)
            ? Path.GetDirectoryName(seriesPath) ?? seriesPath
            : null;

    if (!string.IsNullOrEmpty(mediaRoot))
    {
        // Convert to absolute path if relative
        if (!Path.IsPathRooted(mediaRoot))
        {
            mediaRoot = Path.GetFullPath(mediaRoot);
        }
        
        if (Directory.Exists(mediaRoot))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(mediaRoot),
                RequestPath = "/media",
                ServeUnknownFileTypes = true,
                OnPrepareResponse = ctx =>
                {
                    // Set appropriate content type for images and videos
                    if (ctx.File.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        ctx.File.Name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Context.Response.ContentType = "image/jpeg";
                    }
                    else if (ctx.File.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Context.Response.ContentType = "image/png";
                    }
                    else if (ctx.File.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Context.Response.ContentType = "video/mp4";
                        ctx.Context.Response.Headers["Accept-Ranges"] = "bytes";
                    }
                    else if (ctx.File.Name.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Context.Response.ContentType = "video/x-matroska";
                        ctx.Context.Response.Headers["Accept-Ranges"] = "bytes";
                    }
                }
            });
            
            // Also serve videos directly from the videos path for the /videos/ controller
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(mediaRoot),
                RequestPath = "/videos",
                ServeUnknownFileTypes = true,
                OnPrepareResponse = ctx =>
                {
                    // Set video content types and enable range requests (like Chrome does)
                    var extension = Path.GetExtension(ctx.File.Name).ToLowerInvariant();
                    ctx.Context.Response.ContentType = extension switch
                    {
                        ".mp4" => "video/mp4",
                        ".mkv" => "video/x-matroska",
                        ".avi" => "video/x-msvideo",
                        ".mov" => "video/quicktime",
                        ".wmv" => "video/x-ms-wmv",
                        ".webm" => "video/webm",
                        _ => "video/mp4"
                    };
                    ctx.Context.Response.Headers["Accept-Ranges"] = "bytes";
                    ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=3600";
                }
            });
            Log.Information("Serving media files from: {Path}", mediaRoot);
        }
    }
}

app.UseCors();

// Authentication & Authorization (order matters!)
app.UseAuthentication();
app.UseAuthorization();

// Legacy middleware removed - frontend now served from same origin

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// SPA fallback routing - serve index.html for all non-API routes
// This allows frontend routing (React Router, Vue Router, etc.) to work
// Exclude /api and /hubs routes from fallback
app.MapFallback(context =>
{
    // Don't serve index.html for API or hub requests
    if (context.Request.Path.StartsWithSegments("/api") || 
        context.Request.Path.StartsWithSegments("/hubs"))
    {
        context.Response.StatusCode = 404;
        return Task.CompletedTask;
    }
    
    // Serve index.html for all other routes (SPA routing)
    context.Response.ContentType = "text/html";
    return context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "index.html"));
});

// Map health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration,
                data = e.Value.Data,
                exception = e.Value.Exception?.Message
            })
        }, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await context.Response.WriteAsync(result);
    }
});

// Simple health check endpoint for load balancers
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") || check.Name == "database"
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Just checks if the app is running
});

try
{
    // Display ASCII art banner
    var banner = @"
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║   ██╗      █████╗ ███╗   ██╗███████╗██╗     ██╗██╗  ██╗       ║
║   ██║     ██╔══██╗████╗  ██║██╔════╝██║     ██║╚██╗██╔╝       ║
║   ██║     ███████║██╔██╗ ██║█████╗  ██║     ██║ ╚███╔╝        ║
║   ██║     ██╔══██║██║╚██╗██║██╔══╝  ██║     ██║ ██╔██╗        ║
║   ███████╗██║  ██║██║ ╚████║██║     ███████╗██║██╔╝ ██╗       ║
║   ╚══════╝╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝     ╚══════╝╚═╝╚═╝  ╚═╝       ║
║                                                               ║
║              Media Streaming Server - v2.0.0                  ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
";
    
    Console.WriteLine(banner);
    Log.Information("Starting Lanflix Server v2.0.0");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    
    // Automatically apply database migrations and seed initial data
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<Lanflix.Infrastructure.Persistence.ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Lanflix.Infrastructure.Persistence.DatabaseSeeder>>();
        
        // Ensure database is created (creates tables if they don't exist)
        Log.Information("Ensuring database exists...");
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database is ready");
        
        // Seed initial data
        var seeder = new Lanflix.Infrastructure.Persistence.DatabaseSeeder(context, logger);
        await seeder.SeedAsync();
        
        // Initialize default media folders on first run
        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var folderLogger = scope.ServiceProvider.GetRequiredService<ILogger<Lanflix.Infrastructure.Services.Settings.MediaFolderInitializer>>();
        var folderInitializer = new Lanflix.Infrastructure.Services.Settings.MediaFolderInitializer(
            builder.Configuration,
            settingsService,
            folderLogger);
        await folderInitializer.InitializeAsync();
    }
    
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

// Make the implicit Program class accessible to integration tests
public partial class Program { }
