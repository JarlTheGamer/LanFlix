using FluentAssertions;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Features.Streaming.Strategies;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Lanflix.Application.Tests.Features.Streaming.Strategies;

public class FullTranscodeStrategyTests
{
    private readonly FullTranscodeStrategy _strategy;
    private readonly ITranscodingPipeline _transcodingPipeline;
    private readonly IHardwareAccelerationDetector _hwAccelDetector;
    private readonly ILogger<FullTranscodeStrategy> _logger;

    public FullTranscodeStrategyTests()
    {
        _transcodingPipeline = Substitute.For<ITranscodingPipeline>();
        _hwAccelDetector = Substitute.For<IHardwareAccelerationDetector>();
        _logger = Substitute.For<ILogger<FullTranscodeStrategy>>();
        _strategy = new FullTranscodeStrategy(_transcodingPipeline, _hwAccelDetector, _logger);
    }

    [Fact]
    public void Mode_ShouldReturnFullTranscode()
    {
        // Act
        var mode = _strategy.Mode;

        // Assert
        mode.Should().Be(StreamingMode.FullTranscode);
    }

    [Fact]
    public void Priority_ShouldReturnFour()
    {
        // Act
        var priority = _strategy.Priority;

        // Assert
        priority.Should().Be(4);
    }

    [Fact]
    public void CanHandle_AlwaysReturnsTrue_AsFallbackStrategy()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "unknown_codec",
            audioCodec: "unknown_audio");

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264" },
            audioCodecs: new[] { "aac" });

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_WithIncompatibleEverything_StillReturnsTrue()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "av1",
            audioCodec: "flac",
            container: "mkv",
            width: 7680,
            height: 4320,
            bitrate: 100_000_000,
            isHDR: true);

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264" },
            audioCodecs: new[] { "aac" },
            containers: new[] { "mp4" },
            maxResolution: VideoResolution.HD720p,
            maxBitrate: 2_000_000,
            supportsHDR: false);

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_WithCompatibleCodecs_StillReturnsTrue()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "h264",
            audioCodec: "aac");

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264" },
            audioCodecs: new[] { "aac" });

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeTrue();
    }

    // Helper methods
    private MediaInfo CreateMediaInfo(
        string videoCodec = "av1",
        string audioCodec = "flac",
        string container = "mkv",
        int width = 1920,
        int height = 1080,
        long bitrate = 8_000_000,
        bool isHDR = false)
    {
        return new MediaInfo
        {
            Video = new VideoStream
            {
                Codec = videoCodec,
                Width = width,
                Height = height,
                Bitrate = bitrate,
                FrameRate = 24.0,
                PixelFormat = "yuv420p",
                IsHDR = isHDR
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
            OverallBitrate = bitrate
        };
    }

    private ClientCapabilities CreateClientCapabilities(
        string[] videoCodecs,
        string[] audioCodecs,
        string[]? containers = null,
        VideoResolution maxResolution = VideoResolution.HD1080p,
        int maxBitrate = 0,
        bool supportsHDR = false)
    {
        return new ClientCapabilities
        {
            SupportedVideoCodecs = videoCodecs,
            SupportedAudioCodecs = audioCodecs,
            SupportedContainers = containers ?? new[] { "mp4", "mkv" },
            MaxResolution = maxResolution,
            MaxBitrate = maxBitrate,
            SupportsHDR = supportsHDR
        };
    }
}
