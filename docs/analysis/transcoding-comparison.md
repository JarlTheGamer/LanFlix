# Transcoding System Analysis: Current vs Jellyfin-Style

## Executive Summary

Your current system already implements a **Jellyfin-style streaming transcoding approach** with HLS segmentation and hardware acceleration. However, there are some key differences and areas for improvement to make it more aligned with Jellyfin's philosophy.

## Current System Overview

### What You Have Now

#### 1. **Transcoding Modes** (3 modes)
- **Direct Play**: No transcoding, stream as-is
- **Streaming**: Real-time HLS transcoding (Jellyfin-style)
- **Offline**: Background pre-transcoding (not yet implemented)

#### 2. **Hardware Acceleration**
- ✅ NVIDIA NVENC/NVDEC support
- ✅ Full GPU pipeline (decode + encode)
- ✅ CPU fallback when GPU fails
- ❌ No Intel QSV support
- ❌ No AMD AMF/VA-API support
- ❌ No Apple VideoToolbox support

#### 3. **Transcoding Decision Logic**
```
Current Flow:
1. Client requests media
2. Server checks codec compatibility (H.264, AAC, etc.)
3. If incompatible AND user enabled transcoding → HLS session
4. If compatible OR user disabled transcoding → Direct play
```

#### 4. **HLS Implementation**
- ✅ Segmented streaming (4-second segments)
- ✅ On-the-fly transcoding
- ✅ Seeking support via startTime
- ✅ Session management with timeout
- ✅ Separate audio/video transcoding control

#### 5. **Settings Architecture**
- Per-profile transcoding preferences
- Stored in Settings table as `streamingPreferences_{profileId}`
- Includes: mode, audio/video toggles, hardware accel, preset

---

## Jellyfin's Approach

### Key Principles

#### 1. **Client-Driven Transcoding**
```
Jellyfin Flow:
1. Client sends capabilities (codecs, resolution, bitrate, constraints)
2. Server picks best quality within capabilities
3. Server decides transcoding parameters automatically
4. No manual resolution override
```

**Your System**: Server-driven based on codec compatibility only

#### 2. **Four Playback Types**
1. **Direct Play**: No modification (lowest load)
2. **Remux**: Container change only (audio + video untouched)
3. **Direct Stream**: Transcode audio, copy video
4. **Transcode**: Transcode video (highest load)

**Your System**: Only has Direct Play and Transcode (missing Remux and Direct Stream granularity)

#### 3. **Hardware Acceleration Priority**
```
Jellyfin Priority:
1. Intel QSV (most common, iGPU)
2. NVIDIA NVENC (dedicated GPU)
3. AMD AMF/VA-API (AMD GPUs)
4. Apple VideoToolbox (macOS/iOS)
5. Rockchip RKMPP (ARM SBCs)
6. CPU fallback (slowest)
```

**Your System**: Only NVIDIA NVENC + CPU fallback

#### 4. **Adaptive Bitrate & Resolution**
- Jellyfin automatically adjusts resolution based on:
  - Input resolution
  - Input framerate
  - Target bitrate
  - Client constraints
  - Available codecs

**Your System**: Fixed bitrate (5M) and resolution (copies or transcodes at original resolution)

#### 5. **HDR Tone-Mapping**
- Jellyfin supports HDR10/HLG/Dolby Vision → SDR tone-mapping
- Requires GPU for real-time performance
- Uses VPP (Intel) or Vulkan (AMD)

**Your System**: No HDR tone-mapping support

---

## Gap Analysis

### What's Missing

| Feature | Jellyfin | Your System | Priority |
|---------|----------|-------------|----------|
| Client capability negotiation | ✅ | ❌ | **HIGH** |
| Remux (container-only change) | ✅ | ❌ | **MEDIUM** |
| Direct Stream (audio-only transcode) | ✅ | ✅ (partial) | **LOW** |
| Adaptive bitrate/resolution | ✅ | ❌ | **HIGH** |
| Intel QSV support | ✅ | ❌ | **MEDIUM** |
| AMD AMF/VA-API support | ✅ | ❌ | **LOW** |
| Apple VideoToolbox | ✅ | ❌ | **LOW** |
| HDR tone-mapping | ✅ | ❌ | **MEDIUM** |
| Subtitle burn-in | ✅ | ❌ | **LOW** |
| Multi-bitrate HLS (ABR) | ✅ | ❌ | **MEDIUM** |
| DASH streaming | ✅ | ❌ | **LOW** |

