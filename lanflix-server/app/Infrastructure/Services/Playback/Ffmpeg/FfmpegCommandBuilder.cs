using System.Globalization;
using System.Text;
using Lanflix.Domain.ValueObjects;
using Lanflix.Infrastructure.Services.Playback.Planning;

namespace Lanflix.Infrastructure.Services.Playback.Ffmpeg;

internal sealed record FfmpegSegmentBatch(
    string InputPath,
    string OutputDirectory,
    int FirstSegment,
    int SegmentCount,
    double SegmentDuration,
    PlaybackPlan Plan,
    HlsSegmentKind Kind = HlsSegmentKind.Video,
    int? AudioStreamIndex = null);

internal enum HlsSegmentKind { Video, Audio }

internal sealed class FfmpegCommandBuilder
{
    public string BuildSegmentBatch(FfmpegSegmentBatch batch, bool softwareFallback = false)
    {
        var plan = batch.Plan;
        var start = batch.FirstSegment * batch.SegmentDuration;
        var duration = batch.SegmentCount * batch.SegmentDuration;
        var codec = softwareFallback ? "libx264" : plan.OutputVideoCodec;
        var hardware = softwareFallback ? HwAccelMethod.None : plan.HardwareAcceleration;
        var args = new StringBuilder("-hide_banner -loglevel warning -nostdin -y ");

        AppendHardwareInput(args, hardware);
        // Preserve the source timeline across independently generated batches.
        // Media3 seeks by the timestamps declared in the VOD manifest; resetting
        // every batch to zero makes a request for minute 20 resume at the start.
        args.Append("-copyts ");
        args.Append("-ss ").Append(Number(start)).Append(' ');
        args.Append("-i ").Append(Quote(batch.InputPath)).Append(' ');
        args.Append("-to ").Append(Number(start + duration)).Append(' ');
        if (batch.Kind == HlsSegmentKind.Audio)
        {
            args.Append("-map 0:")
                .Append(batch.AudioStreamIndex?.ToString(CultureInfo.InvariantCulture) ?? "a:0")
                .Append("? -vn -sn -dn ");
        }
        else args.Append("-map 0:v:0 -an -sn -dn ");

        if (batch.Kind == HlsSegmentKind.Audio)
        {
            if (plan.Method == PlannedPlaybackMethod.Remux) args.Append("-c:a copy ");
            else args.Append("-c:a aac -ac 2 -b:a ").Append(plan.AudioBitrate).Append(' ');
        }
        else switch (plan.Method)
        {
            case PlannedPlaybackMethod.Remux:
            case PlannedPlaybackMethod.DirectStream:
                args.Append("-c:v copy ");
                break;
            default:
                args.Append("-c:v ").Append(codec).Append(' ');
                args.Append("-b:v ").Append(plan.VideoBitrate).Append(' ')
                    .Append("-maxrate ").Append(plan.VideoBitrate).Append(' ')
                    .Append("-bufsize ").Append(plan.VideoBitrate * 2).Append(' ');
                AppendVideoFilters(args, plan, hardware);
                AppendEncoderOptions(args, codec, batch.SegmentDuration, plan.Media.Video.FrameRate);
                break;
        }

        args.Append("-max_muxing_queue_size 2048 -avoid_negative_ts disabled -mpegts_copyts 1 ");
        args.Append("-f hls -hls_segment_type mpegts -hls_time ").Append(Number(batch.SegmentDuration)).Append(' ');
        args.Append("-hls_list_size 0 -hls_flags independent_segments+temp_file ");
        args.Append("-start_number ").Append(batch.FirstSegment).Append(' ');
        args.Append("-hls_segment_filename ")
            .Append(Quote(Path.Combine(batch.OutputDirectory, SegmentPrefix(batch) + "%05d.ts"))).Append(' ');
        args.Append(Quote(Path.Combine(batch.OutputDirectory, $"{SegmentPrefix(batch)}batch-{batch.FirstSegment:D5}.m3u8")));
        return args.ToString();
    }

    private static string SegmentPrefix(FfmpegSegmentBatch batch) => batch.Kind == HlsSegmentKind.Audio
        ? $"audio-{batch.AudioStreamIndex ?? 0:D2}-segment-"
        : "video-segment-";

    private static void AppendHardwareInput(StringBuilder args, HwAccelMethod hardware)
    {
        switch (hardware)
        {
            case HwAccelMethod.Nvenc: args.Append("-hwaccel cuda -hwaccel_output_format cuda "); break;
            case HwAccelMethod.QuickSync: args.Append("-hwaccel qsv -hwaccel_output_format qsv "); break;
            case HwAccelMethod.Vaapi: args.Append("-hwaccel vaapi "); break;
        }
    }

    private static void AppendVideoFilters(StringBuilder args, PlaybackPlan plan, HwAccelMethod hardware)
    {
        var filters = new List<string>();
        if (plan.ToneMap)
        {
            // HDR is intentionally decoded and tone-mapped in software. This
            // avoids advertising successful NVENC output that contains black
            // frames because the source surfaces were never converted to SDR.
            filters.Add("zscale=t=linear:npl=100");
            filters.Add("format=gbrpf32le");
            filters.Add("zscale=p=bt709");
            filters.Add("tonemap=hable:desat=0");
            filters.Add("zscale=t=bt709:m=bt709:r=tv");
            filters.Add("format=yuv420p");
        }

        var scale = hardware switch
        {
            HwAccelMethod.Nvenc => $"scale_cuda={plan.Width}:{plan.Height}",
            HwAccelMethod.QuickSync => $"scale_qsv={plan.Width}:{plan.Height}",
            HwAccelMethod.Vaapi => $"scale_vaapi={plan.Width}:{plan.Height}",
            _ => $"scale={plan.Width}:{plan.Height}"
        };
        filters.Add(scale);
        if (filters.Count > 0) args.Append("-vf ").Append(Quote(string.Join(',', filters))).Append(' ');
        args.Append("-pix_fmt yuv420p ");
    }

    private static void AppendEncoderOptions(StringBuilder args, string codec, double segmentDuration, double frameRate)
    {
        var fps = frameRate > 0 ? frameRate : 24;
        var gop = Math.Max(24, (int)Math.Round(fps * segmentDuration));
        if (codec.Contains("nvenc", StringComparison.OrdinalIgnoreCase)) args.Append("-preset p4 -rc vbr ");
        else if (codec.Contains("qsv", StringComparison.OrdinalIgnoreCase)) args.Append("-preset medium ");
        else if (codec.Contains("amf", StringComparison.OrdinalIgnoreCase)) args.Append("-quality balanced ");
        else args.Append("-preset veryfast ");
        args.Append("-g ").Append(gop).Append(" -keyint_min ").Append(gop)
            .Append(" -sc_threshold 0 -force_key_frames ")
            .Append(Quote($"expr:gte(t,n_forced*{Number(segmentDuration)})")).Append(' ');
    }

    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string Quote(string value) => '"' + value.Replace("\"", "\\\"") + '"';
}
