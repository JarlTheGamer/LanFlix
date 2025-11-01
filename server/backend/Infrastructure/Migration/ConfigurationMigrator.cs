using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Migration;

/// <summary>
/// Migrates configuration from legacy .env file to new appsettings.json format
/// </summary>
public class ConfigurationMigrator
{
    private readonly ILogger<ConfigurationMigrator> _logger;

    public ConfigurationMigrator(ILogger<ConfigurationMigrator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Reads and parses the legacy .env file
    /// </summary>
    public Dictionary<string, string> ReadLegacyEnvFile(string envFilePath)
    {
        if (!File.Exists(envFilePath))
        {
            _logger.LogWarning("Legacy .env file not found at: {Path}", envFilePath);
            return new Dictionary<string, string>();
        }

        var config = new Dictionary<string, string>();

        try
        {
            var lines = File.ReadAllLines(envFilePath);

            foreach (var line in lines)
            {
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    // Remove quotes if present
                    if ((value.StartsWith('"') && value.EndsWith('"')) ||
                        (value.StartsWith('\'') && value.EndsWith('\'')))
                    {
                        value = value[1..^1];
                    }

                    config[key] = value;
                }
            }

            _logger.LogInformation("Read {Count} configuration values from legacy .env file", config.Count);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading legacy .env file");
            throw;
        }
    }

    /// <summary>
    /// Transforms legacy configuration to new appsettings.json structure
    /// </summary>
    public object TransformToAppSettings(Dictionary<string, string> legacyConfig)
    {
        var appSettings = new
        {
            Lanflix = new
            {
                MediaPaths = new
                {
                    Movies = GetConfigValue(legacyConfig, "MEDIA_ROOT_PATH", "D:/Media/Movies"),
                    Series = GetConfigValue(legacyConfig, "MEDIA_ROOT_PATH", "D:/Media/Series"),
                    PosterCache = GetConfigValue(legacyConfig, "POSTER_CACHE_PATH", "./data/posters"),
                    BackdropCache = GetConfigValue(legacyConfig, "BACKDROP_CACHE_PATH", "./data/backdrops")
                },
                Transcoding = new
                {
                    EnableHardwareAcceleration = true,
                    PreferredHwAccel = "auto",
                    MaxConcurrentTranscodes = 2,
                    TempPath = Path.Combine(Path.GetTempPath(), "Lanflix", "Transcoding"),
                    DefaultBitrate = 8000000,
                    HlsSegmentDuration = 6
                },
                Streaming = new
                {
                    EnableDirectPlay = true,
                    EnableDirectStream = true,
                    ChunkSize = 81920
                },
                Cache = new
                {
                    Redis = new
                    {
                        Enabled = legacyConfig.ContainsKey("REDIS_URL"),
                        ConnectionString = GetConfigValue(legacyConfig, "REDIS_URL", "localhost:6379"),
                        InstanceName = "lanflix:"
                    },
                    Memory = new
                    {
                        SizeLimit = 512
                    }
                },
                ExternalApis = new
                {
                    Tmdb = new
                    {
                        ApiKey = GetConfigValue(legacyConfig, "TMDB_API_KEY", ""),
                        BaseUrl = "https://api.themoviedb.org/3/"
                    },
                    Sonarr = new
                    {
                        Url = GetConfigValue(legacyConfig, "SONARR_URL", ""),
                        ApiKey = GetConfigValue(legacyConfig, "SONARR_API_KEY", "")
                    },
                    Radarr = new
                    {
                        Url = GetConfigValue(legacyConfig, "RADARR_URL", ""),
                        ApiKey = GetConfigValue(legacyConfig, "RADARR_API_KEY", "")
                    },
                    Prowlarr = new
                    {
                        Url = GetConfigValue(legacyConfig, "PROWLARR_URL", ""),
                        ApiKey = GetConfigValue(legacyConfig, "PROWLARR_API_KEY", "")
                    }
                },
                AppUpdates = new
                {
                    ApkStoragePath = "./data/AppUpdates/Android",
                    EnableAutoUpdate = true
                }
            },
            ConnectionStrings = new
            {
                DefaultConnection = GetConfigValue(legacyConfig, "DATABASE_PATH", "Data Source=lanflix.db")
            },
            Logging = new
            {
                LogLevel = new
                {
                    Default = GetConfigValue(legacyConfig, "LOG_LEVEL", "Information"),
                    Microsoft_AspNetCore = "Warning",
                    Microsoft_EntityFrameworkCore = "Warning"
                }
            },
            AllowedHosts = "*"
        };

        return appSettings;
    }

