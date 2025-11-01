import ffmpeg from 'fluent-ffmpeg';
import logger from './logger';

export interface MediaInfo {
  hasVideo: boolean;
  hasAudio: boolean;
  videoCodec?: string;
  audioCodec?: string;
  duration?: number;
  bitrate?: number;
  width?: number;
  height?: number;
  container?: string;
}

export interface TranscodeOptions {
  inputPath: string;
  outputFormat?: string;
  audioCodec?: string;
  videoCopy?: boolean;
  startTime?: number;
  preset?: string;
  useHardwareAccel?: boolean;
}

/**
 * Probe media file to get codec and stream information
 */
export async function probeMedia(filePath: string): Promise<MediaInfo> {
  return new Promise((resolve, reject) => {
    // Validate that the path is a file, not a directory
    const fs = require('fs');
    if (!fs.existsSync(filePath)) {
      return reject(new Error(`File does not exist: ${filePath}`));
    }

    const stats = fs.statSync(filePath);
    if (stats.isDirectory()) {
      return reject(new Error(`Path is a directory, not a file: ${filePath}`));
    }

    ffmpeg.ffprobe(filePath, (err, metadata) => {
      if (err) {
        logger.error('FFprobe error:', err);
        return reject(err);
      }

      const videoStream = metadata.streams.find(s => s.codec_type === 'video');
      const audioStream = metadata.streams.find(s => s.codec_type === 'audio');

      const info: MediaInfo = {
        hasVideo: !!videoStream,
        hasAudio: !!audioStream,
        videoCodec: videoStream?.codec_name,
        audioCodec: audioStream?.codec_name,
        duration: metadata.format.duration,
        bitrate: metadata.format.bit_rate ? parseInt(String(metadata.format.bit_rate)) : undefined,
        width: videoStream?.width,
        height: videoStream?.height,
        container: metadata.format.format_name
      };

      resolve(info);
    });
  });
}

/**
 * Check if audio codec is browser-compatible
 * Browsers support: AAC, MP3, Opus, Vorbis
 */
export function isAudioCompatible(audioCodec?: string): boolean {
  if (!audioCodec) return false;

  const compatibleCodecs = ['aac', 'mp3', 'opus', 'vorbis'];
  return compatibleCodecs.includes(audioCodec.toLowerCase());
}

/**
 * Check if video codec is browser-compatible
 * Browsers support: H.264, VP8, VP9, AV1
 */
export function isVideoCompatible(videoCodec?: string): boolean {
  if (!videoCodec) return false;

  const compatibleCodecs = ['h264', 'vp8', 'vp9', 'av1'];
  return compatibleCodecs.includes(videoCodec.toLowerCase());
}

/**
 * Determine if file needs transcoding
 */
export async function needsTranscoding(filePath: string): Promise<{
  needsTranscode: boolean;
  transcodeAudio: boolean;
  transcodeVideo: boolean;
  reason?: string;
}> {
  try {
    const info = await probeMedia(filePath);

    if (!info.hasAudio && !info.hasVideo) {
      return {
        needsTranscode: false,
        transcodeAudio: false,
        transcodeVideo: false,
        reason: 'No audio or video streams found'
      };
    }

    const audioCompatible = info.hasAudio ? isAudioCompatible(info.audioCodec) : true;
    const videoCompatible = info.hasVideo ? isVideoCompatible(info.videoCodec) : true;

    if (!info.hasAudio) {
      return {
        needsTranscode: false,
        transcodeAudio: false,
        transcodeVideo: false,
        reason: 'No audio track - direct play'
      };
    }

    if (audioCompatible && videoCompatible) {
      return {
        needsTranscode: false,
        transcodeAudio: false,
        transcodeVideo: false,
        reason: 'All codecs compatible - direct play'
      };
    }

    return {
      needsTranscode: true,
      transcodeAudio: !audioCompatible,
      transcodeVideo: !videoCompatible,
      reason: `Incompatible: ${!audioCompatible ? 'audio(' + info.audioCodec + ')' : ''} ${!videoCompatible ? 'video(' + info.videoCodec + ')' : ''}`
    };
  } catch (error) {
    logger.error('Error checking transcode needs:', error);
    return {
      needsTranscode: false,
      transcodeAudio: false,
      transcodeVideo: false,
      reason: 'Probe failed - attempting direct play'
    };
  }
}

