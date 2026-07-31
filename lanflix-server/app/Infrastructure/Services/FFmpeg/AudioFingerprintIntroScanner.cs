using System.Diagnostics;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.FFmpeg;

/// <summary>
/// Service that implements Jellyfin-style audio fingerprinting cross-correlation
/// to automatically detect TV series intro sequences across season episodes.
/// </summary>
public class AudioFingerprintIntroScanner : IIntroScanner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AudioFingerprintIntroScanner> _logger;
    private readonly string _ffmpegPath;

    public AudioFingerprintIntroScanner(
        IServiceScopeFactory scopeFactory,
        ILogger<AudioFingerprintIntroScanner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _ffmpegPath = FindFFmpegPath();
    }

    public async Task ScanSeasonIntrosAsync(int seriesId, int seasonNumber, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var episodes = await context.Episodes
            .Where(e => e.ContentId == seriesId && e.SeasonNumber == seasonNumber && !string.IsNullOrEmpty(e.FilePath))
            .OrderBy(e => e.EpisodeNumber)
            .ToListAsync(cancellationToken);

        if (episodes.Count < 2)
        {
            _logger.LogInformation("Season {Season} of Series {SeriesId} has fewer than 2 episodes; skipping intro fingerprinting.",
                seasonNumber, seriesId);
            return;
        }

        _logger.LogInformation("Starting audio fingerprinting intro scan for Series {SeriesId} Season {Season} ({Count} episodes)...",
            seriesId, seasonNumber, episodes.Count);

        // 1. Extract downsampled audio energy envelopes (first 10 minutes = 600s, 20 samples/sec)
        var envelopes = new Dictionary<int, float[]>();
        foreach (var ep in episodes)
        {
            if (File.Exists(ep.FilePath))
            {
                var env = await ExtractAudioEnergyEnvelopeAsync(ep.FilePath, 600, cancellationToken);
                if (env.Length > 0)
                {
                    envelopes[ep.Id] = env;
                }
            }
        }

        if (envelopes.Count < 2)
        {
            _logger.LogWarning("Insufficient audio data extracted for intro scanning.");
            return;
        }

        // 2. Find common matching audio segment across episode pairs
        var epList = episodes.Where(e => envelopes.ContainsKey(e.Id)).ToList();
        var epStarts = new Dictionary<int, List<double>>();
        var epEnds = new Dictionary<int, List<double>>();

        for (int i = 0; i < epList.Count - 1; i++)
        {
            var ep1 = epList[i];
            var ep2 = epList[i + 1];
            var env1 = envelopes[ep1.Id];
            var env2 = envelopes[ep2.Id];

            var match = FindLongestMatchingSegment(env1, env2, samplesPerSec: 20, minSec: 15, maxSec: 120);
            if (match.HasValue)
            {
                if (!epStarts.ContainsKey(ep1.Id)) epStarts[ep1.Id] = new List<double>();
                if (!epEnds.ContainsKey(ep1.Id)) epEnds[ep1.Id] = new List<double>();
                epStarts[ep1.Id].Add(match.Value.Start1);
                epEnds[ep1.Id].Add(match.Value.End1);

                if (!epStarts.ContainsKey(ep2.Id)) epStarts[ep2.Id] = new List<double>();
                if (!epEnds.ContainsKey(ep2.Id)) epEnds[ep2.Id] = new List<double>();
                epStarts[ep2.Id].Add(match.Value.Start2);
                epEnds[ep2.Id].Add(match.Value.End2);
            }
        }

        if (epStarts.Count > 0)
        {
            foreach (var ep in episodes)
            {
                if (epStarts.TryGetValue(ep.Id, out var starts) && epEnds.TryGetValue(ep.Id, out var ends) && starts.Count > 0)
                {
                    ep.IntroStartTime = Math.Round(starts.Average(), 1);
                    ep.IntroEndTime = Math.Round(ends.Average(), 1);
                    _logger.LogInformation("  -> Ep {Num} ({Title}): Intro detected at {Start:F1}s - {End:F1}s",
                        ep.EpisodeNumber, ep.Title, ep.IntroStartTime, ep.IntroEndTime);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Completed audio fingerprint intro scan for Series {SeriesId} Season {Season}. Saved per-episode markers to DB.", seriesId, seasonNumber);
        }
        else
        {
            _logger.LogInformation("Completed audio fingerprint scan: No clear intro audio match detected across season {Season} episodes.", seasonNumber);
        }
    }

    private async Task<float[]> ExtractAudioEnergyEnvelopeAsync(string filePath, int durationSec, CancellationToken cancellationToken)
    {
        try
        {
            // Extract mono 8000Hz PCM raw s16le audio quietly (-v quiet)
            var arguments = $"-v quiet -ss 0 -t {durationSec} -i \"{filePath}\" -ac 1 -ar 8000 -f s16le -";
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            using var ms = new MemoryStream();
            await process.StandardOutput.BaseStream.CopyToAsync(ms, cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            byte[] pcm = ms.ToArray();
            int sampleCount = pcm.Length / 2;
            if (sampleCount == 0) return Array.Empty<float>();

            // Chunk into 50ms windows (400 samples at 8000Hz = 20 samples per second)
            int windowSize = 400;
            int numWindows = sampleCount / windowSize;
            float[] envelope = new float[numWindows];

            for (int w = 0; w < numWindows; w++)
            {
                double sumSquare = 0;
                for (int s = 0; s < windowSize; s++)
                {
                    int idx = (w * windowSize + s) * 2;
                    if (idx + 1 < pcm.Length)
                    {
                        short sample = (short)(pcm[idx] | (pcm[idx + 1] << 8));
                        sumSquare += (sample / 32768.0) * (sample / 32768.0);
                    }
                }
                envelope[w] = (float)Math.Sqrt(sumSquare / windowSize);
            }

            return envelope;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract audio envelope for {FilePath}", filePath);
            return Array.Empty<float>();
        }
    }

    /// <summary>
    /// Fast coarse-to-fine cross correlation search to find identical intro music across episode pairs
    /// </summary>
    private (double Start1, double End1, double Start2, double End2)? FindLongestMatchingSegment(
        float[] env1, float[] env2, int samplesPerSec, int minSec, int maxSec)
    {
        // Intros occur in the first 5 minutes (300 seconds)
        int maxOffsetSec = 300;
        int maxOffsetSamples1 = Math.Min(env1.Length, maxOffsetSec * samplesPerSec);
        int maxOffsetSamples2 = Math.Min(env2.Length, maxOffsetSec * samplesPerSec);

        int probeSec = 20;
        int probeSamples = probeSec * samplesPerSec;
        int stepSec = 1; // 1-second coarse step
        int stepSamples = stepSec * samplesPerSec;

        double bestScore = 0.72; // High confidence correlation threshold
        (double Start1, double End1, double Start2, double End2)? bestMatch = null;

        // 1. Coarse search using 20s probe window
        for (int o1 = 0; o1 + probeSamples <= maxOffsetSamples1; o1 += stepSamples)
        {
            for (int o2 = 0; o2 + probeSamples <= maxOffsetSamples2; o2 += stepSamples)
            {
                double probeScore = CalculateNormalizedCorrelation(env1, o1, env2, o2, probeSamples);
                if (probeScore > bestScore)
                {
                    // 2. Expand matching segment forwards and backwards
                    int startOffset1 = o1;
                    int endOffset1 = o1 + probeSamples;

                    // Expand forward (stricter 0.78 threshold to prevent overshooting into episode audio)
                    while (endOffset1 + samplesPerSec <= env1.Length && 
                           (o2 + (endOffset1 - o1) + samplesPerSec) <= env2.Length)
                    {
                        double check = CalculateNormalizedCorrelation(env1, endOffset1 - samplesPerSec, env2, o2 + (endOffset1 - o1) - samplesPerSec, samplesPerSec * 2);
                        if (check >= 0.78)
                        {
                            endOffset1 += samplesPerSec;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Expand backward
                    while (startOffset1 - samplesPerSec >= 0 && (o2 - (o1 - startOffset1) - samplesPerSec) >= 0)
                    {
                        double check = CalculateNormalizedCorrelation(env1, startOffset1 - samplesPerSec, env2, o2 - (o1 - startOffset1) - samplesPerSec, samplesPerSec * 2);
                        if (check >= 0.72)
                        {
                            startOffset1 -= samplesPerSec;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Trim 1.0s off the end to ensure "Skip Intro" lands cleanly before dialogue begins
                    int trimmedEnd1 = Math.Max(startOffset1 + (minSec * samplesPerSec), endOffset1 - samplesPerSec);
                    int matchLenSec = (trimmedEnd1 - startOffset1) / samplesPerSec;
                    if (matchLenSec >= minSec && matchLenSec <= maxSec)
                    {
                        int startOffset2 = o2 - (o1 - startOffset1);
                        int endOffset2 = o2 + (trimmedEnd1 - o1);
                        double matchScore = CalculateNormalizedCorrelation(env1, startOffset1, env2, startOffset2, trimmedEnd1 - startOffset1);
                        if (matchScore > bestScore)
                        {
                            bestScore = matchScore;
                            bestMatch = (
                                (double)startOffset1 / samplesPerSec,
                                (double)trimmedEnd1 / samplesPerSec,
                                (double)startOffset2 / samplesPerSec,
                                (double)endOffset2 / samplesPerSec
                            );
                        }
                    }
                }
            }
        }

        return bestMatch;
    }

    private double CalculateNormalizedCorrelation(float[] a, int offsetA, float[] b, int offsetB, int len)
    {
        double dotProduct = 0;
        double normA = 0;
        double normB = 0;

        for (int i = 0; i < len; i++)
        {
            double valA = a[offsetA + i];
            double valB = b[offsetB + i];
            dotProduct += valA * valB;
            normA += valA * valA;
            normB += valB * valB;
        }

        if (normA == 0 || normB == 0) return 0;
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private string FindFFmpegPath()
    {
        var appPath = AppDomain.CurrentDomain.BaseDirectory;
        var localFmpeg = Path.Combine(appPath, "ffmpeg.exe");
        if (File.Exists(localFmpeg)) return localFmpeg;

        var envPath = Environment.GetEnvironmentVariable("PATH");
        if (envPath != null)
        {
            foreach (var path in envPath.Split(Path.PathSeparator))
            {
                var full = Path.Combine(path, "ffmpeg.exe");
                if (File.Exists(full)) return full;
            }
        }

        return "ffmpeg";
    }
}
