using System.Text.Json;
using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Settings;

public class SettingsService : ISettingsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsFilePath;

    public SettingsService(IConfiguration configuration, ILogger<SettingsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _settingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
    }

    public Task<ServerSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = new ServerSettingsDto
        {
            MediaPaths = new MediaPathsSettings
            {
                Movies = _configuration["Lanflix:MediaPaths:Movies"] ?? string.Empty,
                Series = _configuration["Lanflix:MediaPaths:Series"] ?? string.Empty,
                PosterCache = _configuration["Lanflix:MediaPaths:PosterCache"] ?? string.Empty,
                BackdropCache = _configuration["Lanflix:MediaPaths:BackdropCache"] ?? string.Empty
            },
            Transcoding = new TranscodingSettings
            {
                EnableHardwareAcceleration = _configuration.GetValue<bool>("Lanflix:Transcoding:EnableHardwareAcceleration", true),
                PreferredHwAccel = _configuration["Lanflix:Transcoding:PreferredHwAccel"] ?? "auto",
                MaxConcurrentTranscodes = _configuration.GetValue<int>("Lanflix:Transcoding:MaxConcurrentTranscodes", 2),
                TempPath = _configuration["Lanflix:Transcoding:TempPath"] ?? string.Empty,
                DefaultBitrate = _configuration.GetValue<int>("Lanflix:Transcoding:DefaultBitrate", 8000000),
                HlsSegmentDuration = _configuration.GetValue<int>("Lanflix:Transcoding:HlsSegmentDuration", 6)
            },
            Streaming = new StreamingSettings
            {
                EnableDirectPlay = _configuration.GetValue<bool>("Lanflix:Streaming:EnableDirectPlay", true),
                EnableDirectStream = _configuration.GetValue<bool>("Lanflix:Streaming:EnableDirectStream", true),
                ChunkSize = _configuration.GetValue<int>("Lanflix:Streaming:ChunkSize", 81920)
            },
            Cache = new CacheSettings
            {
                Redis = new RedisCacheSettings
                {
                    Enabled = _configuration.GetValue<bool>("Lanflix:Cache:Redis:Enabled", false),
                    ConnectionString = _configuration["Lanflix:Cache:Redis:ConnectionString"] ?? string.Empty,
                    InstanceName = _configuration["Lanflix:Cache:Redis:InstanceName"] ?? "lanflix:"
                },
                Memory = new MemoryCacheSettings
                {
                    SizeLimit = _configuration.GetValue<int>("Lanflix:Cache:Memory:SizeLimit", 512)
                }
            },
            ExternalApis = new ExternalApisSettings
            {
                Tmdb = new TmdbSettings
                {
                    ApiKey = _configuration["Lanflix:ExternalApis:Tmdb:ApiKey"] ?? string.Empty,
                    BaseUrl = _configuration["Lanflix:ExternalApis:Tmdb:BaseUrl"] ?? "https://api.themoviedb.org/3/"
                }
            }
        };

        return Task.FromResult(settings);
    }

    public async Task UpdateSettingsAsync(ServerSettingsDto settings, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating server settings");

        try
        {
            // Read existing appsettings.json
            string json;
            if (File.Exists(_settingsFilePath))
            {
                json = await File.ReadAllTextAsync(_settingsFilePath, cancellationToken);
            }
            else
            {
                json = "{}";
            }

            // Parse JSON
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            // Create a mutable dictionary
            var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json) 
                ?? new Dictionary<string, object>();

            // Update Lanflix section
            var lanflixSection = new Dictionary<string, object>
            {
                ["MediaPaths"] = new Dictionary<string, object>
                {
                    ["Movies"] = settings.MediaPaths.Movies,
                    ["Series"] = settings.MediaPaths.Series,
                    ["PosterCache"] = settings.MediaPaths.PosterCache,
                    ["BackdropCache"] = settings.MediaPaths.BackdropCache
                },
                ["Transcoding"] = new Dictionary<string, object>
                {
                    ["EnableHardwareAcceleration"] = settings.Transcoding.EnableHardwareAcceleration,
                    ["PreferredHwAccel"] = settings.Transcoding.PreferredHwAccel,
                    ["MaxConcurrentTranscodes"] = settings.Transcoding.MaxConcurrentTranscodes,
                    ["TempPath"] = settings.Transcoding.TempPath,
                    ["DefaultBitrate"] = settings.Transcoding.DefaultBitrate,
                    ["HlsSegmentDuration"] = settings.Transcoding.HlsSegmentDuration
                },
                ["Streaming"] = new Dictionary<string, object>
                {
                    ["EnableDirectPlay"] = settings.Streaming.EnableDirectPlay,
                    ["EnableDirectStream"] = settings.Streaming.EnableDirectStream,
                    ["ChunkSize"] = settings.Streaming.ChunkSize
                },
                ["Cache"] = new Dictionary<string, object>
                {
                    ["Redis"] = new Dictionary<string, object>
                    {
                        ["Enabled"] = settings.Cache.Redis.Enabled,
                        ["ConnectionString"] = settings.Cache.Redis.ConnectionString,
                        ["InstanceName"] = settings.Cache.Redis.InstanceName
                    },
                    ["Memory"] = new Dictionary<string, object>
                    {
                        ["SizeLimit"] = settings.Cache.Memory.SizeLimit
                    }
                },
                ["ExternalApis"] = new Dictionary<string, object>
                {
                    ["Tmdb"] = new Dictionary<string, object>
                    {
                        ["ApiKey"] = settings.ExternalApis.Tmdb.ApiKey,
                        ["BaseUrl"] = settings.ExternalApis.Tmdb.BaseUrl
                    }
                }
            };

            // Preserve AppUpdates section if it exists
            if (root.TryGetProperty("Lanflix", out var existingLanflix) &&
                existingLanflix.TryGetProperty("AppUpdates", out var appUpdates))
            {
                lanflixSection["AppUpdates"] = JsonSerializer.Deserialize<Dictionary<string, object>>(appUpdates.GetRawText())
                    ?? new Dictionary<string, object>();
            }

            configDict["Lanflix"] = lanflixSection;

            // Write back to file
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            json = JsonSerializer.Serialize(configDict, options);
            await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken);

            _logger.LogInformation("Server settings updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating server settings");
            throw;
        }
    }
}
