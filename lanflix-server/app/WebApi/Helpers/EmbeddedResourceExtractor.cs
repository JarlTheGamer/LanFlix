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
        
        var wwwrootResources = resourceNames.Where(r => r.StartsWith(wwwrootPrefix)).ToList();
        
        if (wwwrootResources.Count == 0)
        {
            Console.WriteLine("Warning: No wwwroot resources found in assembly");
            return;
        }
        
        foreach (var resourceName in wwwrootResources)
        {
            // Remove prefix to get relative path
            var relativePath = resourceName.Substring(wwwrootPrefix.Length);
            
            // Convert embedded resource name back to file path
            // Resources are named like: wwwroot.assets.main-D83xSheS.js
            // We need to convert to: wwwroot/assets/main-D83xSheS.js
            var outputPath = ConvertResourceNameToPath(relativePath);
            outputPath = Path.Combine("wwwroot", outputPath);
            
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
    
    private static string ConvertResourceNameToPath(string resourcePath)
    {
        // Find the last dot which is the file extension
        var lastDotIndex = resourcePath.LastIndexOf('.');
        if (lastDotIndex == -1)
            return resourcePath.Replace('.', Path.DirectorySeparatorChar);
            
        // Split into path and extension
        var pathPart = resourcePath.Substring(0, lastDotIndex);
        var extension = resourcePath.Substring(lastDotIndex);
        
        // Replace dots with directory separators in the path part
        var filePath = pathPart.Replace('.', Path.DirectorySeparatorChar) + extension;
        
        return filePath;
    }
}
