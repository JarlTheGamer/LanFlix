using System.Globalization;
using System.Text.RegularExpressions;
using Lanflix.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.FFmpeg;

/// <summary>
/// Parses FFmpeg output to extract progress information
/// </summary>
public partial class FFmpegProgressParser
{
    private readonly ILogger<FFmpegProgressParser> _logger;

    // FFmpeg progress line format:
    // frame=  123 fps= 45 q=28.0 size=    1024kB time=00:00:05.12 bitrate=1638.4kbits/s speed=1.87x
    [GeneratedRegex(@"frame=\s*(\d+)")]
    private static partial Regex FrameRegex();

    [GeneratedRegex(@"fps=\s*([\d.]+)")]
    private static partial Regex FpsRegex();

    [GeneratedRegex(@"bitrate=\s*([\d.]+)kbits/s")]
    private static partial Regex BitrateRegex();

    [GeneratedRegex(@"size=\s*(\d+)kB")]
    private static partial Regex SizeRegex();

    [GeneratedRegex(@"time=(\d{2}):(\d{2}):([\d.]+)")]
    private static partial Regex TimeRegex();

    [GeneratedRegex(@"speed=\s*([\d.]+)x")]
    private static partial Regex SpeedRegex();

    public FFmpegProgressParser(ILogger<FFmpegProgressParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses an FFmpeg progress line and extracts progress information
    /// </summary>
    /// <param name="line">The FFmpeg output line</param>
    /// <param name="sessionId">The session ID</param>
    /// <param name="totalDuration">Total duration of the video in seconds</param>
    /// <returns>Transcoding progress or null if line doesn't contain progress info</returns>
    public TranscodingProgress? ParseProgressLine(string line, string sessionId, double totalDuration)
    {
        if (string.IsNullOrWhiteSpace(line) || !line.Contains("frame="))
        {
            return null;
        }

        try
        {
            var frame = ParseLong(FrameRegex().Match(line));
            var fps = ParseDouble(FpsRegex().Match(line));
            var bitrate = ParseDouble(BitrateRegex().Match(line)) * 1000; // Convert to bits/s
            var size = ParseLong(SizeRegex().Match(line)) * 1024; // Convert to bytes
            var currentTime = ParseTime(TimeRegex().Match(line));
            var speed = ParseDouble(SpeedRegex().Match(line));

            var percentComplete = totalDuration > 0 
                ? Math.Min(100, (currentTime / totalDuration) * 100) 
                : 0;

            return new TranscodingProgress
            {
                SessionId = sessionId,
                Frame = frame,
                Fps = fps,
                Bitrate = (long)bitrate,
                TotalSize = size,
                CurrentTime = currentTime,
                TotalDuration = totalDuration,
                PercentComplete = percentComplete,
                Speed = speed
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse FFmpeg progress line: {Line}", line);
            return null;
        }
    }

    private static long ParseLong(Match match)
    {
        if (match.Success && match.Groups.Count > 1)
        {
            if (long.TryParse(match.Groups[1].Value, out var value))
            {
                return value;
            }
        }
        return 0;
    }

    private static double ParseDouble(Match match)
    {
        if (match.Success && match.Groups.Count > 1)
        {
            if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
        }
        return 0;
    }

    private static double ParseTime(Match match)
    {
        if (match.Success && match.Groups.Count > 3)
        {
            if (int.TryParse(match.Groups[1].Value, out var hours) &&
                int.TryParse(match.Groups[2].Value, out var minutes) &&
                double.TryParse(match.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            {
                return hours * 3600 + minutes * 60 + seconds;
            }
        }
        return 0;
    }
}
