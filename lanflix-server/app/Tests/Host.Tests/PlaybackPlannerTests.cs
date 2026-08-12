using Lanflix.Domain.ValueObjects;
using Lanflix.Infrastructure.Services.Playback.Ffmpeg;
using Lanflix.Infrastructure.Services.Playback.Planning;
using Xunit;

namespace Lanflix.Host.Tests;

public sealed class PlaybackPlannerTests
{
    private readonly PlaybackPlanner _planner = new();
    private static readonly HardwareAcceleration NoHardware = new();
    private static readonly TranscodingSettings Settings = new() { EnableHardwareAcceleration = false, EnableToneMapping = true };

    [Fact]
    public void Android_direct_plays_h264_eac3_mkv()
    {
        var plan = _planner.Plan(Media("mkv", "h264", "eac3"),
            "android-v1|v=h264,hevc|a=aac,eac3|c=mp4,mkv,ts|r=3840x2160|hdr=none", NoHardware, Settings);

        Assert.Equal(PlannedPlaybackMethod.DirectPlay, plan.Method);
        Assert.False(plan.TranscodesVideo);
        Assert.False(plan.TranscodesAudio);
    }

    [Fact]
    public void Android_remuxes_otherwise_compatible_mkv_when_seek_index_requires_it()
    {
        var media = Media("mkv", "hevc", "aac") with
        {
            Video = Media("mkv", "hevc", "aac").Video with { BitDepth = 10 }
        };
        var plan = _planner.Plan(media,
            "android-v1|v=h264,hevc,hevc10|a=aac|c=mp4,mkv,ts|r=3840x2160|hdr=none",
            NoHardware, Settings, requireSeekableContainerRemux: true);

        Assert.Equal(PlannedPlaybackMethod.Remux, plan.Method);
        Assert.False(plan.TranscodesVideo);
        Assert.False(plan.TranscodesAudio);
        Assert.Contains("seek index", plan.Reason, StringComparison.OrdinalIgnoreCase);
        var command = new FfmpegCommandBuilder().BuildSegmentBatch(
            new FfmpegSegmentBatch("C:\\media\\episode.mkv", "C:\\temp\\session", 0, 8, 6, plan));
        Assert.Contains("-map 0:v:0 -an", command);
        Assert.Contains("-c:v copy", command);
    }

    [Fact]
    public void Unsupported_audio_keeps_video_eligible_for_direct_stream()
    {
        var plan = _planner.Plan(Media("mkv", "h264", "dts"),
            "android-v1|v=h264|a=aac|c=mp4,mkv,ts|r=1920x1080|hdr=none", NoHardware, Settings);

        Assert.Equal(PlannedPlaybackMethod.DirectStream, plan.Method);
        Assert.False(plan.TranscodesVideo);
        Assert.True(plan.TranscodesAudio);
    }

    [Fact]
    public void Hdr_source_transcodes_and_tone_maps_for_sdr_phone()
    {
        var plan = _planner.Plan(Media("mp4", "hevc", "aac", hdr: true),
            "android-v1|v=h264,hevc|a=aac|c=mp4,mkv,ts|r=1920x1080|hdr=none", NoHardware, Settings);

        Assert.Equal(PlannedPlaybackMethod.Transcode, plan.Method);
        Assert.True(plan.ToneMap);
        Assert.Equal("libx264", plan.OutputVideoCodec);
        Assert.Equal(1920, plan.Width);
        Assert.Equal(1080, plan.Height);
    }

    [Fact]
    public void Hdr_source_is_tone_mapped_when_resolution_requires_h264_even_on_hdr_phone()
    {
        var plan = _planner.Plan(Media("mp4", "hevc", "eac3", hdr: true),
            "android-v1|v=h264,hevc,hevc10|a=aac,eac3|c=mp4,mkv,ts|r=1920x1080|hdr=hdr10",
            NoHardware, Settings);

        Assert.Equal(PlannedPlaybackMethod.Transcode, plan.Method);
        Assert.True(plan.ToneMap);
        Assert.Equal("libx264", plan.OutputVideoCodec);
    }

    [Fact]
    public void Preferred_audio_language_is_mapped_by_absolute_stream_index()
    {
        var media = Media("mp4", "h264", "aac") with
        {
            Audio =
            [
                new AudioStream { Index = 1, Codec = "aac", Channels = 2, Language = "jpn", IsDefault = true },
                new AudioStream { Index = 2, Codec = "eac3", Channels = 6, Language = "eng" }
            ]
        };
        var plan = _planner.Plan(media,
            "android-v1|v=h264|a=aac,eac3|c=mp4,ts|r=1920x1080|hdr=none|al=en", NoHardware, Settings);

        Assert.Equal(2, plan.AudioStreamIndex);
        var command = new FfmpegCommandBuilder().BuildSegmentBatch(
            new FfmpegSegmentBatch("C:\\media\\movie.mkv", "C:\\temp\\session", 0, 8, 6, plan,
                HlsSegmentKind.Audio, plan.AudioStreamIndex));
        Assert.Contains("-map 0:2?", command);
    }

    [Fact]
    public void Ten_bit_hevc_requires_main10_decoder_capability()
    {
        var media = Media("mkv", "hevc", "aac") with
        {
            Video = Media("mkv", "hevc", "aac").Video with { BitDepth = 10 }
        };
        var plan = _planner.Plan(media,
            "android-v1|v=h264,hevc|a=aac|c=mp4,mkv,ts|r=3840x2160|hdr=none", NoHardware, Settings);

        Assert.Equal(PlannedPlaybackMethod.Transcode, plan.Method);
    }

    [Fact]
    public void Ffmpeg_transcode_segments_are_h264_keyframe_aligned_vod_chunks()
    {
        var plan = _planner.Plan(Media("mp4", "hevc", "aac", hdr: true),
            "android-v1|v=h264|a=aac|c=mp4,ts|r=1920x1080|hdr=none", NoHardware, Settings);
        var command = new FfmpegCommandBuilder().BuildSegmentBatch(
            new FfmpegSegmentBatch("C:\\media\\movie.mkv", "C:\\temp\\session", 10, 8, 6, plan));

        Assert.Contains("-copyts", command);
        Assert.Contains("-ss 60", command);
        Assert.Contains("-to 108", command);
        Assert.Contains("-avoid_negative_ts disabled", command);
        Assert.Contains("-mpegts_copyts 1", command);
        Assert.DoesNotContain("-output_ts_offset", command);
        Assert.Contains("-c:v libx264", command);
        Assert.Contains("-force_key_frames", command);
        Assert.Contains("-hls_flags independent_segments+temp_file", command);
        Assert.Contains("-start_number 10", command);
    }

    private static MediaInfo Media(string container, string video, string audio, bool hdr = false) => new()
    {
        Container = container,
        Duration = TimeSpan.FromMinutes(24),
        FileSize = 1_000_000,
        OverallBitrate = 8_000_000,
        Video = new VideoStream
        {
            Codec = video,
            Width = hdr ? 3840 : 1920,
            Height = hdr ? 2160 : 1080,
            Bitrate = 7_000_000,
            FrameRate = 24,
            PixelFormat = hdr ? "yuv420p10le" : "yuv420p",
            IsHDR = hdr
        },
        Audio = [new AudioStream { Codec = audio, Channels = audio == "eac3" ? 6 : 2, Bitrate = 256_000, IsDefault = true }]
    };
}
