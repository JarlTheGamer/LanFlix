using FluentAssertions;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Features.Streaming.Strategies;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Lanflix.Application.Tests.Features.Streaming.Strategies;

public class TranscodeVideoStrategyTests
{
    private readonly TranscodeVideoStrategy _strategy;
    private readonly ITranscodingPipeline _transcodingPipeline;
    private readonly IHardwareAccelerationDetector _hwAccelDetector;
    private readonly ILogger<TranscodeVideoStrategy> _logger;

    public TranscodeVideoStrategyTests()
    {
        _transcodingPipeline = Substitute.For<ITranscodingPipeline>();
        _hwAccelDetector = Substitute.For<IHardwareAccelerationDetector>();
        _logger = Substitute.For<ILogger<TranscodeVideoStrategy>>();
        _strategy = new TranscodeVideoStrategy(_transcodingPipeline, _hwAccelDetector, _logger);
    }

    [Fact]
    public void Mode_ShouldReturnTranscodeVideo()
    {
        // Act
        var mode = _strategy.Mode;

        // Assert
        mode.Should().Be(StreamingMode.TranscodeVideo);
    }

    [Fact]
    public void Priority_ShouldReturnThree()
    {
        // Act
        var priority = _strategy.Priority;

        // Assert
        priority.Should().Be(3);
    }

    [Fact]
    public void CanHandle_WhenVideoCodecNotSupportedButAudioIs_ReturnsTrue()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "av1",
            audioCodec: "aac");

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264", "hevc" },
            audioCodecs: new[] { "aac", "mp3" });

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_WhenVideoCodecAlreadySupported_ReturnsFalse()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "h264",
            audioCodec: "aac");

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264", "hevc" },
            audioCodecs: new[] { "aac" });

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WhenVideoCodecNotSupportedAndAudioAlsoNot_ReturnsFalse()
    {
        // Arrange
        var media = CreateMediaInfo(
            videoCodec: "av1",
            audioCodec: "flac");

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264", "hevc" },
            audioCodecs: new[] { "aac", "mp3" });

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WhenMultipleAudioStreamsAndOneIsCompatible_ReturnsTrue()
    {
        // Arrange
        var media = new MediaInfo
        {
            Video = new VideoStream
            {
                Codec = "av1",
                Width = 1920,
                Height = 1080,
                Bitrate = 8_000_000,
                FrameRate = 24.0,
                PixelFormat = "yuv420p",
                IsHDR = false
            },
            Audio = new List<AudioStream>
            {
                new AudioStream { Index = 0, Codec = "flac", Channels = 2, SampleRate = 48000, Bitrate = 1_000_000 },
                new AudioStream { Index = 1, Codec = "aac", Channels = 2, SampleRate = 48000, Bitrate = 192_000 }
            },
            Subtitles = new List<SubtitleStream>(),
            Duration = TimeSpan.FromMinutes(120),
            FileSize = 1_000_000_000,
            Container = "mkv",
            OverallBitrate = 8_000_000
        };

        var client = CreateClientCapabilities(
            videoCodecs: new[] { "h264", "hevc" },
            audioCodecs: new[] { "aac", "mp3" });

        // Act
        var result = _strategy.CanHandle(media, client);

        // Assert
        result.Should().BeTrue();
    }

    // Helper methods
    private MediaInfo CreateMediaInfo(
        string videoCodec = "av1",
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
