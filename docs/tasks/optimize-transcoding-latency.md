# Optimize Transcoding Latency (Match Jellyfin Performance)

## Problem
Transcoding feels slower than Jellyfin despite having powerful GPU. The issue isn't quality or GPU power - it's about streaming latency and buffering strategy.

## Why Jellyfin Feels Faster

### 1. Low-Latency FFmpeg Configuration
Jellyfin uses specific flags to minimize buffering and start playback immediately:
- `-fflags +nobuffer` - Reduces input buffering
- `-flags +low_delay` - Minimizes encoding delay  
- `-probesize 32` and `-analyzeduration 0` - Faster stream start
- `-tune zerolatency` (CPU) or `-zerolatency 1` (NVENC) - Real-time encoding mode

### 2. Smaller Fragment Sizes
- Current: Fragments at keyframes (2-10 seconds)
- Jellyfin: 1-2 second fragments for faster initial playback

### 3. Optimized Bitrate Strategy
- Current: 5M/8M/10M bitrate (more data before playback)
- Jellyfin: Lower initial bitrates with adaptive streaming

### 4. Thread Optimization
Missing `-threads 0` to use all CPU cores for muxing/demuxing

### 5. Read-Ahead Control
Missing `-readrate 1` for better memory usage and responsiveness

## Implementation Changes

### GPU Transcoding (NVENC) - Low Latency Mode

**File:** `backend/src/services/media-converter.service.ts`

**Current approach:** Uses `-tune hq` (high quality) with larger buffers
**New approach:** Use `-tune ll` (low latency) with smaller fragments

```typescript
// Replace the video transcoding section in createTranscodeStream()
if (options.transcodeVideo) {
  const preset = options.preset || 'p2';  // Changed from p4 to p2 for lower latency
  logger.info(`VIDEO: Low-latency transcoding with NVENC using preset ${preset}`);
  
  command = command
    .inputOptions([
      '-hwaccel', 'cuda',
      '-hwaccel_output_format', 'cuda',
      '-extra_hw_frames', '8',
      '-fflags', '+nobuffer+genpts',  // Low latency input
      '-probesize', '32',              // Fast probe
      '-analyzeduration', '0'          // Skip analysis
    ])
    .videoCodec('h264_nvenc')
    .addOutputOption('-preset', preset)
    .addOutputOption('-tune', 'll')        // LOW LATENCY (not 'hq')
    .addOutputOption('-zerolatency', '1')  // NVENC zero latency mode
    .addOutputOption('-delay', '0')
    .addOutputOption('-forced-idr', '1')
    .addOutputOption('-rc', 'cbr')         // CBR for consistent streaming
    .addOutputOption('-b:v', '3M')         // Lower bitrate for faster start
    .addOutputOption('-maxrate', '3M')
    .addOutputOption('-bufsize', '1M')     // Smaller buffer (was 10M)
    .addOutputOption('-g', '60')           // Keyframe every 2 sec at 30fps
    .addOutputOption('-profile:v', 'high')
    .addOutputOption('-level', '5.1')
    .addOutputOption('-pix_fmt', 'yuv420p')
    .addOutputOption('-spatial_aq', '1')
    .addOutputOption('-temporal_aq', '1')
    .addOutputOption('-bf', '3')
    .addOutputOption('-threads', '0')      // Use all threads
    .addOutputOption('-gpu', '0');
}

// Update output format options for smaller fragments
command = command
  .outputFormat('mp4')
  .addOutputOption('-movflags', 'frag_keyframe+empty_moov+default_base_moof')
  .addOutputOption('-min_frag_duration', '1000000')  // 1 second fragments
  .addOutputOption('-frag_duration', '2000000')      // 2 second max
  .addOutputOption('-map', '0:v:0')
  .addOutputOption('-map', '0:a:0?');
```

### CPU Transcoding - Low Latency Mode

```typescript
// Update createCPUTranscodeStream() video section
if (options.transcodeVideo) {
  const presetMap: { [key: string]: string } = {
    'p1': 'ultrafast',
    'p2': 'superfast',
    'p3': 'veryfast',
    'p4': 'faster',
    'p5': 'fast',
    'p6': 'medium',
    'p7': 'slow'
  };
  const nvencPreset = options.preset || 'p2';  // Changed from p4
  const cpuPreset = presetMap[nvencPreset] || 'veryfast';
  
  command = command
    .inputOptions([
      '-fflags', '+nobuffer+genpts',
      '-probesize', '32',
      '-analyzeduration', '0'
    ])
    .videoCodec('libx264')
    .addOutputOption('-preset', cpuPreset)
    .addOutputOption('-tune', 'zerolatency')  // Low latency for CPU
    .addOutputOption('-crf', '23')
    .addOutputOption('-profile:v', 'high')
    .addOutputOption('-level', '5.1')
    .addOutputOption('-pix_fmt', 'yuv420p')
    .addOutputOption('-g', '60')              // Keyframe interval
    .addOutputOption('-threads', '0');        // Use all threads
}
```

### Streaming Response Headers

**File:** `backend/src/routes/streaming.routes.ts`

Add chunked transfer encoding for transcoded streams:

```typescript
// In the transcoding section, update headers:
res.writeHead(200, {
  'Content-Type': 'video/mp4',
  'Transfer-Encoding': 'chunked',  // ADD THIS
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'Range',
  'Access-Control-Expose-Headers': 'Content-Type, X-Playback-Mode, X-Transcode-Mode, X-Direct-Play',
  'Cache-Control': 'no-cache',
  'Connection': 'keep-alive',
  'X-Content-Type-Options': 'nosniff',  // ADD THIS
  'X-Playback-Mode': transcodeMode,
  'X-Transcode-Mode': transcodeMode,
  'X-Direct-Play': 'false'
});
```

## Expected Performance Improvements

1. **Playback Start Time:** 2-5 seconds → 0.5-1 second
2. **Seek Response:** 3-4 seconds → 1 second
3. **Buffer Building:** Gradual → Immediate with small initial buffer
4. **Memory Usage:** Lower due to smaller buffers
5. **Perceived Smoothness:** Matches Jellyfin

## Trade-offs

- **Quality:** Slightly lower (tune=ll vs tune=hq) - imperceptible for streaming
- **Bitrate:** 3M vs 5M initial - still excellent quality
- **CPU Usage:** Slightly higher due to smaller fragments
- **Encoding Efficiency:** Lower compression ratio (acceptable for real-time)

## Testing

1. Test with various file formats (MKV, MP4, AVI)
2. Test seeking behavior (forward/backward)
3. Monitor GPU utilization (should be similar)
4. Compare perceived latency with Jellyfin
5. Check quality at 3M bitrate vs 5M

## Future Enhancements

1. **Segment Caching:** Cache transcoded segments to avoid re-transcoding on seek
2. **Adaptive Bitrate:** Start at 2M, increase to 5M after initial buffer
3. **HLS/DASH:** Implement proper adaptive streaming protocols
4. **Prefetching:** Transcode next segments in background
5. **Smart Preset Selection:** Auto-adjust based on content complexity

## References

- FFmpeg NVENC Low Latency: https://docs.nvidia.com/video-technologies/video-codec-sdk/nvenc-video-encoder-api-prog-guide/
- Jellyfin Transcoding: https://jellyfin.org/docs/general/server/transcoding/
- MP4 Fragmentation: https://ffmpeg.org/ffmpeg-formats.html#mov_002c-mp4_002c-ismv
