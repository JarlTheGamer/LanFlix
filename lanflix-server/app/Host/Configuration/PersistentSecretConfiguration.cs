using System.Security.Cryptography;
using System.Text.Json;

namespace Lanflix.Host;

internal static class PersistentSecretConfiguration
{
    public static string Ensure(string configurationDirectory)
    {
        Directory.CreateDirectory(configurationDirectory);
        var path = Path.Combine(configurationDirectory, "lanflix-secrets.json");
        if (File.Exists(path)) return path;

        var temporaryPath = path + ".tmp";
        var document = new { Jwt = new { Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)) } };
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, path, false);
        return path;
    }
}
