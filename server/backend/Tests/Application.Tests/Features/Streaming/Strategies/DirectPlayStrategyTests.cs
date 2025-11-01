using FluentAssertions;
using Lanflix.Application.Features.Streaming.Strategies;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Lanflix.Application.Tests.Features.Streaming.Strategies;

public class DirectPlayStrategyTests
{
    private readonly DirectPlayStrategy _strategy;
    private readonly ILogger<DirectPlayStrategy> _logger;

    public DirectPlayStrategyTests()
    {
        _logger = Substitute.For<ILogger<DirectPlayStrategy>>();
        _strategy = new DirectPlayStrategy(_logger);
    }

    [Fact]
    public void Mode_ShouldReturnDirectPlay()
    {
        // Act
        var mode = _strategy.Mode;

        // Assert
        mode.Should().Be(StreamingMode.DirectPlay);
    }

    [Fact]
    public void Priority_ShouldReturnOne()
    {
        // Act
        var priority = _strategy.Priority;

        // Assert
        priority.Should().Be(1);
    }

    [Fact]
    public void CanHandle_WhenAllCodecsAndContainerSupported_ReturnsTrue()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "h264",
            audioCodec: "aac",
            container: "mp4",
            width: 1920,
            height: 1080,
            bitrate: 8_000_000);

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264", "hevc" },
            audioCodecs: new[] { "aac", "mp3" },
            containers: new[] { "mp4", "mkv" },
            maxResolution: VideoResolution.HD1080p,
            maxBitrate: 10_000_000);

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_WhenVideoCodecNotSupported_ReturnsFalse()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "hevc",
            audioCodec: "aac",
            container: "mp4");

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264", "vp9" },
            audioCodecs: new[] { "aac", "mp3" },
            containers: new[] { "mp4" });

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WhenAudioCodecNotSupported_ReturnsFalse()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "h264",
            audioCodec: "opus",
            container: "mp4");

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264", "hevc" },
            audioCodecs: new[] { "aac", "mp3" },
            containers: new[] { "mp4" });

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WhenContainerNotSupported_ReturnsFalse()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "h264",
            audioCodec: "aac",
            container: "mkv");

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264", "hevc" },
            audioCodecs: new[] { "aac", "mp3" },
            containers: new[] { "mp4", "webm" });

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WhenResolutionExceedsClientMax_ReturnsFalse()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "h264",
            audioCodec: "aac",
            container: "mp4",
            width: 3840,
            height: 2160);

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264" },
            audioCodecs: new[] { "aac" },
            containers: new[] { "mp4" },
            maxResolution: VideoResolution.HD1080p);

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WhenBitrateExceedsClientMax_ReturnsFalse()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "h264",
            audioCodec: "aac",
            container: "mp4",
            bitrate: 15_000_000);

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264" },
            audioCodecs: new[] { "aac" },
            containers: new[] { "mp4" },
            maxBitrate: 10_000_000);

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WhenHDRContentButClientDoesNotSupportHDR_ReturnsFalse()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "hevc",
            audioCodec: "aac",
            container: "mp4",
            isHDR: true);

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "hevc" },
            audioCodecs: new[] { "aac" },
            containers: new[] { "mp4" },
            supportsHDR: false);

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WhenHDRContentAndClientSupportsHDR_ReturnsTrue()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "hevc",
            audioCodec: "aac",
            container: "mp4",
            isHDR: true);

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "hevc" },
            audioCodecs: new[] { "aac" },
            containers: new[] { "mp4" },
            supportsHDR: true);

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_WhenNonHDRContent_IgnoresHDRSupport()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "h264",
            audioCodec: "aac",
            container: "mp4",
            isHDR: false);

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264" },
            audioCodecs: new[] { "aac" },
            containers: new[] { "mp4" },
            supportsHDR: false);

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeTrue();
    }

    // Helper methods
    private MediaInfo CreateMediaInfo(
        string videoCodec = "h264",
        string audioCodec = "aac",
        string container = "mp4",
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
        string[] containers,
        VideoResolution maxResolution = VideoResolution.HD1080p,
        int maxBitrate = 0,
        bool supportsHDR = false)
    {
        return new ClientCapabilities
        {
            SupportedVideoCodecs = videoCodecs,
            SupportedAudioCodecs = audioCodecs,
            SupportedContainers = containers,
            MaxResolution = maxResolution,
            MaxBitrate = maxBitrate,
            SupportsHDR = supportsHDR
        };
    }
}
