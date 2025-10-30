# 4K Transcoding and MP4 Direct Play Fix

## Issues Fixed

### 1. NVDEC Decode Surface Overflow (4K HEVC)
**Error**: `Using more than 32 (36) decode surfaces might cause nvdec to fail`

**Root Cause**: FFmpeg was using too many threads for HEVC decoding, exceeding NVIDIA's 32 decode surface limit.

**Fix**: Limited decoder threads to 8 by adding `-threads 8` input option when using hardware acceleration.

```typescript
// Before
command = command.inputOptions([
  '-hwaccel', 'cuda',
  '-hwaccel_output_format', 'cuda',
  '-extra_hw_frames', '8'
]);

// After
command = command.inputOptions([
  '-hwaccel', 'cuda',
  '-hwaccel_output_format', 'cuda',
  '-extra_hw_frames', '8',
  '-threads', '8'  // Limit decode surfaces to prevent NVDEC overflow
]);
```

### 2. h264_nvenc Invalid Level Error (4K Content)
**Error**: `InitializeEncoder failed: invalid param (8): Invalid Level`

**Root Cause**: 4K content was being encoded with H.264 level 4.1 (max 1080p), but 4K requires level 5.1.

**Fix**: Dynamically detect video resolution and set appropriate H.264 level:
- 4K (2160p): Level 5.1
- 1440p: Level 5.0
- 1080p and below: Level 4.1

```typescript
// Probe media to determine appropriate encoding level
const mediaInfo = await probeMedia(filePath).catch(() => null);
let level = '4.1';  // Default for 1080p
if (mediaInfo && mediaInfo.height) {
  if (mediaInfo.height >= 2160) {
    level = '5.1';  // 4K needs level 5.1
  } else if (mediaInfo.height >= 1440) {
    level = '5.0';  // 1440p needs level 5.0
  }
}
```

### 3. MP4 Files Not Using Direct Play
**Issue**: MP4 files with H.264 video and AAC audio were being transcoded unnecessarily.

**Root Cause**: The codec compatibility check didn't recognize 'avc1' as an H.264 variant, and didn't prioritize MP4 container format.

**Fix**: 
1. Added 'avc1' to compatible video codecs list
2. Enhanced `needsTranscoding()` to explicitly check for MP4 container with compatible codecs

```typescript
// Added avc1 support
const compatibleCodecs = ['h264', 'vp8', 'vp9', 'av1', 'avc1'];

// MP4 container check
const isMp4Container = info.container?.includes('mp4') || info.container?.includes('mov');
if (isMp4Container && audioCompatible && videoCompatible) {
  return {
    needsTranscode: false,
    transcodeAudio: false,
    transcodeVideo: false,
    reason: 'MP4 with compatible codecs - direct play'
  };
}
```

## Files Modified

1. `backend/src/services/media-converter.service.ts`
   - Made `createHlsSession()` and `createTranscodeStream()` async
   - Added thread limiting for NVDEC
   - Added dynamic H.264 level detection based on resolution
   - Fixed MediaInfo import

2. `backend/src/utils/ffmpeg.ts`
   - Added 'avc1' to compatible video codecs
   - Enhanced `needsTranscoding()` with MP4 container check

3. `backend/src/routes/streaming.routes.ts`
   - Updated calls to async `createHlsSession()` method

### 4. Audio Downmix Issue (5.1 to Stereo)
**Issue**: Audio sounded too deep/low when transcoding 5.1 surround to stereo.

**Root Cause**: Simple channel downmixing without proper center channel and surround mixing caused imbalanced audio.

**Fix**: Added proper audio filter for 5.1 to stereo downmix that includes center channel and surrounds:

```typescript
.audioFilters('pan=stereo|FL=FC+0.30*FL+0.30*BL|FR=FC+0.30*FR+0.30*BR')
```

This formula:
- FL (Front Left) = Center + 30% Front Left + 30% Back Left
- FR (Front Right) = Center + 30% Front Right + 30% Back Right

## Testing

Test with:
- 4K HEVC content (should transcode without NVDEC errors)
- MP4 files with H.264/AAC (should direct play)
- 1080p MKV files (should transcode if needed)
- 5.1 surround audio (should downmix properly to stereo)

## Performance Impact

- **4K transcoding**: Now works reliably with NVIDIA GPUs
- **MP4 direct play**: Eliminates unnecessary transcoding, saving CPU/GPU resources
- **Thread limiting**: Slight performance trade-off for stability (8 threads vs 16)
- **Audio downmix**: Proper center channel mixing for better dialogue clarity
