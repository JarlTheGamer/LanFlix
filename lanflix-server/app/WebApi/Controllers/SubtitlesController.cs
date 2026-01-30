using System.Diagnostics;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubtitlesController : ControllerBase
{
    private readonly ILogger<SubtitlesController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IMediaAnalyzer _mediaAnalyzer;
    private readonly IConfiguration _configuration;

    public SubtitlesController(
        ILogger<SubtitlesController> logger,
        ApplicationDbContext context,
        IMediaAnalyzer mediaAnalyzer,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _mediaAnalyzer = mediaAnalyzer;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets available subtitles for a content item or episode
    /// </summary>
    [HttpGet("{contentId}")]
    public async Task<IActionResult> GetSubtitles(int contentId, [FromQuery] int? episodeId = null)
    {
        try
        {
            string filePath;

            // Handle episode info request
            if (episodeId.HasValue)
            {
                var episode = await _context.Episodes
                    .FirstOrDefaultAsync(e => e.Id == episodeId.Value && e.ContentId == contentId);

                if (episode == null)
                {
                    return NotFound("Episode not found");
                }

                filePath = episode.FilePath;
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    return NotFound("Episode file not found");
                }
            }
            else
            {
                // Handle content info request
                var content = await _context.Contents
                    .FirstOrDefaultAsync(c => c.Id == contentId);

                if (content == null)
                {
                    return NotFound("Content not found");
                }

                filePath = content.FilePath;
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    return NotFound("Content file not found");
                }
            }

            _logger.LogInformation("Scanning for subtitles in: {FilePath}", filePath);

            // Scan for subtitles directly
            var subtitleStreams = await ScanSubtitlesAsync(filePath);

            // Build query string for episode parameter
            var episodeParam = episodeId.HasValue ? $"?episodeId={episodeId.Value}" : "";

            // Use the correct base URL for the client to construct tracks
            var subtitles = subtitleStreams.Select(s => new
            {
                Index = s.Index,
                Language = s.Language ?? "unknown",
                Title = s.Title ?? $"Subtitle {s.Index + 1}",
                Format = s.Format,
                IsForced = s.IsForced,
                IsDefault = s.IsDefault,
                IsEmbedded = s.IsEmbedded,
                Url = $"/api/subtitles/track/{contentId}/{s.Index}{episodeParam}"
            }).ToArray();

            _logger.LogInformation("Found {Count} subtitle tracks", subtitles.Length);

            return Ok(new { Subtitles = subtitles });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subtitles for content: {ContentId}", contentId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Extracts and serves a specific subtitle track as WebVTT
    /// </summary>
    [HttpGet("track/{contentId}/{subtitleIndex}")]
    public async Task<IActionResult> GetSubtitleTrack(int contentId, int subtitleIndex, [FromQuery] int? episodeId = null, [FromQuery] double? startTime = null)
    {
        try
        {
            string filePath;

            if (episodeId.HasValue)
            {
                var episode = await _context.Episodes
                    .FirstOrDefaultAsync(e => e.Id == episodeId.Value && e.ContentId == contentId);

                if (episode == null)
                {
                    _logger.LogWarning("Episode not found: {EpisodeId}", episodeId);
                    return NotFound("Episode not found");
                }
                filePath = episode.FilePath;
            }
            else
            {
                var content = await _context.Contents
                    .FirstOrDefaultAsync(c => c.Id == contentId);

                if (content == null)
                {
                    _logger.LogWarning("Content not found: {ContentId}", contentId);
                    return NotFound("Content not found");
                }
                filePath = content.FilePath;
            }

            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("Media file not found: {FilePath}", filePath);
                return NotFound("Media file not found");
            }

            _logger.LogInformation("Extracting subtitle track {SubtitleIndex} from {FilePath}", subtitleIndex, filePath);

            var vttContent = await ExtractSubtitleAsWebVTT(filePath, subtitleIndex, startTime);

            if (vttContent == null)
            {
                _logger.LogWarning("Subtitle extraction returned null for track {SubtitleIndex}", subtitleIndex);
                return NotFound("Subtitle track not found or extraction failed");
            }

            _logger.LogInformation("Successfully extracted subtitle track {SubtitleIndex}, length: {Length} chars", subtitleIndex, vttContent.Length);
            return Content(vttContent, "text/vtt");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subtitle track {SubtitleIndex} for content {ContentId}", subtitleIndex, contentId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Extracts subtitle from video file and converts to WebVTT format
    /// </summary>
    private async Task<string?> ExtractSubtitleAsWebVTT(string filePath, int subtitleIndex, double? startTime = null)
    {
        try
        {
            var subtitleStreams = await ScanSubtitlesAsync(filePath);
            var subtitle = subtitleStreams.FirstOrDefault(s => s.Index == subtitleIndex);

            if (subtitle == null)
            {
                _logger.LogWarning("Subtitle index {Index} not found in file", subtitleIndex);
                return null;
            }

            _logger.LogInformation("Found subtitle: Index={Index}, Embedded={IsEmbedded}, Format={Format}", 
                subtitle.Index, subtitle.IsEmbedded, subtitle.Format);

            // Handle external subtitle files
            if (!subtitle.IsEmbedded && !string.IsNullOrEmpty(subtitle.ExternalFilePath) && System.IO.File.Exists(subtitle.ExternalFilePath))
            {
                var extension = Path.GetExtension(subtitle.ExternalFilePath).ToLower();
                var content = await System.IO.File.ReadAllTextAsync(subtitle.ExternalFilePath);

                if (extension == ".vtt")
                {
                    return content;
                }
                else if (extension == ".srt")
                {
                    return ConvertSrtToWebVTT(content, startTime);
                }
                else
                {
                    return await ConvertSubtitleToWebVTT(subtitle.ExternalFilePath, startTime);
                }
            }

            // Handle embedded subtitles - extract using FFmpeg
            var ffmpegPath = FindFFmpegPath();
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"subtitle_{Guid.NewGuid()}.vtt");

            try
            {
                // Map by stream index, not subtitle stream index
                var seekArgs = startTime.HasValue && startTime.Value > 0 ? $"-ss {startTime.Value:F3} " : "";
                var arguments = $"{seekArgs}-i \"{filePath}\" -map 0:{subtitleIndex} -f webvtt \"{tempOutputPath}\"";

                _logger.LogInformation("Running FFmpeg: {FFmpegPath} {Arguments}", ffmpegPath, arguments);

                var startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    _logger.LogError("Failed to start FFmpeg process");
                    return null;
                }

                var errorOutput = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && System.IO.File.Exists(tempOutputPath))
                {
                    var result = await System.IO.File.ReadAllTextAsync(tempOutputPath);
                    _logger.LogInformation("Successfully extracted subtitle, length: {Length} chars", result.Length);
                    return result;
                }
                else
                {
                    _logger.LogError("FFmpeg subtitle extraction failed with exit code {ExitCode}: {Error}", 
                        process.ExitCode, errorOutput);
                    return null;
                }
            }
            finally
            {
                if (System.IO.File.Exists(tempOutputPath))
                {
                    try { System.IO.File.Delete(tempOutputPath); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting subtitle index {Index} from {FilePath}", subtitleIndex, filePath);
            return null;
        }
    }

    /// <summary>
    /// Scans for embedded and external subtitle tracks
    /// </summary>
    private async Task<List<SubtitleStream>> ScanSubtitlesAsync(string filePath)
    {
        var allSubtitles = new List<SubtitleStream>();

        try
        {
            // Scan for embedded subtitles using FFprobe
            var embeddedSubtitles = await ScanEmbeddedSubtitlesAsync(filePath);
            allSubtitles.AddRange(embeddedSubtitles);

            // Scan for external subtitle files
            var externalSubtitles = ScanExternalSubtitles(filePath, embeddedSubtitles.Count);
            allSubtitles.AddRange(externalSubtitles);

            _logger.LogInformation("Found {Total} subtitles (Embedded: {Embedded}, External: {External})",
                allSubtitles.Count, embeddedSubtitles.Count, externalSubtitles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan subtitles for: {FilePath}", filePath);
        }

        return allSubtitles;
    }

    /// <summary>
    /// Scans for embedded subtitle streams using FFprobe
    /// </summary>
    private async Task<List<SubtitleStream>> ScanEmbeddedSubtitlesAsync(string filePath)
    {
        var subtitles = new List<SubtitleStream>();

        try
        {
            var ffprobePath = FindFFprobePath();
            var arguments = $"-v error -print_format json -show_streams -select_streams s \"{filePath}\"";

            _logger.LogInformation("Running FFprobe for subtitles: {FFprobePath} {Arguments}", ffprobePath, arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start FFprobe process");
                return subtitles;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            _logger.LogInformation("FFprobe exit code: {ExitCode}", process.ExitCode);
            
            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("FFprobe stderr: {Error}", error);
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.LogInformation("FFprobe output length: {Length} characters", output.Length);
                
                // Log first stream to see structure
                if (output.Length > 500)
                {
                    _logger.LogDebug("FFprobe output sample: {Sample}", output.Substring(0, 500));
                }
            }
            else
            {
                _logger.LogWarning("FFprobe returned empty output");
            }

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                var probeResult = System.Text.Json.JsonSerializer.Deserialize<FFprobeResult>(output, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (probeResult?.Streams != null && probeResult.Streams.Count > 0)
                {
                    _logger.LogInformation("FFprobe found {Count} streams", probeResult.Streams.Count);
                    
                    // Since we used -select_streams s, ALL returned streams are subtitle streams
                    // We don't need to filter by CodecType
                    subtitles = probeResult.Streams
                        .Select((stream, index) => new SubtitleStream
                        {
                            Index = stream.Index ?? index,
                            Format = stream.CodecName ?? "unknown",
                            Language = stream.Tags?.Language,
                            Title = stream.Tags?.Title,
                            IsDefault = stream.Disposition?.Default == 1,
                            IsForced = stream.Disposition?.Forced == 1,
                            IsEmbedded = true,
                            ExternalFilePath = null
                        })
                        .ToList();
                    
                    _logger.LogInformation("Extracted {Count} subtitle streams", subtitles.Count);
                }
                else
                {
                    _logger.LogInformation("FFprobe result has no streams");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan embedded subtitles for: {FilePath}", filePath);
        }

        return subtitles;
    }

    /// <summary>
    /// Scans for external subtitle files
    /// </summary>
    private List<SubtitleStream> ScanExternalSubtitles(string videoFilePath, int startIndex)
    {
        var externalSubtitles = new List<SubtitleStream>();

        try
        {
            var directory = Path.GetDirectoryName(videoFilePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(videoFilePath);

            if (string.IsNullOrEmpty(directory))
                return externalSubtitles;

            var subtitleExtensions = new[] { ".srt", ".vtt", ".ass", ".ssa", ".sub" };

            var subtitleFiles = Directory.GetFiles(directory, $"{fileNameWithoutExt}*")
                .Where(f => subtitleExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            int index = startIndex;
            foreach (var subtitleFile in subtitleFiles)
            {
                var fileName = Path.GetFileName(subtitleFile);
                var extension = Path.GetExtension(subtitleFile).ToLowerInvariant();
                var language = ExtractLanguageFromFilename(fileName);
                var isForced = fileName.Contains(".forced.", StringComparison.OrdinalIgnoreCase);

                externalSubtitles.Add(new SubtitleStream
                {
                    Index = index++,
                    Format = extension.TrimStart('.'),
                    Language = language,
                    Title = $"External - {language ?? "Unknown"}",
                    IsDefault = false,
                    IsForced = isForced,
                    IsEmbedded = false,
                    ExternalFilePath = subtitleFile
                });

                _logger.LogInformation("Found external subtitle: {FileName}, Language: {Language}", fileName, language ?? "unknown");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan external subtitles");
        }

        return externalSubtitles;
    }

    /// <summary>
    /// Extracts language code from subtitle filename
    /// </summary>
    private string? ExtractLanguageFromFilename(string filename)
    {
        var patterns = new Dictionary<string, string>
        {
            { "en", "eng" }, { "eng", "eng" }, { "english", "eng" },
            { "es", "spa" }, { "spa", "spa" }, { "spanish", "spa" },
            { "fr", "fra" }, { "fra", "fra" }, { "french", "fra" },
            { "de", "ger" }, { "ger", "ger" }, { "german", "ger" },
            { "it", "ita" }, { "ita", "ita" }, { "italian", "ita" },
            { "pt", "por" }, { "por", "por" }, { "portuguese", "por" },
            { "ja", "jpn" }, { "jpn", "jpn" }, { "japanese", "jpn" },
            { "ko", "kor" }, { "kor", "kor" }, { "korean", "kor" },
            { "zh", "chi" }, { "chi", "chi" }, { "chinese", "chi" },
            { "ar", "ara" }, { "ara", "ara" }, { "arabic", "ara" },
            { "ru", "rus" }, { "rus", "rus" }, { "russian", "rus" },
            { "hi", "hin" }, { "hin", "hin" }, { "hindi", "hin" }
        };

        var lowerFilename = filename.ToLowerInvariant();

        foreach (var pattern in patterns)
        {
            if (lowerFilename.Contains($".{pattern.Key}.") ||
                lowerFilename.Contains($".{pattern.Key}_") ||
                lowerFilename.EndsWith($".{pattern.Key}"))
            {
                return pattern.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Converts SRT subtitle format to WebVTT
    /// </summary>
    private string ConvertSrtToWebVTT(string srtContent, double? startTime = null)
    {
        // Simple SRT to WebVTT conversion
        var lines = srtContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var vttLines = new List<string> { "WEBVTT", "" };
        
        var offset = startTime.HasValue ? TimeSpan.FromSeconds(startTime.Value) : TimeSpan.Zero;

        foreach (var line in lines)
        {
            // Replace SRT timestamp format (00:00:00,000) with WebVTT format (00:00:00.000)
            if (line.Contains("-->"))
            {
                // Parse timestamps and apply offset
                if (startTime.HasValue && startTime.Value > 0)
                {
                    try 
                    {
                        var parts = line.Split("-->");
                        if (parts.Length == 2)
                        {
                            var start = ParseSrtTimestamp(parts[0].Trim());
                            var end = ParseSrtTimestamp(parts[1].Trim());
                            
                            start -= offset;
                            end -= offset;
                            
                            // Skip lines that are fully before the start time
                            if (end < TimeSpan.Zero) continue;
                            if (start < TimeSpan.Zero) start = TimeSpan.Zero;
                            
                            vttLines.Add($"{FormatVttTimestamp(start)} --> {FormatVttTimestamp(end)}");
                            continue;
                        }
                    }
                    catch 
                    {
                        // Fallback to simple replacement if parsing fails
                    }
                }
                
                vttLines.Add(line.Replace(',', '.'));
            }
            else
            {
                vttLines.Add(line);
            }
        }

        return string.Join("\n", vttLines);
    }
    
    private TimeSpan ParseSrtTimestamp(string timestamp)
    {
        // Format: 00:00:00,000
        return TimeSpan.ParseExact(timestamp.Replace(',', '.'), @"hh\:mm\:ss\.fff", null);
    }
    
    private string FormatVttTimestamp(TimeSpan timestamp)
    {
        return timestamp.ToString(@"hh\:mm\:ss\.fff");
    }

    /// <summary>
    /// Converts any subtitle format to WebVTT using FFmpeg
    /// </summary>
    private async Task<string?> ConvertSubtitleToWebVTT(string subtitleFilePath, double? startTime = null)
    {
        try
        {
            var ffmpegPath = FindFFmpegPath();
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"subtitle_{Guid.NewGuid()}.vtt");

            try
            {
                // Add -ss if startTime is provided
                var seekArgs = startTime.HasValue && startTime.Value > 0 ? $"-ss {startTime.Value:F3} " : "";
                var arguments = $"{seekArgs}-i \"{subtitleFilePath}\" \"{tempOutputPath}\"";

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && System.IO.File.Exists(tempOutputPath))
                {
                    return await System.IO.File.ReadAllTextAsync(tempOutputPath);
                }

                return null;
            }
            finally
            {
                if (System.IO.File.Exists(tempOutputPath))
                {
                    try { System.IO.File.Delete(tempOutputPath); } catch { }
                }
            }
        }
        catch
        {
            return null;
        }
    }

    private string FindFFmpegPath()
    {
        // Check for local ffmpeg-path file first
        if (System.IO.File.Exists("ffmpeg-path"))
        {
            var path = System.IO.File.ReadAllText("ffmpeg-path").Trim();
            if (System.IO.File.Exists(path))
            {
                return path;
            }
        }

        // Check common locations
        var commonPaths = new[]
        {
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"/usr/bin/ffmpeg",
            @"/usr/local/bin/ffmpeg"
        };

        foreach (var path in commonPaths)
        {
            if (System.IO.File.Exists(path)) return path;
        }

        // Fallback to just "ffmpeg" and hope it's in PATH
        return "ffmpeg";
    }

    private string FindFFprobePath()
    {
        // Check for local ffprobe-path file first
        if (System.IO.File.Exists("ffprobe-path"))
        {
            var path = System.IO.File.ReadAllText("ffprobe-path").Trim();
            if (System.IO.File.Exists(path))
            {
                return path;
            }
        }

        // Check common locations
        var commonPaths = new[]
        {
            @"C:\ffmpeg\bin\ffprobe.exe",
            @"/usr/bin/ffprobe",
            @"/usr/local/bin/ffprobe"
        };

        foreach (var path in commonPaths)
        {
            if (System.IO.File.Exists(path)) return path;
        }

        // Fallback to just "ffprobe" and hope it's in PATH
        return "ffprobe";
    }

    #region FFprobe JSON Models

    private class FFprobeResult
    {
        public List<FFprobeStream>? Streams { get; set; }
    }

    private class FFprobeStream
    {
        public int? Index { get; set; }
        public string? CodecName { get; set; }
        public string? CodecType { get; set; }
        public FFprobeDisposition? Disposition { get; set; }
        public FFprobeTags? Tags { get; set; }
    }

    private class FFprobeDisposition
    {
        public int? Default { get; set; }
        public int? Forced { get; set; }
    }

    private class FFprobeTags
    {
        public string? Language { get; set; }
        public string? Title { get; set; }
    }

    #endregion
}

// Helper class for subtitle stream info
public class SubtitleStream
{
    public int Index { get; set; }
    public string Format { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? Title { get; set; }
    public bool IsDefault { get; set; }
    public bool IsForced { get; set; }
    public bool IsEmbedded { get; set; }
    public string? ExternalFilePath { get; set; }
}
