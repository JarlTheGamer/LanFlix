using Lanflix.Domain.ValueObjects;

namespace Lanflix.Infrastructure.Services.Playback.Planning;

/// <summary>
/// Produces one deterministic playback decision from probed media and a client
/// capability profile. Direct play always wins; conversion is introduced only
/// for the incompatible part of the source.
/// </summary>
internal sealed class PlaybackPlanner
{
    public PlaybackPlan Plan(
        MediaInfo media,
        string clientType,
        HardwareAcceleration hardware,
        TranscodingSettings settings,
        bool requireSeekableContainerRemux = false)
    {
        var profile = Profiles.For(clientType);
        var container = Normalize(media.Container);
        var videoCodec = Normalize(media.Video.Codec) switch
        {
            "hevc" when media.Video.BitDepth > 8 => "hevc10",
            "h264" when media.Video.BitDepth > 8 => "h26410",
            var codec => codec
        };
        var audio = SelectAudio(media.Audio, profile.PreferredAudioLanguage);
        var audioCodec = Normalize(audio?.Codec ?? "none");
        var bitrate = media.OverallBitrate ?? media.Video.Bitrate;

        var containerSupported = profile.Containers.Contains(container);
        var hdrFormat = NormalizeHdr(media.Video.HdrFormat);
        var hdrSupported = !media.Video.IsHDR || profile.SupportsHdr &&
            (hdrFormat == "unknown" || profile.HdrFormats.Contains(hdrFormat));
        var videoSupported = profile.VideoCodecs.Contains(videoCodec) &&
            media.Video.Width <= profile.MaxWidth && media.Video.Height <= profile.MaxHeight &&
            (profile.MaxBitrate <= 0 || bitrate <= profile.MaxBitrate) &&
            hdrSupported;
        var audioSupported = audio is null ||
            (profile.AudioCodecs.Contains(audioCodec) && audio.Channels <= profile.MaxAudioChannels);

        if (!profile.ForceTranscode && requireSeekableContainerRemux &&
            containerSupported && videoSupported && audioSupported)
            return Build(PlannedPlaybackMethod.Remux,
                "The Matroska seek index is not directly readable by Android Media3", media, profile, hardware, settings);

        if (!profile.ForceTranscode && containerSupported && videoSupported && audioSupported)
            return Build(PlannedPlaybackMethod.DirectPlay, "Container, video, audio and HDR are supported by the client", media, profile, hardware, settings);

        var hlsCanCopyVideo = videoCodec == "h264";
        var hlsCanCopyAudio = audioCodec is "aac" or "ac3" or "eac3" or "mp3" or "none";

        if (!profile.ForceTranscode && videoSupported && audioSupported && hlsCanCopyVideo && hlsCanCopyAudio)
            return Build(PlannedPlaybackMethod.Remux, $"Container '{container}' is not supported by the client", media, profile, hardware, settings);

        if (!profile.ForceTranscode && videoSupported && hlsCanCopyVideo)
            return Build(PlannedPlaybackMethod.DirectStream, $"Audio '{audioCodec}' or its channel layout is not supported", media, profile, hardware, settings);

        var reason = profile.ForceTranscode
            ? $"The '{profile.Id}' quality profile requires conversion"
            : media.Video.IsHDR && !profile.SupportsHdr
                ? "HDR source requires SDR tone mapping for this client"
                : $"Video '{videoCodec}' exceeds the client codec, resolution or bitrate limits";
        return Build(PlannedPlaybackMethod.Transcode, reason, media, profile, hardware, settings);
    }

    private static PlaybackPlan Build(
        PlannedPlaybackMethod method,
        string reason,
        MediaInfo media,
        PlaybackCapabilityProfile profile,
        HardwareAcceleration hardware,
        TranscodingSettings settings)
    {
        var (width, height) = Fit(media.Video.Width, media.Video.Height, profile.MaxWidth, profile.MaxHeight);
        var useHardware = method == PlannedPlaybackMethod.Transcode && !media.Video.IsHDR &&
            settings.EnableHardwareAcceleration && hardware.IsAvailable && SupportsH264(hardware);
        var hw = useHardware ? hardware.PreferredMethod : HwAccelMethod.None;
        var videoCodec = hw switch
        {
            HwAccelMethod.Nvenc => "h264_nvenc",
            HwAccelMethod.QuickSync => "h264_qsv",
            HwAccelMethod.Amf => "h264_amf",
            HwAccelMethod.Vaapi => "h264_vaapi",
            HwAccelMethod.VideoToolbox => "h264_videotoolbox",
            _ => "libx264"
        };
        var videoBitrate = Math.Min(profile.MaxBitrate > 0 ? profile.MaxBitrate : 12_000_000,
            width * height <= 1280 * 720 ? 4_000_000 : 8_000_000);
        // The managed transcode target is SDR H.264. If an HDR source needs
        // video conversion for any reason (resolution, bitrate, or codec), its
        // transfer function and color primaries must be converted as well.
        // Preserving PQ metadata on an 8-bit H.264 output causes black frames
        // or MediaCodec rejection on Android.
        var toneMap = method == PlannedPlaybackMethod.Transcode && media.Video.IsHDR;
        return new PlaybackPlan(method, reason, media, videoCodec, "aac", width, height,
            videoBitrate, 192_000, SelectAudio(media.Audio, profile.PreferredAudioLanguage)?.Index,
            toneMap, hw);
    }

