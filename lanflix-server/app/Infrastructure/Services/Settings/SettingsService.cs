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
        _logger.LogInformation("Updating server settings to database and persistent config");

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

            await UpsertSetting("Lanflix:Cache:Memory:SizeLimit", settings.Cache.Memory.SizeLimit.ToString());

            // Only update API keys if they're not placeholders or empty
            if (!string.IsNullOrEmpty(settings.ExternalApis.Tmdb.ApiKey) && 
                !settings.ExternalApis.Tmdb.ApiKey.StartsWith("${"))
            {
                await UpsertSetting("Lanflix:ExternalApis:Tmdb:ApiKey", settings.ExternalApis.Tmdb.ApiKey);
            }
            await UpsertSetting("Lanflix:ExternalApis:Tmdb:BaseUrl", settings.ExternalApis.Tmdb.BaseUrl);

            await UpsertSetting("Lanflix:ExternalApis:Sonarr:Url", settings.ExternalApis.Sonarr.Url);
            if (!string.IsNullOrEmpty(settings.ExternalApis.Sonarr.ApiKey) && 
                !settings.ExternalApis.Sonarr.ApiKey.StartsWith("${"))
            {
                await UpsertSetting("Lanflix:ExternalApis:Sonarr:ApiKey", settings.ExternalApis.Sonarr.ApiKey);
            }

            await UpsertSetting("Lanflix:ExternalApis:Radarr:Url", settings.ExternalApis.Radarr.Url);
            if (!string.IsNullOrEmpty(settings.ExternalApis.Radarr.ApiKey) && 
                !settings.ExternalApis.Radarr.ApiKey.StartsWith("${"))
            {
                await UpsertSetting("Lanflix:ExternalApis:Radarr:ApiKey", settings.ExternalApis.Radarr.ApiKey);
            }

            await UpsertSetting("Lanflix:ExternalApis:Prowlarr:Url", settings.ExternalApis.Prowlarr.Url);
            if (!string.IsNullOrEmpty(settings.ExternalApis.Prowlarr.ApiKey) && 
                !settings.ExternalApis.Prowlarr.ApiKey.StartsWith("${"))
            {
                await UpsertSetting("Lanflix:ExternalApis:Prowlarr:ApiKey", settings.ExternalApis.Prowlarr.ApiKey);
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            // --- PERSISTENCE TO FILE ---
            // Save to config/lanflix.json so it survives updates
            try 
            {
                await SaveSettingsToFileAsync(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings to persistent config file");
                // Don't throw, as DB save was successful
            }

            _logger.LogInformation("Server settings saved to database and config file successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating server settings");
            throw;
        }
    }

    private async Task SaveSettingsToFileAsync(ServerSettingsDto settings)
    {
        var baseDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        var configPath = Path.Combine(baseDir, "config", "lanflix.json");
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // Map DTO to the configuration structure expected by IConfiguration
        // We recreate the object structure: Lanflix -> { MediaPaths, Transcoding, ... }
        var configObject = new
        {
            Lanflix = new 
            {
                MediaPaths = settings.MediaPaths,
                Transcoding = settings.Transcoding,
                Streaming = settings.Streaming,
                Cache = settings.Cache,
                ExternalApis = new 
                {
                    Tmdb = settings.ExternalApis.Tmdb,
                    Sonarr = settings.ExternalApis.Sonarr,
                    Radarr = settings.ExternalApis.Radarr,
                    Prowlarr = settings.ExternalApis.Prowlarr
                }
            }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(configObject, options);
        await File.WriteAllTextAsync(configPath, json);
        _logger.LogDebug("Wrote persistent config to {Path}", configPath);
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
    public async Task EnsureConfigFileExistsAsync(CancellationToken cancellationToken = default)
    {
        var baseDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        var configPath = Path.Combine(baseDir, "config", "lanflix.json");
        if (File.Exists(configPath)) return;

        _logger.LogInformation("Persistent config file not found. Creating from database settings...");
        
        try 
        {
            var settings = await GetSettingsAsync(cancellationToken);
            await SaveSettingsToFileAsync(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create initial persistent config file");
        }
    }
}
