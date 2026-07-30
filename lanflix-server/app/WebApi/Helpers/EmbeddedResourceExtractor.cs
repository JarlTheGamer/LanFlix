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
        
        var extractedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int updatedCount = 0;
        
        foreach (var resourceName in wwwrootResources)
        {
            // Remove prefix to get relative path
            var relativePath = resourceName.Substring(wwwrootPrefix.Length);
            
            // Convert embedded resource name back to file path
            var outputPath = Path.Combine("wwwroot", ConvertResourceNameToPath(relativePath));
            extractedPaths.Add(Path.GetFullPath(outputPath));
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                continue;
                
            bool needsOverwrite = true;
            if (File.Exists(outputPath))
            {
                var existingInfo = new FileInfo(outputPath);
                if (existingInfo.Length == stream.Length)
                {
                    using var existingStream = File.OpenRead(outputPath);
                    if (StreamsAreEqual(stream, existingStream))
                    {
                        needsOverwrite = false;
                    }
                    stream.Position = 0; // Reset position after reading comparison
                }
            }
            
            if (needsOverwrite)
            {
                var outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);
                    
                using var fileStream = File.Create(outputPath);
                stream.CopyTo(fileStream);
                updatedCount++;
            }
        }
        
        if (updatedCount > 0)
        {
            Console.WriteLine($"Extracted/Updated {updatedCount} wwwroot UI asset(s)");
        }

        // Clean up obsolete stale bundle assets in wwwroot/assets no longer in current embedded build
        var assetsDir = Path.Combine("wwwroot", "assets");
        if (Directory.Exists(assetsDir))
        {
            foreach (var file in Directory.GetFiles(assetsDir, "*.*", SearchOption.AllDirectories))
            {
                if (!extractedPaths.Contains(Path.GetFullPath(file)))
                {
                    try 
                    { 
                        File.Delete(file);
                    } 
                    catch 
                    { 
                        // Ignore files locked by running processes
                    }
                }
            }
        }
    }
    
    private static bool StreamsAreEqual(Stream stream1, Stream stream2)
    {
        const int bufferSize = 8192;
        var buffer1 = new byte[bufferSize];
        var buffer2 = new byte[bufferSize];

        while (true)
        {
            int count1 = stream1.Read(buffer1, 0, bufferSize);
            int count2 = stream2.Read(buffer2, 0, bufferSize);

            if (count1 != count2)
                return false;

            if (count1 == 0)
                return true;

            for (int i = 0; i < count1; i++)
            {
                if (buffer1[i] != buffer2[i])
                    return false;
            }
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