    private static AudioStream? SelectAudio(IReadOnlyList<AudioStream> streams, string? preferredLanguage)
    {
        var preferred = NormalizeLanguage(preferredLanguage);
        return streams.FirstOrDefault(stream => preferred is not null && NormalizeLanguage(stream.Language) == preferred)
            ?? streams.FirstOrDefault(stream => stream.IsDefault)
            ?? streams.FirstOrDefault();
    }

    private static string? NormalizeLanguage(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" => null,
        "en" or "eng" or "english" => "eng",
        "nl" or "nld" or "dut" or "dutch" => "nld",
        "de" or "deu" or "ger" or "german" => "deu",
        "fr" or "fra" or "fre" or "french" => "fra",
        "es" or "spa" or "spanish" => "spa",
        var language => language
    };

    private static (int Width, int Height) Fit(int width, int height, int maxWidth, int maxHeight)
    {
        if (width <= maxWidth && height <= maxHeight) return (Even(width), Even(height));
        var scale = Math.Min((double)maxWidth / width, (double)maxHeight / height);
        return (Even((int)(width * scale)), Even((int)(height * scale)));
    }

    private static int Even(int value) => Math.Max(2, value - value % 2);
    private static bool SupportsH264(HardwareAcceleration hardware) => hardware.PreferredMethod switch
    {
        HwAccelMethod.Nvenc => hardware.Nvenc.SupportsH264,
        HwAccelMethod.QuickSync => hardware.QuickSync.SupportsH264,
        HwAccelMethod.Amf => hardware.Amf.SupportsH264,
        HwAccelMethod.Vaapi => hardware.Vaapi.SupportsH264,
        HwAccelMethod.VideoToolbox => hardware.VideoToolbox.SupportsH264,
        HwAccelMethod.Rockchip => hardware.Rockchip.SupportsH264,
        _ => false
    };
    private static string Normalize(string value) => value.Trim().TrimStart('.').ToLowerInvariant() switch
    {
        "matroska,webm" => "mkv",
        "h265" => "hevc",
        "avc" => "h264",
        _ => value.Trim().TrimStart('.').ToLowerInvariant()
    };

    private static string NormalizeHdr(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" => "unknown",
        var text when text.Contains("dolby") => "dv",
        var text when text.Contains("hdr10+") => "hdr10plus",
        var text when text.Contains("hdr10") => "hdr10",
        var text when text.Contains("hlg") => "hlg",
        _ => "unknown"
    };

    private static class Profiles
    {
        private static readonly HashSet<string> AndroidContainers = new(StringComparer.OrdinalIgnoreCase)
            { "mp4", "m4v", "mov", "mkv", "webm", "ts", "mpegts" };
        private static readonly HashSet<string> AndroidVideo = new(StringComparer.OrdinalIgnoreCase)
            { "h264", "hevc", "vp8", "vp9", "av1" };
        private static readonly HashSet<string> AndroidAudio = new(StringComparer.OrdinalIgnoreCase)
            { "aac", "mp3", "flac", "opus", "vorbis", "ac3", "eac3" };

        public static PlaybackCapabilityProfile For(string clientType)
        {
            if (clientType.Equals("mobile-low", StringComparison.OrdinalIgnoreCase))
                return new("mobile-low", AndroidContainers, AndroidVideo, AndroidAudio,
                    1280, 720, 2_000_000, 2, null, false, new HashSet<string>(), true);
            if (!clientType.StartsWith("android-v1|", StringComparison.OrdinalIgnoreCase))
                return new("mobile-high", AndroidContainers, AndroidVideo, AndroidAudio,
                    3840, 2160, 50_000_000, 8, null, false, new HashSet<string>(), false);

            var values = clientType.Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Select(part => part.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);
            var video = Csv(values.GetValueOrDefault("v"));
            var audio = Csv(values.GetValueOrDefault("a"));
            var containers = Csv(values.GetValueOrDefault("c"));
            var resolution = values.GetValueOrDefault("r")?.Split('x', 2);
            var maxWidth = resolution?.Length == 2 && int.TryParse(resolution[0], out var width) ? width : 1920;
            var maxHeight = resolution?.Length == 2 && int.TryParse(resolution[1], out var height) ? height : 1080;
            var hdrFormats = Csv(values.GetValueOrDefault("hdr"));
            hdrFormats.Remove("none");
            var preferredAudioLanguage = values.GetValueOrDefault("al");
            return new("android-v1", containers.Count > 0 ? containers : AndroidContainers,
                video.Count > 0 ? video : new HashSet<string>(["h264"], StringComparer.OrdinalIgnoreCase),
                audio.Count > 0 ? audio : new HashSet<string>(["aac"], StringComparer.OrdinalIgnoreCase),
                Math.Clamp(maxWidth, 640, 7680), Math.Clamp(maxHeight, 360, 4320),
                50_000_000, 8, preferredAudioLanguage, hdrFormats.Count > 0, hdrFormats, false);
        }

        private static HashSet<string> Csv(string? value) => new(
            value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    }
}
