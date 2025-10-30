import ffmpeg from 'fluent-ffmpeg';
import fs from 'fs';
import path from 'path';
import logger from '../utils/logger';
import { probeMedia, isAudioCompatible, isVideoCompatible, needsTranscoding } from '../utils/ffmpeg';
import { PassThrough } from 'stream';

export class MediaConverterService {
  /**
   * Check if file needs transcoding for browser compatibility
   * Returns transcoding requirements without actually converting
   */
  async checkCompatibility(filePath: string): Promise<{
    needsTranscode: boolean;
    transcodeAudio: boolean;
    transcodeVideo: boolean;
    mediaInfo: any;
  }> {
    try {
      if (!fs.existsSync(filePath)) {
        throw new Error(`File not found: ${filePath}`);
      }

      const mediaInfo = await probeMedia(filePath);
      const audioCompatible = isAudioCompatible(mediaInfo.audioCodec);
      const videoCompatible = isVideoCompatible(mediaInfo.videoCodec);

      const needsTranscode = !audioCompatible || !videoCompatible;

      logger.info(`Compatibility check: ${filePath}`, {
        audioCodec: mediaInfo.audioCodec,
        videoCodec: mediaInfo.videoCodec,
        audioCompatible,
        videoCompatible,
        needsTranscode
      });

      return {
        needsTranscode,
        transcodeAudio: !audioCompatible,
        transcodeVideo: !videoCompatible,
        mediaInfo
      };
    } catch (error) {
      logger.error(`Error checking compatibility: ${error}`);
      throw error;
    }
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
    }
  ): PassThrough {
    const outputStream = new PassThrough();

    logger.info(`Starting real-time transcode stream: ${filePath}`, options);

    let command = ffmpeg(filePath);

    // NVDEC hardware-accelerated decoding (GPU decode)
    if (options.transcodeVideo) {
      command = command
        .inputOptions([
          '-hwaccel', 'cuda',
          '-hwaccel_output_format', 'cuda',
          '-extra_hw_frames', '8'
        ]);
    }

    // Seek if needed (before decoding for efficiency)
    if (options.startTime && options.startTime > 0) {
      command = command.seekInput(options.startTime);
    }

    command = command
      .outputFormat('mp4')
      .addOutputOption('-movflags', 'frag_keyframe+empty_moov+faststart')
      .addOutputOption('-map', '0:v:0')
      .addOutputOption('-map', '0:a:0?'); // Optional audio

    // Video transcoding with NVENC (GPU encode)
    if (options.transcodeVideo) {
      logger.info('Using NVDEC decode + NVENC encode (full GPU pipeline)');
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
      logger.info('Copying video stream (no transcode)');
      command = command.videoCodec('copy');
    }

    // Audio transcoding
    if (options.transcodeAudio) {
      logger.info('Transcoding audio to AAC');
      command = command
        .audioCodec('aac')
        .audioBitrate('192k')
        .audioChannels(2)
        .audioFrequency(48000);
    } else {
      logger.info('Copying audio stream (no transcode)');
      command = command.audioCodec('copy');
    }

    // Streaming optimizations
    command = command
      .addOutputOption('-avoid_negative_ts', 'make_zero')
      .addOutputOption('-max_muxing_queue_size', '1024')
      .on('start', (commandLine) => {
        logger.info('FFmpeg stream started:', commandLine);
      })
      .on('progress', (progress) => {
        if (progress.percent) {
          logger.debug(`Stream progress: ${progress.percent.toFixed(1)}%`);
        }
      })
      .on('error', (err, stdout, stderr) => {
        logger.error('FFmpeg stream error:', err.message);
        if (stderr) logger.error('FFmpeg stderr:', stderr);
        
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
    }
  ): PassThrough {
    const outputStream = new PassThrough();

    logger.info(`Starting CPU transcode stream: ${filePath}`, options);

    let command = ffmpeg(filePath);

    if (options.startTime && options.startTime > 0) {
      command = command.seekInput(options.startTime);
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
      .on('start', (commandLine) => {
        logger.info('FFmpeg CPU stream started:', commandLine);
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
