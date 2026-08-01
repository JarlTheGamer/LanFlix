using System.Security.Cryptography;
using System.Text.Json;

namespace Lanflix.WebApi.Helpers;

public static class PersistentSecretConfiguration
{
    public static string EnsureSecretFile(string configDirectory)
    {
        Directory.CreateDirectory(configDirectory);
        var path = Path.Combine(configDirectory, "lanflix-secrets.json");
        if (File.Exists(path)) return path;

        var temporaryPath = path + ".tmp";
        var payload = new
        {
            Jwt = new
            {
                Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            }
        };
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, path, overwrite: false);
        return path;
    }
}
