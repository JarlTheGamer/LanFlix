using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Streaming;

/// <summary>
/// Service for cleaning up temporary transcoding files
/// </summary>
public class TranscodingFileCleanupService
{
    private readonly ILogger<TranscodingFileCleanupService> _logger;
    private readonly string _tempPath;

    public TranscodingFileCleanupService(
        IConfiguration configuration,
        ILogger<TranscodingFileCleanupService> logger)
    {
        _logger = logger;
        _tempPath = configuration["Lanflix:Transcoding:TempPath"] 
            ?? Path.Combine(Path.GetTempPath(), "Lanflix", "Transcoding");

        // Ensure temp directory exists
        if (!Directory.Exists(_tempPath))
        {
            Directory.CreateDirectory(_tempPath);
            _logger.LogInformation("Created transcoding temp directory: {Path}", _tempPath);
        }
    }

    /// <summary>
    /// Gets the temporary file path for a session
    /// </summary>
    public string GetSessionTempPath(string sessionId)
    {
        return Path.Combine(_tempPath, sessionId);
    }

    /// <summary>
    /// Creates a temporary directory for a session
    /// </summary>
    public string CreateSessionTempDirectory(string sessionId)
    {
        var sessionPath = GetSessionTempPath(sessionId);
        
        if (!Directory.Exists(sessionPath))
        {
            Directory.CreateDirectory(sessionPath);
            _logger.LogDebug("Created temp directory for session {SessionId}: {Path}", sessionId, sessionPath);
        }

        return sessionPath;
    }

    /// <summary>
    /// Cleans up temporary files for a specific session
    /// </summary>
    public async Task CleanupSessionFilesAsync(string sessionId)
    {
        var sessionPath = GetSessionTempPath(sessionId);

        if (!Directory.Exists(sessionPath))
        {
            return;
        }

        try
        {
            _logger.LogInformation("Cleaning up temp files for session {SessionId}", sessionId);

            // Delete all files in the session directory
            var files = Directory.GetFiles(sessionPath);
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                    _logger.LogDebug("Deleted temp file: {File}", Path.GetFileName(file));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file: {File}", file);
                }
            }

            // Delete the directory
            Directory.Delete(sessionPath, recursive: true);
            _logger.LogInformation("Cleaned up temp directory for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup temp files for session {SessionId}", sessionId);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up all old temporary files (older than specified age)
    /// </summary>
    public async Task CleanupOldFilesAsync(TimeSpan maxAge)
    {
        if (!Directory.Exists(_tempPath))
        {
            return;
        }

        try
        {
            var cutoffTime = DateTime.UtcNow - maxAge;
            var directories = Directory.GetDirectories(_tempPath);
            var cleanedCount = 0;

            foreach (var directory in directories)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(directory);
                    
                    if (dirInfo.LastWriteTimeUtc < cutoffTime)
                    {
                        _logger.LogInformation(
                            "Cleaning up old temp directory: {Directory}, last modified: {LastModified}",
                            dirInfo.Name,
                            dirInfo.LastWriteTimeUtc);

                        Directory.Delete(directory, recursive: true);
                        cleanedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup old directory: {Directory}", directory);
                }
            }

            if (cleanedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old temp directories", cleanedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old temp files");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the total size of temporary files
    /// </summary>
    public long GetTotalTempFileSize()
    {
        if (!Directory.Exists(_tempPath))
        {
            return 0;
        }

        try
        {
            var dirInfo = new DirectoryInfo(_tempPath);
            return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate temp file size");
            return 0;
        }
    }
}
