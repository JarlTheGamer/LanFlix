using FluentAssertions;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Features.Streaming.Strategies;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Lanflix.Application.Tests.Features.Streaming.Strategies;

public class DirectStreamStrategyTests
{
    private readonly DirectStreamStrategy _strategy;
    private readonly ITranscodingPipeline _transcodingPipeline;
    private readonly ILogger<DirectStreamStrategy> _logger;

    public DirectStreamStrategyTests()
    {
        _transcodingPipeline = Substitute.For<ITranscodingPipeline>();
        _logger = Substitute.For<ILogger<DirectStreamStrategy>>();
        _strategy = new DirectStreamStrategy(_transcodingPipeline, _logger);
    }

    [Fact]
    public void Mode_ShouldReturnDirectStream()
    {
        // Act
        var mode = _strategy.Mode;

        // Assert
        mode.Should().Be(StreamingMode.DirectStream);
    }

    [Fact]
    public void Priority_ShouldReturnTwo()
    {
        // Act
        var priority = _strategy.Priority;

        // Assert
        priority.Should().Be(2);
    }

    [Fact]
    public void CanHandle_WhenCodecsCompatibleButContainerNot_ReturnsTrue()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "h264",
            audioCodec: "aac",
            container: "mkv");

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264", "hevc" },
            audioCodecs: new[] { "aac", "mp3" },
            containers: new[] { "mp4", "webm" }); // mkv not supported

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_WhenContainerAlreadySupported_ReturnsFalse()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "h264",
            audioCodec: "aac",
            container: "mp4");

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264" },
            audioCodecs: new[] { "aac" },
            containers: new[] { "mp4" }); // mp4 is supported

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WhenVideoCodecNotSupported_ReturnsFalse()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "av1",
            audioCodec: "aac",
            container: "mkv");

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264", "hevc" },
            audioCodecs: new[] { "aac" },
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
            audioCodec: "flac",
            container: "mkv");

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264" },
            audioCodecs: new[] { "aac", "mp3" },
            containers: new[] { "mp4" });

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
            container: "mkv",
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
            container: "mkv",
            bitrate: 20_000_000);

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
            container: "mkv",
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

    // Helper methods
    private MediaInfo CreateMediaInfo(
        string videoCodec = "h264",
        string audioCodec = "aac",
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
