using System.Reflection;

namespace Lanflix.WebApi.Helpers;

public static class EmbeddedResourceExtractor
{
    public static void ExtractConfigFiles()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        
        // Create necessary directories
        CreateDirectories();
        
        // Extract appsettings files
        ExtractResource(assembly, "Lanflix.WebApi.appsettings.json", "appsettings.json");
        ExtractResource(assembly, "Lanflix.WebApi.appsettings.Development.json", "appsettings.Development.json");
        ExtractResource(assembly, "Lanflix.WebApi.appsettings.Production.json", "appsettings.Production.json");
        
        // Extract wwwroot folder
        ExtractWwwroot(assembly, resourceNames);
    }
    
    private static void CreateDirectories()
    {
        // Create data directory for database
        Directory.CreateDirectory("data");
        
        // Create logs directory
        Directory.CreateDirectory("logs");
    }
    
    private static void ExtractResource(Assembly assembly, string resourceName, string outputPath)
    {
        if (File.Exists(outputPath))
            return; // Don't overwrite existing files
            
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return;
            
        using var fileStream = File.Create(outputPath);
        stream.CopyTo(fileStream);
    }
    
    private static void ExtractWwwroot(Assembly assembly, string[] resourceNames)
    {
        const string wwwrootPrefix = "Lanflix.WebApi.wwwroot.";
        
        foreach (var resourceName in resourceNames.Where(r => r.StartsWith(wwwrootPrefix)))
        {
            var relativePath = resourceName.Substring(wwwrootPrefix.Length);
            var outputPath = Path.Combine("wwwroot", relativePath.Replace('.', Path.DirectorySeparatorChar));
            
            // Handle file extensions properly
            var lastDot = outputPath.LastIndexOf('.');
            if (lastDot > 0)
            {
                var extension = outputPath.Substring(lastDot);
                var pathWithoutExt = outputPath.Substring(0, lastDot);
                
                // Reconstruct proper path
                var parts = pathWithoutExt.Split(Path.DirectorySeparatorChar);
                if (parts.Length > 0)
                {
                    var fileName = parts[^1];
                    var dirPath = string.Join(Path.DirectorySeparatorChar.ToString(), parts.Take(parts.Length - 1));
                    outputPath = Path.Combine(dirPath, fileName + extension);
                }
            }
            
            if (File.Exists(outputPath))
                continue;
                
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
                
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                continue;
                
            using var fileStream = File.Create(outputPath);
            stream.CopyTo(fileStream);
        }
    }
}
