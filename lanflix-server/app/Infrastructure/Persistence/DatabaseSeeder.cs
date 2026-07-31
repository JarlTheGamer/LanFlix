using Lanflix.Domain.Entities;
using Lanflix.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Persistence;

/// <summary>
/// Seeds the database with initial data
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seed initial data if database is empty
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Check if we need to seed profiles
            var hasProfiles = await _context.Profiles.AnyAsync();
            
            if (!hasProfiles)
            {
                _logger.LogInformation("No profiles found. Creating default profile...");
                
                var defaultProfile = new Profile
                {
                    Name = "Default",
                    IsKidsProfile = false,
                    Preferences = new UserPreferences
                    {
                        PreferredAudioLanguage = "en",
                        PreferredSubtitleLanguage = "en",
                        AutoPlayNextEpisode = true,
                        MaxResolution = "1080p"
                    },
                    CreatedAt = DateTime.UtcNow
                };

                _context.Profiles.Add(defaultProfile);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Default profile created successfully");
            }

            // Ensure a Guest profile exists
            var hasGuestProfile = await _context.Profiles.AnyAsync(p => p.IsGuest);
            if (!hasGuestProfile)
            {
                _logger.LogInformation("Creating default Guest profile...");
                var guestProfile = new Profile
                {
                    Name = "Guest",
                    IsKidsProfile = false,
                    IsGuest = true,
                    CanDownload = false,
                    CanManageSettings = false,
                    Preferences = new UserPreferences
                    {
                        PreferredAudioLanguage = "en",
                        PreferredSubtitleLanguage = "en",
                        AutoPlayNextEpisode = true,
                        MaxResolution = "1080p"
                    },
                    CreatedAt = DateTime.UtcNow
                };

                _context.Profiles.Add(guestProfile);
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Default Guest profile created successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed database");
        }
    }
}
