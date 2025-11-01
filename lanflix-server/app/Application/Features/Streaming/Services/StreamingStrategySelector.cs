using Lanflix.Application.Features.Streaming.Strategies;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Streaming.Services;

/// <summary>
/// Service for selecting the optimal streaming strategy based on media info and client capabilities
/// </summary>
public class StreamingStrategySelector
{
    private readonly IEnumerable<IStreamingStrategy> _strategies;
    private readonly ILogger<StreamingStrategySelector> _logger;

    public StreamingStrategySelector(
        IEnumerable<IStreamingStrategy> strategies,
        ILogger<StreamingStrategySelector> logger)
    {
        _strategies = strategies;
        _logger = logger;
    }

    /// <summary>
    /// Selects the optimal streaming strategy based on media info, client capabilities, and user preferences
    /// </summary>
    /// <param name="media">Media information</param>
    /// <param name="client">Client capabilities</param>
    /// <param name="preferences">User preferences (optional)</param>
    /// <returns>The optimal streaming strategy</returns>
    public IStreamingStrategy SelectOptimalStrategy(
        MediaInfo media,
        ClientCapabilities client,
        UserPreferences? preferences = null)
    {
        _logger.LogInformation(
            "Selecting streaming strategy for media: Container={Container}, VideoCodec={VideoCodec}, AudioCodec={AudioCodec}, Resolution={Width}x{Height}",
            media.Container, media.Video.Codec, media.Audio.FirstOrDefault()?.Codec ?? "none",
            media.Video.Width, media.Video.Height);

        _logger.LogDebug(
            "Client capabilities: VideoCodecs=[{VideoCodecs}], AudioCodecs=[{AudioCodecs}], Containers=[{Containers}], MaxResolution={MaxResolution}, MaxBitrate={MaxBitrate}, SupportsHDR={SupportsHDR}",
            string.Join(", ", client.SupportedVideoCodecs),
            string.Join(", ", client.SupportedAudioCodecs),
            string.Join(", ", client.SupportedContainers),
            client.MaxResolution,
            client.MaxBitrate,
            client.SupportsHDR);

        // Check user preferences for forced transcoding
        if (preferences?.ForceTranscode == true)
        {
            _logger.LogInformation("User preferences force transcoding, skipping DirectPlay/DirectStream");
            var transcodeStrategy = _strategies
                .Where(s => s.Mode == Domain.Enums.StreamingMode.FullTranscode)
                .FirstOrDefault();

            if (transcodeStrategy != null)
            {
                _logger.LogInformation("Selected strategy: {Strategy} (forced by user preference)", transcodeStrategy.Mode);
                return transcodeStrategy;
            }
        }

        // Find all strategies that can handle this request
        var compatibleStrategies = _strategies
            .Where(s => s.CanHandle(media, client))
            .OrderBy(s => s.Priority)
            .ToList();

        if (compatibleStrategies.Count == 0)
        {
            _logger.LogWarning("No compatible strategies found, this should not happen as FullTranscode is fallback");
            throw new InvalidOperationException("No compatible streaming strategy found");
        }

        // Select the strategy with the highest priority (lowest priority number)
        var selectedStrategy = compatibleStrategies.First();

        _logger.LogInformation(
            "Selected streaming strategy: {Strategy} (Priority: {Priority})",
            selectedStrategy.Mode,
            selectedStrategy.Priority);

        // Log alternative strategies that were considered
        if (compatibleStrategies.Count > 1)
        {
            var alternatives = compatibleStrategies.Skip(1).Select(s => $"{s.Mode} (Priority: {s.Priority})");
            _logger.LogDebug("Alternative strategies available: {Alternatives}", string.Join(", ", alternatives));
        }

        return selectedStrategy;
    }

    /// <summary>
    /// Gets all available streaming strategies ordered by priority
    /// </summary>
    /// <returns>List of all strategies</returns>
    public IEnumerable<IStreamingStrategy> GetAllStrategies()
    {
        return _strategies.OrderBy(s => s.Priority);
    }

    /// <summary>
    /// Gets a specific strategy by streaming mode
    /// </summary>
    /// <param name="mode">The streaming mode</param>
    /// <returns>The strategy for the specified mode, or null if not found</returns>
    public IStreamingStrategy? GetStrategyByMode(Domain.Enums.StreamingMode mode)
    {
        return _strategies.FirstOrDefault(s => s.Mode == mode);
    }

    /// <summary>
    /// Tests which strategies can handle the given media and client capabilities
    /// Useful for diagnostics and debugging
    /// </summary>
    /// <param name="media">Media information</param>
    /// <param name="client">Client capabilities</param>
    /// <returns>Dictionary of strategy modes and whether they can handle the request</returns>
    public Dictionary<Domain.Enums.StreamingMode, bool> TestStrategies(
        MediaInfo media,
        ClientCapabilities client)
    {
        var results = new Dictionary<Domain.Enums.StreamingMode, bool>();

        foreach (var strategy in _strategies.OrderBy(s => s.Priority))
        {
            var canHandle = strategy.CanHandle(media, client);
            results[strategy.Mode] = canHandle;

            _logger.LogDebug(
                "Strategy test: {Strategy} (Priority: {Priority}) - CanHandle: {CanHandle}",
                strategy.Mode,
                strategy.Priority,
                canHandle);
        }

        return results;
    }
}
