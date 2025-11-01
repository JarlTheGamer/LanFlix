using FluentAssertions;
using Lanflix.Application.Features.Streaming.Services;
using Lanflix.Application.Features.Streaming.Strategies;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Lanflix.Application.Tests.Features.Streaming.Services;

public class StreamingStrategySelectorTests
{
    private readonly StreamingStrategySelector _selector;
    private readonly ILogger<StreamingStrategySelector> _logger;
    private readonly List<IStreamingStrategy> _strategies;

    public StreamingStrategySelectorTests()
    {
        _logger = Substitute.For<ILogger<StreamingStrategySelector>>();
        
        // Create mock strategies
        _strategies = new List<IStreamingStrategy>
        {
            CreateMockStrategy(StreamingMode.DirectPlay, 1),
            CreateMockStrategy(StreamingMode.DirectStream, 2),
            CreateMockStrategy(StreamingMode.TranscodeVideo, 3),
            CreateMockStrategy(StreamingMode.FullTranscode, 4)
        };

        _selector = new StreamingStrategySelector(_strategies, _logger);
    }

    [Fact]
    public void SelectOptimalStrategy_WhenDirectPlayPossible_SelectsDirectPlay()
    {
        // Arrange
        var media = CreateMediaInfo("h264", "aac", "mp4");
        var client = CreateClientCapabilities(
            new[] { "h264" },
            new[] { "aac" },
            new[] { "mp4" });

        // Configure DirectPlay to return true
        _strategies[0].CanHandle(media, client).Returns(true);
        _strategies[1].CanHandle(media, client).Returns(false);
        _strategies[2].CanHandle(media, client).Returns(false);
        _strategies[3].CanHandle(media, client).Returns(true);

        // Act
        var result = _selector.SelectOptimalStrategy(media, client);

        // Assert
        result.Mode.Should().Be(StreamingMode.DirectPlay);
        result.Priority.Should().Be(1);
    }

    [Fact]
    public void SelectOptimalStrategy_WhenDirectPlayNotPossibleButDirectStreamIs_SelectsDirectStream()
    {
        // Arrange
        var media = CreateMediaInfo("h264", "aac", "mkv");
        var client = CreateClientCapabilities(
            new[] { "h264" },
            new[] { "aac" },
            new[] { "mp4" });

        // Configure strategies
        _strategies[0].CanHandle(media, client).Returns(false);
        _strategies[1].CanHandle(media, client).Returns(true);
        _strategies[2].CanHandle(media, client).Returns(false);
        _strategies[3].CanHandle(media, client).Returns(true);

        // Act
        var result = _selector.SelectOptimalStrategy(media, client);

        // Assert
        result.Mode.Should().Be(StreamingMode.DirectStream);
        result.Priority.Should().Be(2);
    }

    [Fact]
    public void SelectOptimalStrategy_WhenOnlyTranscodeVideoPossible_SelectsTranscodeVideo()
    {
        // Arrange
        var media = CreateMediaInfo("av1", "aac", "mkv");
        var client = CreateClientCapabilities(
            new[] { "h264" },
            new[] { "aac" },
            new[] { "mp4" });

        // Configure strategies
        _strategies[0].CanHandle(media, client).Returns(false);
        _strategies[1].CanHandle(media, client).Returns(false);
        _strategies[2].CanHandle(media, client).Returns(true);
        _strategies[3].CanHandle(media, client).Returns(true);

        // Act
        var result = _selector.SelectOptimalStrategy(media, client);

        // Assert
        result.Mode.Should().Be(StreamingMode.TranscodeVideo);
        result.Priority.Should().Be(3);
    }

    [Fact]
    public void SelectOptimalStrategy_WhenNothingElsePossible_SelectsFullTranscode()
    {
        // Arrange
        var media = CreateMediaInfo("av1", "flac", "mkv");
        var client = CreateClientCapabilities(
            new[] { "h264" },
            new[] { "aac" },
            new[] { "mp4" });

        // Configure strategies - only FullTranscode returns true
        _strategies[0].CanHandle(media, client).Returns(false);
        _strategies[1].CanHandle(media, client).Returns(false);
        _strategies[2].CanHandle(media, client).Returns(false);
        _strategies[3].CanHandle(media, client).Returns(true);

        // Act
        var result = _selector.SelectOptimalStrategy(media, client);

        // Assert
        result.Mode.Should().Be(StreamingMode.FullTranscode);
        result.Priority.Should().Be(4);
    }

