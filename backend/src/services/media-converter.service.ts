import ffmpeg from 'fluent-ffmpeg';
import fs from 'fs';
import logger from '../utils/logger';
import { probeMedia, isAudioCompatible, isVideoCompatible } from '../utils/ffmpeg';
import { PassThrough } from 'stream';

export type PlaybackMode = 'direct-play' | 'remux' | 'direct-stream' | 'transcode';

export interface CompatibilityCheck {
  playbackMode: PlaybackMode;
  needsTranscode: boolean;
  transcodeAudio: boolean;
  transcodeVideo: boolean;
  needsRemux: boolean;
  canDirectPlay: boolean;
  mediaInfo: any;
  reason: string;
}

export class MediaConverterService {
  /**
   * Check if container format is browser-compatible
   * Browsers support: MP4, WebM
   */
  isContainerCompatible(container?: string): boolean {
    if (!container) return false;

    // MP4 and its variants
    const mp4Containers = ['mp4', 'mov', 'm4v', 'mp4,mov,m4a,3gp,3g2,mj2'];
    // WebM
    const webmContainers = ['webm', 'matroska,webm'];

    const compatible = [...mp4Containers, ...webmContainers];
    return compatible.some(c => container.toLowerCase().includes(c));
  }

  /**
   * Determine optimal playback mode (Jellyfin-style)
   * 1. Direct Play - No transcoding, direct file streaming
   * 2. Remux - Container change only (e.g., MKV -> MP4)
   * 3. Direct Stream - Audio transcode, video copy
   * 4. Transcode - Full video+audio transcoding
   */
  async checkCompatibility(filePath: string): Promise<CompatibilityCheck> {
    try {
      if (!fs.existsSync(filePath)) {
        throw new Error(`File not found: ${filePath}`);
      }

      const mediaInfo = await probeMedia(filePath);
      const audioCompatible = isAudioCompatible(mediaInfo.audioCodec);
      const videoCompatible = isVideoCompatible(mediaInfo.videoCodec);
      const containerCompatible = this.isContainerCompatible(mediaInfo.container);

      // Determine playback mode
      let playbackMode: PlaybackMode;
      let reason: string;

      if (audioCompatible && videoCompatible && containerCompatible) {
        // Perfect - everything is compatible
        playbackMode = 'direct-play';
        reason = 'All codecs and container compatible';
      } else if (audioCompatible && videoCompatible && !containerCompatible) {
        // Only container needs changing (MKV -> MP4)
        playbackMode = 'remux';
        reason = `Container incompatible (${mediaInfo.container}), remuxing to MP4`;
      } else if (!audioCompatible && videoCompatible) {
        // Only audio needs transcoding
        playbackMode = 'direct-stream';
        reason = `Audio incompatible (${mediaInfo.audioCodec}), transcoding audio only`;
      } else {
        // Full transcoding needed
        playbackMode = 'transcode';
        reason = `Video/audio incompatible (v:${mediaInfo.videoCodec}, a:${mediaInfo.audioCodec})`;
      }

      logger.info(`Compatibility check: ${filePath}`, {
        audioCodec: mediaInfo.audioCodec,
        videoCodec: mediaInfo.videoCodec,
        container: mediaInfo.container,
        audioCompatible,
        videoCompatible,
        containerCompatible,
        playbackMode,
        reason
      });

      return {
        playbackMode,
        needsTranscode: playbackMode === 'transcode' || playbackMode === 'direct-stream',
        transcodeAudio: !audioCompatible,
        transcodeVideo: !videoCompatible,
        needsRemux: playbackMode === 'remux',
        canDirectPlay: playbackMode === 'direct-play',
        mediaInfo,
        reason
      };
    } catch (error) {
      logger.error(`Error checking compatibility: ${error}`);
      throw error;
    }
  }

