using Lanflix.Application.Common.Interfaces;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Infrastructure.Services.Settings;
using Lanflix.WebApi.Hubs;
using Lanflix.WebApi.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Lanflix.WebApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseLanflixPipeline(this WebApplication app)
    {
        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // Exception handling middleware (must be first)
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Response compression (before static files and routing)
        app.UseResponseCompression();

        return app;
    }

    public static WebApplication UseLanflixStaticFiles(this WebApplication app, IConfiguration configuration)
    {
        // Serve static files from wwwroot (frontend build output) with no-cache headers for HTML/JS/CSS to ensure instant UI updates
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                var path = ctx.File.Name.ToLowerInvariant();
                if (path.EndsWith(".html") || path.EndsWith(".js") || path.EndsWith(".css") || path.EndsWith(".json"))
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                }
            }
        });

        // Serve media files directly from media folders
        var moviesPath = configuration["Lanflix:MediaPaths:Movies"];
        var seriesPath = configuration["Lanflix:MediaPaths:Series"];

        // Create a combined media root if either path is configured
        // Disabled by default: media must flow through authorized range/stream endpoints.
        // This switch exists only for short-lived v1 rollback during the staged migration.
        if (configuration.GetValue<bool>("Lanflix:Compatibility:EnableRawMediaStaticFiles")
            && (!string.IsNullOrEmpty(moviesPath) || !string.IsNullOrEmpty(seriesPath)))
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
        
        return app;
    }

    public static WebApplication UseLanflixAuth(this WebApplication app)
    {
        app.UseCors();

        // Authentication & Authorization (order matters!)
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapLanflixEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapHub<NotificationHub>("/hubs/notifications");
        app.MapHub<SyncPlayHub>("/hubs/syncplay");

        // SPA fallback routing - serve index.html for all non-API routes
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
        app.MapHealthChecks("/health", new HealthCheckOptions
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
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready") || check.Name == "database"
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false // Just checks if the app is running
        });

        return app;
    }

    public static async Task InitializeLanflixDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<Lanflix.Infrastructure.Persistence.ApplicationDbContext>();
        var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Lanflix.Infrastructure.Persistence.StartupDatabaseMigrator>>();
        var migrator = new Lanflix.Infrastructure.Persistence.StartupDatabaseMigrator(context, migrationLogger);
        await migrator.MigrateAsync(app.Lifetime.ApplicationStopping);

        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var folderLogger = scope.ServiceProvider.GetRequiredService<ILogger<Lanflix.Infrastructure.Services.Settings.MediaFolderInitializer>>();
        var folderInitializer = new Lanflix.Infrastructure.Services.Settings.MediaFolderInitializer(
            app.Configuration,
            settingsService,
            folderLogger);
        await folderInitializer.InitializeAsync();
        await settingsService.EnsureConfigFileExistsAsync();
    }
}