/**
 * Create a transcode stream with HLS segmentation
 * Supports audio-only, video-only, or both transcoding
 * Uses hardware acceleration when available
 */
export function createTranscodeStream(options: TranscodeOptions): ffmpeg.FfmpegCommand {
  const {
    inputPath,
    outputFormat = 'mpegts',
    audioCodec = 'aac',
    videoCopy = true,
    startTime,
    preset = 'p4',
    useHardwareAccel = true
  } = options;

  let command = ffmpeg(inputPath);

  // Hardware acceleration setup (NVDEC for decoding)
  if (useHardwareAccel && !videoCopy) {
    command = command
      .inputOption('-hwaccel', 'cuda')
      .inputOption('-hwaccel_output_format', 'cuda');
  }

  // Seek to start time if provided (accurate seeking)
  if (startTime && startTime > 0) {
    command = command.seekInput(startTime);
  }

  command = command.outputFormat(outputFormat);

  // Audio handling
  if (audioCodec === 'copy') {
    command = command.audioCodec('copy');
  } else {
    command = command
      .audioCodec(audioCodec)
      .audioBitrate('192k')
      .audioChannels(2);
  }

  // Video handling
  if (videoCopy) {
    // Copy video stream without re-encoding (fast!)
    command = command.videoCodec('copy');
  } else {
    // Transcode video with hardware acceleration
    if (useHardwareAccel) {
      // NVENC hardware encoding
      command = command
        .videoCodec('h264_nvenc')
        .addOutputOption('-preset', preset) // p1-p7, p4 is balanced
        .addOutputOption('-rc', 'vbr')
        .addOutputOption('-cq', '23')
        .addOutputOption('-b:v', '4M')
        .addOutputOption('-maxrate', '6M')
        .addOutputOption('-bufsize', '8M')
        .addOutputOption('-profile:v', 'high')
        .addOutputOption('-level', '4.1');
    } else {
      // Software encoding fallback
      command = command
        .videoCodec('libx264')
        .addOutputOption('-preset', 'veryfast')
        .addOutputOption('-crf', '23')
        .addOutputOption('-maxrate', '4M')
        .addOutputOption('-bufsize', '8M')
        .addOutputOption('-profile:v', 'high')
        .addOutputOption('-level', '4.1');
    }
  }

  // Streaming optimizations
  command = command
    .addOutputOption('-movflags', '+faststart')
    .addOutputOption('-avoid_negative_ts', 'make_zero')
    .addOutputOption('-fflags', '+genpts+discardcorrupt')
    .addOutputOption('-max_muxing_queue_size', '1024')
    .addOutputOption('-copyts')
    .on('start', (commandLine) => {
      logger.info('FFmpeg transcode started:', commandLine);
    })
    .on('error', (err) => {
      logger.error('FFmpeg transcode error:', err);
    })
    .on('stderr', (stderrLine) => {
      // Log progress for debugging
      if (stderrLine.includes('time=')) {
        logger.debug('Transcode progress:', stderrLine);
      }
    });

  return command;
}

/**
 * Check if NVIDIA hardware acceleration is available
 */
export async function checkHardwareAccel(): Promise<boolean> {
  return new Promise((resolve) => {
    ffmpeg()
      .input('color=c=black:s=256x256:d=1')
      .inputFormat('lavfi')
      .videoCodec('h264_nvenc')
      .output('/dev/null')
      .on('error', () => resolve(false))
      .on('end', () => resolve(true))
      .run();
  });
}