### What You Have That's Good

✅ **HLS segmented streaming** - Core Jellyfin approach
✅ **Hardware acceleration** - NVIDIA support is solid
✅ **Per-profile settings** - Good UX
✅ **Separate audio/video control** - Flexibility
✅ **Session management** - Proper cleanup
✅ **Seeking support** - Time-based seeking works
✅ **CPU fallback** - Reliability

---

## Recommendations

### Phase 1: Client Capability Negotiation (HIGH PRIORITY)

**Problem**: Server decides transcoding based only on codec compatibility, not client capabilities.

**Solution**: Implement client profile system

```typescript
interface ClientProfile {
  // Supported codecs
  videoCodecs: string[];  // ['h264', 'hevc', 'vp9', 'av1']
  audioCodecs: string[];  // ['aac', 'mp3', 'opus', 'ac3']
  
  // Constraints
  maxWidth: number;       // 1920, 3840, etc.
  maxHeight: number;      // 1080, 2160, etc.
  maxBitrate: number;     // 8000000 (8 Mbps)
  maxFramerate: number;   // 30, 60
  
  // Container support
  containers: string[];   // ['mp4', 'webm', 'mkv']
  
  // Features
  supportsHDR: boolean;
  supportsSubtitles: boolean;
}
```

**Implementation**:
1. Client sends profile in request headers or query params
2. Server compares media properties vs client profile
3. Server decides: Direct Play, Remux, Direct Stream, or Transcode
4. Server picks optimal bitrate/resolution within constraints

### Phase 2: Adaptive Bitrate Streaming (HIGH PRIORITY)

**Problem**: Fixed bitrate doesn't adapt to network conditions or client capabilities.

**Solution**: Generate multiple quality variants

```typescript
// Generate HLS master playlist with multiple bitrates
const variants = [
  { resolution: '1920x1080', bitrate: 8000000, preset: 'p4' },
  { resolution: '1280x720',  bitrate: 4000000, preset: 'p3' },
  { resolution: 854x480',    bitrate: 2000000, preset: 'p2' },
];

// Client automatically switches based on bandwidth
```

### Phase 3: Intel QSV Support (MEDIUM PRIORITY)

**Problem**: Most users have Intel iGPUs, not NVIDIA GPUs.

**Solution**: Add QSV detection and encoding

```typescript
// Check for Intel QSV
const hasQSV = await checkQSVSupport();

if (hasQSV) {
  command
    .inputOptions(['-hwaccel', 'qsv', '-hwaccel_output_format', 'qsv'])
    .videoCodec('h264_qsv')
    .addOutputOption('-preset', 'medium')
    .addOutputOption('-global_quality', '23');
}
```

### Phase 4: HDR Tone-Mapping (MEDIUM PRIORITY)

**Problem**: HDR content looks washed out on SDR displays.

**Solution**: Implement tone-mapping filters

```typescript
if (isHDR && !clientSupportsHDR) {
  // Intel VPP tone-mapping
  command.addOutputOption('-vf', 'vpp_tonemap=format=nv12:matrix=bt709:primaries=bt709:transfer=bt709');
  
  // Or Vulkan tone-mapping (AMD/NVIDIA)
  command.addOutputOption('-vf', 'hwupload,tonemap_vulkan=format=nv12,hwdownload,format=nv12');
}
```

### Phase 5: Remux Support (MEDIUM PRIORITY)

**Problem**: MKV files with compatible codecs still get transcoded.

**Solution**: Add container-only remuxing

```typescript
if (videoCompatible && audioCompatible && containerIncompatible) {
  // Remux: Change container, keep streams
  command
    .videoCodec('copy')
    .audioCodec('copy')
    .outputFormat('mp4')
    .addOutputOption('-movflags', '+faststart');
}
```

---

## Proposed Architecture Changes

### 1. Enhanced Compatibility Check