    /// <summary>
    /// Writes the transformed configuration to appsettings.json file
    /// </summary>
    public void WriteAppSettingsFile(object appSettings, string outputPath)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(appSettings, options);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outputPath, json, Encoding.UTF8);
            
            _logger.LogInformation("Successfully wrote appsettings.json to {Path}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing appsettings.json file");
            throw;
        }
    }

    /// <summary>
    /// Migrates media paths from legacy to new format
    /// </summary>
    public Dictionary<string, string> MigrateMediaPaths(Dictionary<string, string> legacyConfig)
    {
        var mediaPaths = new Dictionary<string, string>();

        var mediaRoot = GetConfigValue(legacyConfig, "MEDIA_ROOT_PATH", "");
        if (!string.IsNullOrEmpty(mediaRoot))
        {
            // Legacy used a single media root, new system separates movies and series
            mediaPaths["Movies"] = Path.Combine(mediaRoot, "Movies");
            mediaPaths["Series"] = Path.Combine(mediaRoot, "Series");
            
            _logger.LogInformation("Migrated media root path: {Path}", mediaRoot);
        }

        var posterCache = GetConfigValue(legacyConfig, "POSTER_CACHE_PATH", "");
        if (!string.IsNullOrEmpty(posterCache))
        {
            mediaPaths["PosterCache"] = posterCache;
        }

        var backdropCache = GetConfigValue(legacyConfig, "BACKDROP_CACHE_PATH", "");
        if (!string.IsNullOrEmpty(backdropCache))
        {
            mediaPaths["BackdropCache"] = backdropCache;
        }

        return mediaPaths;
    }

    /// <summary>
    /// Migrates API keys from legacy configuration
    /// </summary>
    public Dictionary<string, string> MigrateApiKeys(Dictionary<string, string> legacyConfig)
    {
        var apiKeys = new Dictionary<string, string>();

        var tmdbKey = GetConfigValue(legacyConfig, "TMDB_API_KEY", "");
        if (!string.IsNullOrEmpty(tmdbKey))
        {
            apiKeys["TMDB"] = tmdbKey;
            _logger.LogInformation("Migrated TMDB API key");
        }

        var sonarrKey = GetConfigValue(legacyConfig, "SONARR_API_KEY", "");
        if (!string.IsNullOrEmpty(sonarrKey))
        {
            apiKeys["Sonarr"] = sonarrKey;
            _logger.LogInformation("Migrated Sonarr API key");
        }

        var radarrKey = GetConfigValue(legacyConfig, "RADARR_API_KEY", "");
        if (!string.IsNullOrEmpty(radarrKey))
        {
            apiKeys["Radarr"] = radarrKey;
            _logger.LogInformation("Migrated Radarr API key");
        }

        var prowlarrKey = GetConfigValue(legacyConfig, "PROWLARR_API_KEY", "");
        if (!string.IsNullOrEmpty(prowlarrKey))
        {
            apiKeys["Prowlarr"] = prowlarrKey;
            _logger.LogInformation("Migrated Prowlarr API key");
        }

        return apiKeys;
    }

    /// <summary>
    /// Validates that critical configuration values are present
    /// </summary>
    public List<string> ValidateConfiguration(Dictionary<string, string> legacyConfig)
    {
        var warnings = new List<string>();

        if (!legacyConfig.ContainsKey("MEDIA_ROOT_PATH") || string.IsNullOrEmpty(legacyConfig["MEDIA_ROOT_PATH"]))
        {
            warnings.Add("MEDIA_ROOT_PATH is not configured");
        }

        if (!legacyConfig.ContainsKey("TMDB_API_KEY") || string.IsNullOrEmpty(legacyConfig["TMDB_API_KEY"]))
        {
            warnings.Add("TMDB_API_KEY is not configured - metadata fetching will not work");
        }

        if (!legacyConfig.ContainsKey("DATABASE_PATH") || string.IsNullOrEmpty(legacyConfig["DATABASE_PATH"]))
        {
            warnings.Add("DATABASE_PATH is not configured");
        }

        return warnings;
    }

    /// <summary>
    /// Generates a migration report for configuration changes
    /// </summary>
    public string GenerateConfigurationReport(Dictionary<string, string> legacyConfig, object newConfig)
    {
        var report = new StringBuilder();
        report.AppendLine("=== Configuration Migration Report ===");
        report.AppendLine();
        report.AppendLine($"Migration Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine();
        report.AppendLine("Legacy Configuration Values:");
        
        foreach (var kvp in legacyConfig.OrderBy(k => k.Key))
        {
            // Mask sensitive values
            var value = IsSensitiveKey(kvp.Key) ? "***REDACTED***" : kvp.Value;
            report.AppendLine($"  {kvp.Key} = {value}");
        }

        report.AppendLine();
        report.AppendLine("New Configuration Structure:");
        report.AppendLine("  See generated appsettings.json file");
        report.AppendLine();

        var warnings = ValidateConfiguration(legacyConfig);
        if (warnings.Any())
        {
            report.AppendLine("Warnings:");
            foreach (var warning in warnings)
            {
                report.AppendLine($"  - {warning}");
            }
        }
        else
        {
            report.AppendLine("No warnings - all critical configuration values present");
        }

        return report.ToString();
    }

    private string GetConfigValue(Dictionary<string, string> config, string key, string defaultValue)
    {
        return config.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) 
            ? value 
            : defaultValue;
    }

    private bool IsSensitiveKey(string key)
    {
        var sensitiveKeys = new[] { "API_KEY", "PASSWORD", "SECRET", "TOKEN" };
        return sensitiveKeys.Any(sk => key.Contains(sk, StringComparison.OrdinalIgnoreCase));
    }
}
