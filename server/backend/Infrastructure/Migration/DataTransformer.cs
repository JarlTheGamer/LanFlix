using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Lanflix.Infrastructure.Migration.Models;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Migration;

/// <summary>
/// Transforms legacy data models to new backend schema
/// </summary>
public class DataTransformer
{
    private readonly ILogger<DataTransformer> _logger;

    public DataTransformer(ILogger<DataTransformer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Transforms a legacy Content entity to the new schema
    /// </summary>
    public Content TransformContent(LegacyContent legacy)
    {
        if (legacy == null)
            throw new ArgumentNullException(nameof(legacy));

        try
        {
            var content = new Content
            {
                Id = legacy.Id,
                TmdbId = legacy.TmdbId,
                Type = TransformContentType(legacy.Type),
                Title = legacy.Title ?? string.Empty,
                OriginalTitle = legacy.OriginalTitle,
                Overview = legacy.Overview,
                FilePath = legacy.FilePath ?? string.Empty,
                ReleaseDate = legacy.ReleaseDate,
                PosterPath = legacy.PosterPath,
                BackdropPath = legacy.BackdropPath,
                Rating = legacy.VoteAverage.HasValue ? (double)legacy.VoteAverage.Value : null,
                Genres = ParseGenres(legacy.Genres),
                AddedAt = legacy.AddedAt,
                CreatedAt = legacy.AddedAt,
                UpdatedAt = legacy.UpdatedAt,
                IsDeleted = false,
                DeletedAt = null,
                MediaInfo = null // Will be populated by media analyzer if needed
            };

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming content with ID {Id}", legacy.Id);
            throw;
        }
    }

    /// <summary>
    /// Transforms a legacy Profile entity to the new schema
    /// </summary>
    public Profile TransformProfile(LegacyProfile legacy)
    {
        if (legacy == null)
            throw new ArgumentNullException(nameof(legacy));

        try
        {
            var profile = new Profile
            {
                Id = legacy.Id,
                Name = legacy.Name ?? string.Empty,
                AvatarPath = null, // Legacy used color codes, new system uses avatar images
                IsKidsProfile = false, // Default value, can be updated later
                Preferences = CreateDefaultPreferences(legacy),
                PinCode = null,
                IsDefault = legacy.Id == 1, // First profile is default
                CreatedAt = legacy.CreatedAt,
                UpdatedAt = legacy.UpdatedAt
            };

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming profile with ID {Id}", legacy.Id);
            throw;
        }
    }

    /// <summary>
    /// Transforms a legacy WatchHistory entity to the new schema
    /// </summary>
    public WatchHistory TransformWatchHistory(LegacyWatchHistory legacy, int? durationSeconds = null)
    {
        if (legacy == null)
            throw new ArgumentNullException(nameof(legacy));

        try
        {
            // Convert seconds to ticks (1 tick = 100 nanoseconds, 1 second = 10,000,000 ticks)
            const long ticksPerSecond = 10_000_000;
            var positionTicks = legacy.ProgressSeconds * ticksPerSecond;

            // Calculate watched percentage
            double watchedPercentage = 0;
            if (legacy.DurationSeconds.HasValue && legacy.DurationSeconds.Value > 0)
            {
                watchedPercentage = (double)legacy.ProgressSeconds / legacy.DurationSeconds.Value * 100;
            }
            else if (durationSeconds.HasValue && durationSeconds.Value > 0)
            {
                watchedPercentage = (double)legacy.ProgressSeconds / durationSeconds.Value * 100;
            }

            var watchHistory = new WatchHistory
            {
                Id = legacy.Id,
                ProfileId = legacy.ProfileId,
                ContentId = legacy.ContentId,
                EpisodeId = legacy.EpisodeId,
                PositionTicks = positionTicks,
                IsCompleted = legacy.Completed,
                LastWatchedAt = legacy.LastWatchedAt,
                WatchedPercentage = Math.Min(100, Math.Max(0, watchedPercentage)),
                CreatedAt = legacy.LastWatchedAt,
                UpdatedAt = legacy.LastWatchedAt
            };

            return watchHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming watch history with ID {Id}", legacy.Id);
            throw;
        }
    }

    /// <summary>
    /// Transforms a legacy SeriesEpisode entity to the new schema
    /// </summary>
    public Episode TransformEpisode(LegacySeriesEpisode legacy)
    {
        if (legacy == null)
            throw new ArgumentNullException(nameof(legacy));

        try
        {
            var episode = new Episode
            {
                Id = legacy.Id,
                ContentId = legacy.ContentId,
                TmdbId = null, // Legacy didn't store TMDB ID for episodes
                SeasonNumber = legacy.SeasonNumber,
                EpisodeNumber = legacy.EpisodeNumber,
                Title = legacy.Title ?? $"Episode {legacy.EpisodeNumber}",
                Overview = legacy.Overview,
                FilePath = legacy.FilePath ?? string.Empty,
                AirDate = legacy.AirDate,
                StillPath = legacy.StillPath,
                AddedAt = DateTime.UtcNow, // Legacy didn't track this
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false,
                DeletedAt = null,
                MediaInfo = null // Will be populated by media analyzer if needed
            };

            return episode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming episode with ID {Id}", legacy.Id);
            throw;
        }
    }

    /// <summary>
    /// Transforms content type string to enum
    /// </summary>
    private ContentType TransformContentType(string type)
    {
        return type?.ToLowerInvariant() switch
        {
            "movie" => ContentType.Movie,
            "series" => ContentType.Series,
            _ => throw new ArgumentException($"Unknown content type: {type}", nameof(type))
        };
    }

    /// <summary>
    /// Parses genres string (comma-separated) to array
    /// </summary>
    private string[]? ParseGenres(string? genresString)
    {
        if (string.IsNullOrWhiteSpace(genresString))
            return null;

        return genresString
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .ToArray();
    }

    /// <summary>
    /// Creates default user preferences for migrated profiles
    /// </summary>
    private UserPreferences CreateDefaultPreferences(LegacyProfile legacy)
    {
        // Legacy profiles stored avatar colors, we'll preserve them in a comment
        // but the new system uses avatar images instead
        return new UserPreferences
        {
            PreferredAudioLanguage = "eng",
            PreferredSubtitleLanguage = null,
            SubtitlesEnabled = false,
            PreferredBitrate = null,
            AutoSkipIntro = false,
            AutoPlayNextEpisode = true,
            MaxResolution = null,
            AllowHardwareAcceleration = true,
            ForceTranscode = false,
            Theme = "dark"
        };
    }

    /// <summary>
    /// Validates transformed data for consistency
    /// </summary>
    public bool ValidateTransformedContent(Content content)
    {
        if (content == null)
            return false;

        if (string.IsNullOrWhiteSpace(content.Title))
        {
            _logger.LogWarning("Content {Id} has empty title", content.Id);
            return false;
        }

        if (content.TmdbId <= 0)
        {
            _logger.LogWarning("Content {Id} has invalid TMDB ID: {TmdbId}", content.Id, content.TmdbId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates transformed profile data
    /// </summary>
    public bool ValidateTransformedProfile(Profile profile)
    {
        if (profile == null)
            return false;

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            _logger.LogWarning("Profile {Id} has empty name", profile.Id);
            return false;
        }

        return true;
    }
}
