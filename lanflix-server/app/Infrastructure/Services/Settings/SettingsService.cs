using System.Text.Json;
using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Settings;

public class SettingsService : ISettingsService
{
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(
        IConfiguration configuration,
        IApplicationDbContext context,
        ILogger<SettingsService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    public async Task<ServerSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        // Load all settings from database once
        var dbSettings = await _context.ServerSettings
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        
        _logger.LogInformation("Loaded {Count} settings from database", dbSettings.Count);
        
        var settingsDict = dbSettings.ToDictionary(s => s.Key, s => s.Value);

        // Helper to get setting from DB or config
        string GetSetting(string key, string defaultValue = "")
        {
            if (settingsDict.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            {
                _logger.LogDebug("Setting {Key} loaded from database: {Value}", key, 
                    key.Contains("ApiKey") || key.Contains("Key") ? "***" : value);
                return value;
            }
            var configValue = _configuration[key] ?? defaultValue;
            _logger.LogDebug("Setting {Key} loaded from config: {Value}", key, 
                key.Contains("ApiKey") || key.Contains("Key") ? "***" : configValue);
            return configValue;
        }

        int GetIntSetting(string key, int defaultValue)
        {
            if (settingsDict.TryGetValue(key, out var value) && int.TryParse(value, out var intValue))
                return intValue;
            return _configuration.GetValue<int>(key, defaultValue);
        }

        bool GetBoolSetting(string key, bool defaultValue)
        {
            if (settingsDict.TryGetValue(key, out var value) && bool.TryParse(value, out var boolValue))
                return boolValue;
            return _configuration.GetValue<bool>(key, defaultValue);
        }

        var settings = new ServerSettingsDto
        {
            MediaPaths = new MediaPathsSettings
            {
                Movies = GetSetting("Lanflix:MediaPaths:Movies"),
                Series = GetSetting("Lanflix:MediaPaths:Series"),
                PosterCache = GetSetting("Lanflix:MediaPaths:PosterCache"),
                BackdropCache = GetSetting("Lanflix:MediaPaths:BackdropCache")
            },
            Transcoding = new TranscodingSettings
            {
                EnableHardwareAcceleration = GetBoolSetting("Lanflix:Transcoding:EnableHardwareAcceleration", true),
                PreferredHwAccel = GetSetting("Lanflix:Transcoding:PreferredHwAccel", "auto"),
                MaxConcurrentTranscodes = GetIntSetting("Lanflix:Transcoding:MaxConcurrentTranscodes", 2),
                TempPath = GetSetting("Lanflix:Transcoding:TempPath"),
                DefaultBitrate = GetIntSetting("Lanflix:Transcoding:DefaultBitrate", 8000000),
                HlsSegmentDuration = GetIntSetting("Lanflix:Transcoding:HlsSegmentDuration", 6)
            },
            Streaming = new StreamingSettings
            {
                EnableDirectPlay = GetBoolSetting("Lanflix:Streaming:EnableDirectPlay", true),
                EnableDirectStream = GetBoolSetting("Lanflix:Streaming:EnableDirectStream", true),
                ChunkSize = GetIntSetting("Lanflix:Streaming:ChunkSize", 81920)
            },
            Cache = new CacheSettings
            {
                Redis = new RedisCacheSettings
                {
                    Enabled = GetBoolSetting("Lanflix:Cache:Redis:Enabled", false),
                    ConnectionString = GetSetting("Lanflix:Cache:Redis:ConnectionString"),
                    InstanceName = GetSetting("Lanflix:Cache:Redis:InstanceName", "lanflix:")
                },
                Memory = new MemoryCacheSettings
                {
                    SizeLimit = GetIntSetting("Lanflix:Cache:Memory:SizeLimit", 512)
                }
            },
            ExternalApis = new ExternalApisSettings
            {
                Tmdb = new TmdbSettings
                {
                    ApiKey = GetSetting("Lanflix:ExternalApis:Tmdb:ApiKey"),
                    BaseUrl = GetSetting("Lanflix:ExternalApis:Tmdb:BaseUrl", "https://api.themoviedb.org/3/")
                },
                Sonarr = new ExternalServiceSettings
                {
                    Url = GetSetting("Lanflix:ExternalApis:Sonarr:Url"),
                    ApiKey = GetSetting("Lanflix:ExternalApis:Sonarr:ApiKey")
                },
                Radarr = new ExternalServiceSettings
                {
                    Url = GetSetting("Lanflix:ExternalApis:Radarr:Url"),
                    ApiKey = GetSetting("Lanflix:ExternalApis:Radarr:ApiKey")
                },
                Prowlarr = new ExternalServiceSettings
                {
                    Url = GetSetting("Lanflix:ExternalApis:Prowlarr:Url"),
                    ApiKey = GetSetting("Lanflix:ExternalApis:Prowlarr:ApiKey")
                }
            }
        };

        return settings;
    }

    public async Task UpdateSettingsAsync(ServerSettingsDto settings, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating server settings to database");

        try
        {
            var now = DateTime.UtcNow;

            // Helper to upsert a setting
            async Task UpsertSetting(string key, string value)
            {
                var existing = await _context.ServerSettings
                    .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

                if (existing != null)
                {
                    existing.Value = value;
                    existing.UpdatedAt = now;
                }
                else
                {
                    _context.ServerSettings.Add(new Domain.Entities.ServerSetting
                    {
                        Key = key,
                        Value = value,
                        UpdatedAt = now
                    });
                }
            }

            // Save all settings to database
            await UpsertSetting("Lanflix:MediaPaths:Movies", settings.MediaPaths.Movies);
            await UpsertSetting("Lanflix:MediaPaths:Series", settings.MediaPaths.Series);
            await UpsertSetting("Lanflix:MediaPaths:PosterCache", settings.MediaPaths.PosterCache);
            await UpsertSetting("Lanflix:MediaPaths:BackdropCache", settings.MediaPaths.BackdropCache);

            await UpsertSetting("Lanflix:Transcoding:EnableHardwareAcceleration", settings.Transcoding.EnableHardwareAcceleration.ToString());
            await UpsertSetting("Lanflix:Transcoding:PreferredHwAccel", settings.Transcoding.PreferredHwAccel);
            await UpsertSetting("Lanflix:Transcoding:MaxConcurrentTranscodes", settings.Transcoding.MaxConcurrentTranscodes.ToString());
            await UpsertSetting("Lanflix:Transcoding:TempPath", settings.Transcoding.TempPath);
            await UpsertSetting("Lanflix:Transcoding:DefaultBitrate", settings.Transcoding.DefaultBitrate.ToString());
            await UpsertSetting("Lanflix:Transcoding:HlsSegmentDuration", settings.Transcoding.HlsSegmentDuration.ToString());

            await UpsertSetting("Lanflix:Streaming:EnableDirectPlay", settings.Streaming.EnableDirectPlay.ToString());
            await UpsertSetting("Lanflix:Streaming:EnableDirectStream", settings.Streaming.EnableDirectStream.ToString());
            await UpsertSetting("Lanflix:Streaming:ChunkSize", settings.Streaming.ChunkSize.ToString());

            await UpsertSetting("Lanflix:Cache:Redis:Enabled", settings.Cache.Redis.Enabled.ToString());
            await UpsertSetting("Lanflix:Cache:Redis:ConnectionString", settings.Cache.Redis.ConnectionString);
            await UpsertSetting("Lanflix:Cache:Redis:InstanceName", settings.Cache.Redis.InstanceName);
            await UpsertSetting("Lanflix:Cache:Memory:SizeLimit", settings.Cache.Memory.SizeLimit.ToString());

            await UpsertSetting("Lanflix:ExternalApis:Tmdb:ApiKey", settings.ExternalApis.Tmdb.ApiKey);
            await UpsertSetting("Lanflix:ExternalApis:Tmdb:BaseUrl", settings.ExternalApis.Tmdb.BaseUrl);

            await UpsertSetting("Lanflix:ExternalApis:Sonarr:Url", settings.ExternalApis.Sonarr.Url);
            await UpsertSetting("Lanflix:ExternalApis:Sonarr:ApiKey", settings.ExternalApis.Sonarr.ApiKey);

            await UpsertSetting("Lanflix:ExternalApis:Radarr:Url", settings.ExternalApis.Radarr.Url);
            await UpsertSetting("Lanflix:ExternalApis:Radarr:ApiKey", settings.ExternalApis.Radarr.ApiKey);

            await UpsertSetting("Lanflix:ExternalApis:Prowlarr:Url", settings.ExternalApis.Prowlarr.Url);
            await UpsertSetting("Lanflix:ExternalApis:Prowlarr:ApiKey", settings.ExternalApis.Prowlarr.ApiKey);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Server settings saved to database successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating server settings");
            throw;
        }
    }

    public async Task UpdateSettingAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating setting: {Key}", key);

        try
        {
            var now = DateTime.UtcNow;
            var existing = await _context.ServerSettings
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = now;
            }
            else
            {
                _context.ServerSettings.Add(new Domain.Entities.ServerSetting
                {
                    Key = key,
                    Value = value,
                    UpdatedAt = now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Setting {Key} updated successfully", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating setting: {Key}", key);
            throw;
        }
    }

    public async Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var setting = await _context.ServerSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

            return setting?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting: {Key}", key);
            return null;
        }
    }
}
