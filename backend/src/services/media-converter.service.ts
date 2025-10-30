import ffmpeg from 'fluent-ffmpeg';
import fs from 'fs';
import path from 'path';
import { randomUUID } from 'crypto';
import logger from '../utils/logger';
import { probeMedia, isAudioCompatible, isVideoCompatible } from '../utils/ffmpeg';
import { PassThrough } from 'stream';

interface HLSSession {
  id: string;
  filePath: string;
  outputDir: string;
  playlistPath: string;
  command: ffmpeg.FfmpegCommand;
  createdAt: number;
  lastAccess: number;
  startTime: number;
  cleanupTimer?: NodeJS.Timeout;
  transcodeAudio: boolean;
  transcodeVideo: boolean;
}

export class MediaConverterService {
  private readonly hlsSessions = new Map<string, HLSSession>();
  private readonly sessionTimeoutMs = 5 * 60 * 1000; // 5 minutes inactivity timeout
  private readonly hlsRoot = path.join(process.cwd(), 'tmp', 'hls');

  /**
   * Check if file needs transcoding for browser compatibility
   * Returns transcoding requirements without actually converting
   */
  async checkCompatibility(filePath: string): Promise<{
    needsTranscode: boolean;
    transcodeAudio: boolean;
    transcodeVideo: boolean;
    mediaInfo: MediaInfo;
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

  private ensureHlsRoot(): void {
    if (!fs.existsSync(this.hlsRoot)) {
      fs.mkdirSync(this.hlsRoot, { recursive: true });
    }
  }

  private scheduleCleanup(session: HLSSession): void {
    if (session.cleanupTimer) {
      clearTimeout(session.cleanupTimer);
    }

    session.cleanupTimer = setTimeout(() => {
      logger.info(`Cleaning up idle HLS session ${session.id}`);
      this.endSession(session.id);
    }, this.sessionTimeoutMs);
  }

  private touchSession(sessionId: string): void {
    const session = this.hlsSessions.get(sessionId);
    if (!session) {
      return;
    }

    session.lastAccess = Date.now();
    this.scheduleCleanup(session);
  }

  private getSession(sessionId: string): HLSSession {
    const session = this.hlsSessions.get(sessionId);
    if (!session) {
      throw new Error(`HLS session not found: ${sessionId}`);
    }

    return session;
  }

  private removeSessionArtifacts(outputDir: string): void {
    try {
      if (fs.existsSync(outputDir)) {
        fs.rmSync(outputDir, { recursive: true, force: true });
      }
    } catch (error) {
      logger.warn(`Failed to remove HLS session directory ${outputDir}:`, error);
    }
  }

  private async waitForFile(filePath: string, timeoutMs = 5000): Promise<void> {
    const start = Date.now();

    while (Date.now() - start < timeoutMs) {
      try {
        const stats = await fs.promises.stat(filePath);
        if (stats.isFile() && stats.size > 0) {
          return;
        }
      } catch {
        // File not ready yet, continue waiting
      }

      await new Promise(resolve => setTimeout(resolve, 100));
    }

    throw new Error(`Timed out waiting for file to be ready: ${filePath}`);
  }

  endSession(sessionId: string): void {
    const session = this.hlsSessions.get(sessionId);
    if (!session) {
      return;
    }

    if (session.cleanupTimer) {
      clearTimeout(session.cleanupTimer);
    }

    try {
      session.command.kill('SIGKILL');
    } catch (error) {
      logger.warn(`Failed to terminate FFmpeg for session ${sessionId}:`, error);
    }

    this.hlsSessions.delete(sessionId);
    this.removeSessionArtifacts(session.outputDir);
  }

  async getHlsManifest(sessionId: string): Promise<string> {
    const session = this.getSession(sessionId);
    await this.waitForFile(session.playlistPath, 7000);
    this.touchSession(sessionId);

    return fs.readFileSync(session.playlistPath, 'utf8');
  }

  async getHlsSegmentStream(sessionId: string, segmentName: string): Promise<fs.ReadStream> {
    const session = this.getSession(sessionId);
    const safeSegment = path.basename(segmentName);

    if (safeSegment !== segmentName) {
      throw new Error('Invalid segment name');
    }

    const segmentPath = path.join(session.outputDir, safeSegment);
    await this.waitForFile(segmentPath, 7000);
    this.touchSession(sessionId);

    return fs.createReadStream(segmentPath);
  }

  getSessionMetadata(sessionId: string): {
    transcodeAudio: boolean;
    transcodeVideo: boolean;
    startTime: number;
  } {
    const session = this.getSession(sessionId);
    this.touchSession(sessionId);

    return {
      transcodeAudio: session.transcodeAudio,
      transcodeVideo: session.transcodeVideo,
      startTime: session.startTime
    };
  }

  createHlsSession(
    filePath: string,
    options: {
      transcodeAudio: boolean;
      transcodeVideo: boolean;
      startTime?: number;
      preset?: string;
    }
  ): { sessionId: string; playlistPath: string } {
    this.ensureHlsRoot();

    const sessionId = randomUUID();
    const outputDir = path.join(this.hlsRoot, sessionId);
    fs.mkdirSync(outputDir, { recursive: true });

    const playlistPath = path.join(outputDir, 'index.m3u8');
    const segmentPattern = path.join(outputDir, 'segment_%05d.ts');

    let command = ffmpeg(filePath);

    if (options.startTime && options.startTime > 0) {
      command = command.seekInput(options.startTime);
    }

    if (options.transcodeVideo) {
      command = command.inputOptions([
        '-hwaccel', 'cuda',
        '-hwaccel_output_format', 'cuda',
        '-extra_hw_frames', '8'
      ]);
    }

    command = command
      .addOutputOption('-map', '0:v:0')
      .addOutputOption('-map', '0:a:0?');

    if (options.transcodeVideo) {
      const preset = options.preset || 'p4';
      command = command
        .videoCodec('h264_nvenc')
        .addOutputOption('-preset', preset)
        .addOutputOption('-rc', 'vbr')
        .addOutputOption('-cq', '23')
        .addOutputOption('-b:v', '5M')
        .addOutputOption('-maxrate', '8M')
        .addOutputOption('-bufsize', '10M')
        .addOutputOption('-profile:v', 'high')
        .addOutputOption('-level', '4.1')
        .addOutputOption('-g', '48')
        .addOutputOption('-keyint_min', '48')
        .addOutputOption('-pix_fmt', 'yuv420p')
        .addOutputOption('-spatial_aq', '1')
        .addOutputOption('-temporal_aq', '1')
        .addOutputOption('-rc-lookahead', '20');
    } else {
      command = command.videoCodec('copy');
    }

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
      .output(playlistPath)
      .addOutputOption('-f', 'hls')
      .addOutputOption('-hls_time', '4')
      .addOutputOption('-hls_list_size', '0')
      .addOutputOption('-hls_flags', 'append_list+independent_segments')
      .addOutputOption('-hls_segment_filename', segmentPattern)
      .addOutputOption('-max_muxing_queue_size', '2048')
      .addOutputOption('-fflags', '+genpts+discardcorrupt')
      .addOutputOption('-reset_timestamps', '1')
      .on('start', (commandLine) => {
        logger.info(`FFmpeg HLS session ${sessionId} started: ${commandLine}`);
      })
      .on('stderr', (stderrLine) => {
        if (stderrLine.includes('fps=') || stderrLine.includes('speed=')) {
          logger.debug(`FFmpeg HLS session ${sessionId}: ${stderrLine}`);
        }
      })
      .on('error', (err, stdout, stderr) => {
        logger.error(`FFmpeg HLS session ${sessionId} error: ${err.message}`);
        if (stderr) {
          logger.error(stderr);
        }
        this.endSession(sessionId);
      })
      .on('end', () => {
        logger.info(`FFmpeg HLS session ${sessionId} completed`);
      });

    command.run();

    const session: HLSSession = {
      id: sessionId,
      filePath,
      outputDir,
      playlistPath,
      command,
      createdAt: Date.now(),
      lastAccess: Date.now(),
      startTime: options.startTime || 0,
      transcodeAudio: options.transcodeAudio,
      transcodeVideo: options.transcodeVideo
    };

    this.hlsSessions.set(sessionId, session);
    this.scheduleCleanup(session);

    return { sessionId, playlistPath };
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

    // Seek if needed (before decoding for efficiency)
    if (options.startTime && options.startTime > 0) {
      command = command.seekInput(options.startTime);
    }

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