  /**
   * Create a remux stream (container change only, no transcoding)
   * Used when codecs are compatible but container isn't (e.g., MKV -> MP4)
   */
  createRemuxStream(
    filePath: string,
    options: {
      startTime?: number;
    }
  ): PassThrough {
    const outputStream = new PassThrough();

    logger.info(`Starting remux stream: ${filePath}`, options);

    let command = ffmpeg(filePath);

    // Seek if needed (fast seeking for remux)
    if (options.startTime && options.startTime > 0) {
      command = command.seekInput(options.startTime);
    }

    command = command
      .outputFormat('mp4')
      .addOutputOption('-movflags', 'frag_keyframe+empty_moov+faststart')
      .addOutputOption('-map', '0:v:0')
      .addOutputOption('-map', '0:a:0?')
      .videoCodec('copy')  // Copy video stream
      .audioCodec('copy')  // Copy audio stream
      .addOutputOption('-avoid_negative_ts', 'make_zero')
      .addOutputOption('-max_muxing_queue_size', '1024')
      .on('start', (commandLine) => {
        logger.info('FFmpeg remux started:', commandLine);
      })
      .on('error', (err) => {
        logger.error('FFmpeg remux error:', err.message);
        outputStream.destroy(err);
      })
      .on('end', () => {
        logger.info('FFmpeg remux completed');
        outputStream.end();
      });

    command.pipe(outputStream, { end: true });

    return outputStream;
  }

  /**
   * Create a real-time transcode stream (Jellyfin-style)
   * Transcodes on-the-fly as data is requested
   */
  createTranscodeStream(
    filePath: string,
    options: {
      transcodeAudio: boolean;
      transcodeVideo: boolean;
      startTime?: number;
      progressCallback?: (progress: { timemark: string; percent?: number }) => void;
    }
  ): PassThrough {
    const outputStream = new PassThrough();

    logger.info(`Starting real-time transcode stream: ${filePath}`, options);

    let command = ffmpeg(filePath);

    // NVDEC hardware-accelerated decoding (GPU decode)
    // Only use hardware decode if we're transcoding video
    if (options.transcodeVideo) {
      command = command
        .inputOptions([
          '-hwaccel', 'cuda',
          '-hwaccel_output_format', 'cuda',
          '-extra_hw_frames', '8'
        ]);
    }

    // Seek if needed - use input seeking with noaccurate_seek for better sync
    if (options.startTime && options.startTime > 0) {
      command = command
        .seekInput(options.startTime)
        .inputOptions(['-noaccurate_seek']); // Fast seek, better for A/V sync
    }

    command = command
      .outputFormat('mp4')
      .addOutputOption('-movflags', 'frag_keyframe+empty_moov+faststart')
      .addOutputOption('-map', '0:v:0')
      .addOutputOption('-map', '0:a:0?'); // Optional audio

    // Video transcoding with NVENC (GPU encode)
    if (options.transcodeVideo) {
      logger.info('VIDEO: Transcoding with NVDEC decode + NVENC encode (full GPU pipeline)');
      command = command
        .videoCodec('h264_nvenc')
        .addOutputOption('-preset', 'p4')           // Balanced preset (fast + good quality)
        .addOutputOption('-tune', 'hq')
        .addOutputOption('-rc', 'vbr')
        .addOutputOption('-cq', '23')               // Good quality, not overkill
        .addOutputOption('-b:v', '5M')
        .addOutputOption('-maxrate', '8M')
        .addOutputOption('-bufsize', '10M')
        .addOutputOption('-profile:v', 'high')
        .addOutputOption('-level', '4.1')
        .addOutputOption('-pix_fmt', 'yuv420p')
        .addOutputOption('-spatial_aq', '1')
        .addOutputOption('-temporal_aq', '1')
        .addOutputOption('-rc-lookahead', '20')
        .addOutputOption('-bf', '3')
        .addOutputOption('-gpu', '0');
    } else {
      logger.info('VIDEO: Copying stream (no transcoding)');
      command = command.videoCodec('copy');
    }

    // Audio transcoding
    if (options.transcodeAudio) {
      logger.info('AUDIO: Transcoding to AAC');
      command = command
        .audioCodec('aac')
        .audioBitrate('192k')
        .audioChannels(2)
        .audioFrequency(48000);
    } else {
      logger.info('AUDIO: Copying stream (no transcoding)');
      command = command.audioCodec('copy');
    }

    // Streaming optimizations
    command = command
      .addOutputOption('-avoid_negative_ts', 'make_zero')
      .addOutputOption('-max_muxing_queue_size', '1024')
      .addOutputOption('-fflags', '+genpts')  // Generate presentation timestamps
      .on('start', (commandLine) => {
        logger.info('FFmpeg stream started:', commandLine);
      })
      .on('progress', (progress) => {
        if (options.progressCallback) {
          options.progressCallback(progress);
        }
        if (progress.percent) {
          logger.debug(`Stream progress: ${progress.percent.toFixed(1)}%`);
        }
      })
      .on('error', (err) => {
        logger.error('FFmpeg stream error:', err.message);

        // Try CPU fallback on GPU error
        if (err.message.includes('nvenc') || err.message.includes('cuda')) {
          logger.warn('GPU error detected, client should retry with CPU fallback');
        }

        outputStream.destroy(err);
      })
      .on('end', () => {
        logger.info('FFmpeg stream completed');
        outputStream.end();
      });

    // Pipe to output stream
    command.pipe(outputStream, { end: true });

    return outputStream;
  }

