using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lanflix.Host;

internal static class MediaEnvironment
{
    public static string Ensure(string applicationDirectory, string configurationDirectory)
    {
        var configuredRoot = Environment.GetEnvironmentVariable("LANFLIX_MEDIA_PATH");
        var mediaRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(applicationDirectory, "media")
            : configuredRoot);

        var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Movies"] = Path.Combine(mediaRoot, "movies"),
            ["Series"] = Path.Combine(mediaRoot, "series"),
            ["Music"] = Path.Combine(mediaRoot, "music")
        };
        foreach (var path in defaults.Values) Directory.CreateDirectory(path);

        Directory.CreateDirectory(configurationDirectory);
        var configPath = Path.Combine(configurationDirectory, "lanflix.json");
        JsonObject root;
        try
        {
            root = File.Exists(configPath)
                ? JsonNode.Parse(File.ReadAllText(configPath)) as JsonObject ?? new JsonObject()
                : new JsonObject();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"The Lanflix configuration is not valid JSON: {configPath}", exception);
        }

        var lanflix = root["Lanflix"] as JsonObject ?? new JsonObject();
        root["Lanflix"] = lanflix;
        var mediaPaths = lanflix["MediaPaths"] as JsonObject ?? new JsonObject();
        lanflix["MediaPaths"] = mediaPaths;
        var changed = !File.Exists(configPath);
        foreach (var (name, fallback) in defaults)
        {
            var current = mediaPaths[name]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(current))
            {
                Directory.CreateDirectory(Path.GetFullPath(current));
                continue;
            }
            mediaPaths[name] = fallback;
            changed = true;
        }

        if (changed)
        {
            var temporary = configPath + ".tmp";
            File.WriteAllText(temporary, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, configPath, true);
        }
        return configPath;
    }
}
