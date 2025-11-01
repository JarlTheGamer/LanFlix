using Lanflix.Application.Common.Interfaces;
using Lanflix.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lanflix.WebApi.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Use test configuration - set connection strings to null to prevent SQLite registration
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = null,
                ["ConnectionStrings:PostgresConnection"] = null,
                ["Lanflix:Cache:Redis:Enabled"] = "false"
            });
        });

        // Replace the database registration AFTER the application services are configured
        builder.ConfigureServices(services =>
        {
            // Remove all existing DbContext-related registrations
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions) ||
                           d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                           d.ServiceType == typeof(ApplicationDbContext) ||
                           d.ServiceType == typeof(IApplicationDbContext))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Register IApplicationDbContext
            services.AddScoped<IApplicationDbContext>(provider => 
                provider.GetRequiredService<ApplicationDbContext>());
        });

        builder.ConfigureServices(services =>
        {
            // Build the service provider after all services are registered
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();

            // Ensure the database is created
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Seed test data
            SeedTestData(db);
        });
    }

    private static void SeedTestData(ApplicationDbContext context)
    {
        // Database is already clean from EnsureDeleted/EnsureCreated

        // Seed profiles
        var profile1 = new Domain.Entities.Profile
        {
            Id = 1,
            Name = "Test Profile",
            IsKidsProfile = false,
            Preferences = new Domain.ValueObjects.UserPreferences
            {
                PreferredAudioLanguage = "eng",
                PreferredSubtitleLanguage = "eng",
                AutoPlayNextEpisode = true,
                MaxResolution = "1080p"
            },
            CreatedAt = DateTime.UtcNow
        };

        var profile2 = new Domain.Entities.Profile
        {
            Id = 2,
            Name = "Kids Profile",
            IsKidsProfile = true,
            Preferences = new Domain.ValueObjects.UserPreferences
            {
                PreferredAudioLanguage = "eng",
                PreferredSubtitleLanguage = "eng",
                AutoPlayNextEpisode = true,
                MaxResolution = "720p"
            },
            CreatedAt = DateTime.UtcNow
        };

        context.Profiles.AddRange(profile1, profile2);

        // Seed content
        var content1 = new Domain.Entities.Content
        {
            Id = 1,
            TmdbId = 12345,
            Type = Domain.Enums.ContentType.Movie,
            Title = "Test Movie",
            OriginalTitle = "Test Movie Original",
            Overview = "A test movie for integration testing",
            FilePath = "/test/movies/test-movie.mp4",
            MediaInfo = new Domain.ValueObjects.MediaInfo
            {
                Video = new Domain.ValueObjects.VideoStream
                {
                    Codec = "h264",
                    Width = 1920,
                    Height = 1080,
                    Bitrate = 8_000_000,
                    FrameRate = 24.0,
                    PixelFormat = "yuv420p",
                    IsHDR = false
                },
                Audio = new List<Domain.ValueObjects.AudioStream>
                {
                    new()
                    {
                        Index = 0,
                        Codec = "aac",
                        Channels = 2,
                        SampleRate = 48000,
                        Bitrate = 192_000,
                        Language = "en"
                    }
                },
                Subtitles = new List<Domain.ValueObjects.SubtitleStream>(),
                Duration = TimeSpan.FromMinutes(120),
                FileSize = 2_000_000_000,
                Container = "mp4"
            },
            ReleaseDate = new DateTime(2024, 1, 1),
            Rating = 7.5,
            Genres = new[] { "Action", "Adventure" },
            AddedAt = DateTime.UtcNow
        };

        var content2 = new Domain.Entities.Content
        {
            Id = 2,
            TmdbId = 67890,
            Type = Domain.Enums.ContentType.Series,
            Title = "Test Series",
            OriginalTitle = "Test Series Original",
            Overview = "A test series for integration testing",
            FilePath = "/test/series/test-series",
            MediaInfo = new Domain.ValueObjects.MediaInfo
            {
                Video = new Domain.ValueObjects.VideoStream
                {
                    Codec = "hevc",
                    Width = 3840,
                    Height = 2160,
                    Bitrate = 20_000_000,
                    FrameRate = 24.0,
                    PixelFormat = "yuv420p10le",
                    IsHDR = true
                },
                Audio = new List<Domain.ValueObjects.AudioStream>
                {
                    new()
                    {
                        Index = 0,
                        Codec = "ac3",
                        Channels = 6,
                        SampleRate = 48000,
                        Bitrate = 640_000,
                        Language = "en"
                    }
                },
                Subtitles = new List<Domain.ValueObjects.SubtitleStream>(),
                Duration = TimeSpan.FromMinutes(45),
                FileSize = 5_000_000_000,
                Container = "mkv"
            },
            ReleaseDate = new DateTime(2024, 6, 1),
            Rating = 8.2,
            Genres = new[] { "Drama", "Thriller" },
            AddedAt = DateTime.UtcNow
        };

        context.Contents.AddRange(content1, content2);

        // Seed watch history
        var watchHistory = new Domain.Entities.WatchHistory
        {
            Id = 1,
            ProfileId = 1,
            ContentId = 1,
            PositionTicks = TimeSpan.FromMinutes(30).Ticks,
            IsCompleted = false,
            LastWatchedAt = DateTime.UtcNow.AddDays(-1)
        };

        context.WatchHistories.Add(watchHistory);

        context.SaveChanges();
    }
}