  /**
   * Create CPU fallback transcode stream
   */
  createCPUTranscodeStream(
    filePath: string,
    options: {
      transcodeAudio: boolean;
      transcodeVideo: boolean;
      startTime?: number;
      progressCallback?: (progress: { timemark: string; percent?: number }) => void;
    }
  ): PassThrough {
    const outputStream = new PassThrough();

    logger.info(`Starting CPU transcode stream: ${filePath}`, options);

    let command = ffmpeg(filePath);

    // Seek if needed - use input seeking with noaccurate_seek for better sync
    if (options.startTime && options.startTime > 0) {
      command = command
        .seekInput(options.startTime)
        .inputOptions(['-noaccurate_seek']); // Fast seek, better for A/V sync
    }

    command = command
      .outputFormat('mp4')
      .addOutputOption('-movflags', 'frag_keyframe+empty_moov+faststart')
      .addOutputOption('-map', '0:v:0')
      .addOutputOption('-map', '0:a:0?');

    // CPU video transcoding
    if (options.transcodeVideo) {
      logger.info('Using CPU encoding (libx264)');
      command = command
        .videoCodec('libx264')
        .addOutputOption('-preset', 'veryfast')    // Fast CPU preset
        .addOutputOption('-crf', '23')
        .addOutputOption('-profile:v', 'high')
        .addOutputOption('-level', '4.1')
        .addOutputOption('-pix_fmt', 'yuv420p');
    } else {
      command = command.videoCodec('copy');
    }

    // Audio transcoding (same as GPU)
    if (options.transcodeAudio) {
      command = command
        .audioCodec('aac')
        .audioBitrate('192k')
        .audioChannels(2)
        .audioFrequency(48000);
    } else {
      command = command.audioCodec('copy');
    }

    command = command
      .addOutputOption('-avoid_negative_ts', 'make_zero')
      .addOutputOption('-max_muxing_queue_size', '1024')
      .addOutputOption('-fflags', '+genpts')  // Generate presentation timestamps
      .on('start', (commandLine) => {
        logger.info('FFmpeg CPU stream started:', commandLine);
      })
      .on('progress', (progress) => {
        if (options.progressCallback) {
          options.progressCallback(progress);
        }
      })
      .on('error', (err) => {
        logger.error('FFmpeg CPU stream error:', err);
        outputStream.destroy(err);
      })
      .on('end', () => {
        logger.info('FFmpeg CPU stream completed');
        outputStream.end();
      });

    command.pipe(outputStream, { end: true });

    return outputStream;
  }

}

export const mediaConverterService = new MediaConverterService();