using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Lanflix.Application;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Infrastructure;
using Lanflix.Infrastructure.Services.SyncPlay;
using Lanflix.WebApi.Authorization;
using Lanflix.WebApi.Helpers;
using Lanflix.WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Lanflix.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLanflixHealthChecks(this IServiceCollection services)
    {
        var healthChecks = services.AddHealthChecks()
            .AddDbContextCheck<Lanflix.Infrastructure.Persistence.ApplicationDbContext>("database")
            .AddCheck<Lanflix.Infrastructure.HealthChecks.FFmpegHealthCheck>("ffmpeg")
            .AddCheck<Lanflix.Infrastructure.HealthChecks.DiskSpaceHealthCheck>("disk-space");

        return services;
    }

    public static IServiceCollection AddLanflixCoreServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddMemoryCache();
        services.AddOpenApi();
        services.AddHttpContextAccessor();
        
        return services;
    }

    public static IServiceCollection AddLanflixCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            
            // MIME types to compress
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
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

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        });

        return services;
    }

    public static IServiceCollection AddLanflixAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:Key"] 
                        ?? throw new InvalidOperationException("JWT Key not configured"))),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
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

        return services;
    }

    public static IServiceCollection AddLanflixAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Owner", "Administrator"));

            options.AddPolicy("ServerManage", policy =>
                policy.RequireClaim("permission", "server.manage"));
            
            options.AddPolicy("ProfileOwner", policy =>
                policy.Requirements.Add(new ProfileOwnerRequirement()));
        });

        services.AddSingleton<IAuthorizationHandler, ProfileAuthorizationHandler>();

        return services;
    }

    public static IServiceCollection AddLanflixOutputCaching(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
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

        return services;
    }

    public static IServiceCollection AddLanflixRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
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
                    ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous"
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
                    ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous"
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

        return services;
    }

    public static IServiceCollection AddLanflixSignalR(this IServiceCollection services, IWebHostEnvironment environment)
    {
        var signalRBuilder = services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = environment.IsDevelopment();
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.HandshakeTimeout = TimeSpan.FromSeconds(15);
            options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
            options.MaximumParallelInvocationsPerClient = 1;
        });

        Log.Information("SignalR configured for the single-process server");

        services.AddSingleton<IProgressBroadcaster, SignalRProgressBroadcaster>();
        services.AddSingleton<ISyncPlayRoomService, SyncPlayRoomService>();

        return services;
    }

    public static IServiceCollection AddLanflixCors(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            // Default policy for development
            options.AddDefaultPolicy(policy =>
            {
                var allowedOrigins = configuration.GetSection("Lanflix:Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] 
                    { 
                        "http://localhost:3000",      // React dev server
                        "http://localhost:5173",      // Vite dev server
                        "http://localhost:8080",      // Vue dev server
                        "http://localhost:4200"       // Angular dev server
                    };
                
                // In development, allow local network access
                if (environment.IsDevelopment())
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
                var productionOrigins = configuration.GetSection("Lanflix:Cors:ProductionOrigins").Get<string[]>()
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

        return services;
    }

    public static IServiceCollection AddLanflixServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddLanflixHealthChecks();
        services.AddLanflixCoreServices();
        services.AddLanflixCompression();
        services.AddLanflixAuthentication(configuration);
        services.AddLanflixAuthorization();
        services.AddLanflixOutputCaching();
        services.AddLanflixRateLimiting();
        
        // Add Clean Architecture layers
        services.AddApplication();
        services.AddInfrastructure(configuration);
        
        services.AddLanflixSignalR(environment);
        services.AddLanflixCors(configuration, environment);

        return services;
    }
}