    [Fact]
    public void SelectOptimalStrategy_WithForceTranscodePreference_SelectsFullTranscode()
    {
        // Arrange
        var media = CreateMediaInfo("h264", "aac", "mp4");
        var client = CreateClientCapabilities(
            new[] { "h264" },
            new[] { "aac" },
            new[] { "mp4" });
        var preferences = new UserPreferences { ForceTranscode = true };

        // Configure all strategies to return true
        _strategies[0].CanHandle(media, client).Returns(true);
        _strategies[1].CanHandle(media, client).Returns(true);
        _strategies[2].CanHandle(media, client).Returns(true);
        _strategies[3].CanHandle(media, client).Returns(true);

        // Act
        var result = _selector.SelectOptimalStrategy(media, client, preferences);

        // Assert
        result.Mode.Should().Be(StreamingMode.FullTranscode);
    }

    [Fact]
    public void GetAllStrategies_ReturnsStrategiesOrderedByPriority()
    {
        // Act
        var result = _selector.GetAllStrategies().ToList();

        // Assert
        result.Should().HaveCount(4);
        result[0].Priority.Should().Be(1);
        result[1].Priority.Should().Be(2);
        result[2].Priority.Should().Be(3);
        result[3].Priority.Should().Be(4);
    }

    [Fact]
    public void GetStrategyByMode_WithValidMode_ReturnsCorrectStrategy()
    {
        // Act
        var result = _selector.GetStrategyByMode(StreamingMode.DirectPlay);

        // Assert
        result.Should().NotBeNull();
        result!.Mode.Should().Be(StreamingMode.DirectPlay);
    }

    [Fact]
    public void GetStrategyByMode_WithInvalidMode_ReturnsNull()
    {
        // Act
        var result = _selector.GetStrategyByMode((StreamingMode)999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TestStrategies_ReturnsCapabilityMapForAllStrategies()
    {
        // Arrange
        var media = CreateMediaInfo("h264", "aac", "mp4");
        var client = CreateClientCapabilities(
            new[] { "h264" },
            new[] { "aac" },
            new[] { "mp4" });

        // Configure strategies
        _strategies[0].CanHandle(media, client).Returns(true);
        _strategies[1].CanHandle(media, client).Returns(false);
        _strategies[2].CanHandle(media, client).Returns(false);
        _strategies[3].CanHandle(media, client).Returns(true);

        // Act
        var result = _selector.TestStrategies(media, client);

        // Assert
        result.Should().HaveCount(4);
        result[StreamingMode.DirectPlay].Should().BeTrue();
        result[StreamingMode.DirectStream].Should().BeFalse();
        result[StreamingMode.TranscodeVideo].Should().BeFalse();
        result[StreamingMode.FullTranscode].Should().BeTrue();
    }

    // Helper methods
    private IStreamingStrategy CreateMockStrategy(StreamingMode mode, int priority)
    {
        var strategy = Substitute.For<IStreamingStrategy>();
        strategy.Mode.Returns(mode);
        strategy.Priority.Returns(priority);
        return strategy;
    }

    private MediaInfo CreateMediaInfo(
        string videoCodec,
        string audioCodec,
        string container)
    {
        return new MediaInfo
        {
            Video = new VideoStream
            {
                Codec = videoCodec,
                Width = 1920,
                Height = 1080,
                Bitrate = 8_000_000,
                FrameRate = 24.0,
                PixelFormat = "yuv420p",
                IsHDR = false
            },
            Audio = new List<AudioStream>
            {
                new AudioStream
                {
                    Index = 0,
                    Codec = audioCodec,
                    Channels = 2,
                    SampleRate = 48000,
                    Bitrate = 192_000
                }
            },
            Subtitles = new List<SubtitleStream>(),
            Duration = TimeSpan.FromMinutes(120),
            FileSize = 1_000_000_000,
            Container = container,
            OverallBitrate = 8_000_000
        };
    }

    private ClientCapabilities CreateClientCapabilities(
        string[] videoCodecs,
        string[] audioCodecs,
        string[] containers)
    {
        return new ClientCapabilities
        {
            SupportedVideoCodecs = videoCodecs,
            SupportedAudioCodecs = audioCodecs,
            SupportedContainers = containers,
            MaxResolution = VideoResolution.HD1080p,
            MaxBitrate = 0,
            SupportsHDR = false
        };
    }
}
