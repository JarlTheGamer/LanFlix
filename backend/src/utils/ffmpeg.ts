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
 * Create a transcode stream (audio only, copy video)
 * This is the Jellyfin approach: only transcode what's needed
 */
export function createTranscodeStream(options: TranscodeOptions): ffmpeg.FfmpegCommand {
  const {
    inputPath,
    outputFormat = 'matroska',
    audioCodec = 'aac',
    videoCopy = true,
    startTime
  } = options;

  let command = ffmpeg(inputPath)
    .outputFormat(outputFormat)
    .audioCodec(audioCodec)
    .audioBitrate('192k');

  // Copy video stream without re-encoding (fast!)
  if (videoCopy) {
    command = command.videoCodec('copy');
  }

  // Seek to start time if provided
  if (startTime) {
    command = command.seekInput(startTime);
  }

  // Additional options for streaming
  command = command
    .addOutputOption('-movflags', 'frag_keyframe+empty_moov')
    .addOutputOption('-preset', 'ultrafast')
    .on('start', (commandLine) => {
      logger.info('FFmpeg transcode started:', commandLine);
    })
    .on('error', (err) => {
      logger.error('FFmpeg transcode error:', err);
    });

  return command;
}