```typescript
interface TranscodingDecision {
  playbackType: 'direct-play' | 'remux' | 'direct-stream' | 'transcode';
  transcodeVideo: boolean;
  transcodeAudio: boolean;
  targetResolution?: { width: number; height: number };
  targetBitrate: number;
  targetCodec: string;
  reason: string;
}

async function determineTranscoding(
  mediaInfo: MediaInfo,
  clientProfile: ClientProfile,
  userPreferences: StreamingPreferences
): Promise<TranscodingDecision> {
  // 1. Check if direct play is possible
  if (isDirectPlayPossible(mediaInfo, clientProfile)) {
    return { playbackType: 'direct-play', ... };
  }
  
  // 2. Check if remux is sufficient
  if (isRemuxSufficient(mediaInfo, clientProfile)) {
    return { playbackType: 'remux', ... };
  }
  
  // 3. Check if direct stream works (audio-only transcode)
  if (isDirectStreamPossible(mediaInfo, clientProfile)) {
    return { playbackType: 'direct-stream', transcodeAudio: true, ... };
  }
  
  // 4. Full transcode required
  return {
    playbackType: 'transcode',
    transcodeVideo: true,
    transcodeAudio: true,
    targetResolution: calculateOptimalResolution(mediaInfo, clientProfile),
    targetBitrate: calculateOptimalBitrate(mediaInfo, clientProfile),
    ...
  };
}
```

### 2. Hardware Acceleration Detection

```typescript
interface HardwareCapabilities {
  nvidia: { available: boolean; devices: string[] };
  intel: { available: boolean; devices: string[] };
  amd: { available: boolean; devices: string[] };
  apple: { available: boolean };
}

async function detectHardware(): Promise<HardwareCapabilities> {
  const caps: HardwareCapabilities = {
    nvidia: await checkNVENC(),
    intel: await checkQSV(),
    amd: await checkAMF(),
    apple: await checkVideoToolbox()
  };
  
  return caps;
}

function selectBestEncoder(caps: HardwareCapabilities): EncoderConfig {
  if (caps.intel.available) return { type: 'qsv', codec: 'h264_qsv' };
  if (caps.nvidia.available) return { type: 'nvenc', codec: 'h264_nvenc' };
  if (caps.amd.available) return { type: 'amf', codec: 'h264_amf' };
  if (caps.apple.available) return { type: 'videotoolbox', codec: 'h264_videotoolbox' };
  return { type: 'cpu', codec: 'libx264' };
}
```

### 3. Client Profile API

```typescript
// New endpoint: POST /api/stream/:id/session
router.post('/:id/session', async (req, res) => {
  const { clientProfile, startTime } = req.body;
  
  const mediaInfo = await probeMedia(filePath);
  const decision = await determineTranscoding(mediaInfo, clientProfile, userPrefs);
  
  if (decision.playbackType === 'direct-play') {
    return res.json({ type: 'direct-play', url: `/api/stream/${id}` });
  }
  
  const session = await createTranscodingSession(filePath, decision, startTime);
  return res.json({ type: 'hls', sessionId: session.id, url: session.playlistUrl });
});
```

---

## Implementation Priority

### Must Have (Phase 1)
1. ✅ Client capability negotiation
2. ✅ Adaptive resolution/bitrate selection
3. ✅ Remux support for compatible codecs

### Should Have (Phase 2)
4. ✅ Intel QSV support (most common hardware)
5. ✅ Multi-bitrate HLS (adaptive streaming)
6. ✅ HDR tone-mapping

### Nice to Have (Phase 3)
7. AMD AMF/VA-API support
8. Apple VideoToolbox support
9. Subtitle burn-in
10. DASH streaming

---

## Conclusion

**Your current system is already 70% Jellyfin-style!** You have:
- ✅ HLS segmented streaming
- ✅ Hardware acceleration (NVIDIA)
- ✅ On-the-fly transcoding
- ✅ Seeking support
- ✅ Per-profile settings

**To make it fully Jellyfin-like, focus on:**
1. **Client capability negotiation** - Let clients tell you what they support
2. **Adaptive bitrate/resolution** - Don't use fixed 5M bitrate
3. **Intel QSV support** - Most users have Intel iGPUs
4. **Remux support** - Don't transcode when only container is incompatible

The architecture is solid. You just need to add the missing decision logic and hardware support.
