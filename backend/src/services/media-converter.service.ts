import ffmpeg from 'fluent-ffmpeg';
import fs from 'fs';
import path from 'path';
import logger from '../utils/logger';
import { probeMedia, isAudioCompatible, isVideoCompatible } from '../utils/ffmpeg';

export class MediaConverterService {
  private conversionQueue: Map<string, boolean> = new Map();

  /**
   * Check if file needs conversion and convert if necessary
   * Returns the path to the playable file (original or converted)
   */
  async ensureCompatible(filePath: string): Promise<string> {
    try {
      // Check if already converting
      if (this.conversionQueue.has(filePath)) {
        logger.info(`File already being converted, skipping: ${filePath}`);
        return filePath;
      }

      // Check if file exists
      if (!fs.existsSync(filePath)) {
        throw new Error(`File not found: ${filePath}`);
      }

      // Check if a .converting file exists (previous conversion in progress)
      const dir = path.dirname(filePath);
      const ext = path.extname(filePath);
      const basename = path.basename(filePath, ext);
      const convertingFile = path.join(dir, `${basename}.converting.mp4`);

      if (fs.existsSync(convertingFile)) {
        logger.info(`Conversion already in progress (temp file exists): ${convertingFile}`);
        return filePath;
      }

      // Skip if this IS a .converting file (incomplete conversion)
      if (filePath.includes('.converting.')) {
        logger.warn(`Skipping incomplete conversion file: ${filePath}`);
        return filePath;
      }

      // Probe the media file
      const mediaInfo = await probeMedia(filePath);

      const audioCompatible = isAudioCompatible(mediaInfo.audioCodec);
      const videoCompatible = isVideoCompatible(mediaInfo.videoCodec);

      // If everything is compatible, return original file
      if (audioCompatible && videoCompatible) {
        logger.info(`File is already compatible: ${filePath}`);
        return filePath;
      }

      // File needs conversion
      logger.info(`File needs conversion: ${filePath}`, {
        audioCodec: mediaInfo.audioCodec,
        videoCodec: mediaInfo.videoCodec,
        audioCompatible,
        videoCompatible
      });

      // Convert the file
      const convertedPath = await this.convertFile(filePath, {
        transcodeAudio: !audioCompatible,
        transcodeVideo: !videoCompatible
      });

      return convertedPath;
    } catch (error) {
      logger.error(`Error ensuring file compatibility: ${error}`);
      throw error;
    }
  }

  /**
   * Convert media file to browser-compatible format
   */
  private async convertFile(
    inputPath: string,
    options: { transcodeAudio: boolean; transcodeVideo: boolean }
  ): Promise<string> {
    return new Promise((resolve, reject) => {
      // Mark as converting
      this.conversionQueue.set(inputPath, true);

      const dir = path.dirname(inputPath);
      const ext = path.extname(inputPath);
      const basename = path.basename(inputPath, ext);
      const tempOutput = path.join(dir, `${basename}.converting.mp4`);
      const finalOutput = path.join(dir, `${basename}.mp4`);

      logger.info(`Starting HIGH QUALITY conversion: ${inputPath} -> ${finalOutput}`, {
        transcodeAudio: options.transcodeAudio,
        transcodeVideo: options.transcodeVideo,
        quality: 'near-lossless'
      });

      let command = ffmpeg(inputPath)
        .inputOptions([
          '-hwaccel', 'cuda',                        // Enable CUDA hardware acceleration
          '-hwaccel_output_format', 'cuda',          // Keep decoded frames on GPU
          '-extra_hw_frames', '16',                  // More hardware frame buffers for better GPU utilization
          '-threads', '0'                            // Use all available CPU threads for demuxing
        ])
        .outputFormat('mp4')
        .addOutputOption('-movflags', '+faststart')  // Optimize for streaming
        .addOutputOption('-map', '0:v:0')            // Map first video stream
        .addOutputOption('-map', '0:a:0');           // Map first audio stream

      // Video handling
      if (options.transcodeVideo) {
        logger.info('Transcoding video to H.264 with MAXIMUM GPU acceleration (RTX 4070 Ti optimized)');
        command = command
          .videoCodec('h264_nvenc')                // NVIDIA GPU encoding
          .addOutputOption('-preset', 'p7')        // Maximum quality preset (4070 Ti can handle it)
          .addOutputOption('-tune', 'hq')          // High quality tuning
          .addOutputOption('-multipass', 'fullres') // Two-pass encoding for best quality
          .addOutputOption('-rc', 'vbr')           // Variable bitrate for quality
          .addOutputOption('-cq', '16')            // Quality level (16 = near-lossless)
          .addOutputOption('-b:v', '0')            // Let CQ control bitrate
          .addOutputOption('-maxrate', '80M')      // High bitrate cap for 4K content
          .addOutputOption('-bufsize', '160M')     // Large buffer for VBR (2x maxrate)
          .addOutputOption('-profile:v', 'high')   // H.264 High Profile
          .addOutputOption('-level', '5.2')        // Highest level for 4K support
          .addOutputOption('-pix_fmt', 'yuv420p')  // Maximum compatibility
          .addOutputOption('-spatial_aq', '1')     // Spatial AQ for better quality
          .addOutputOption('-temporal_aq', '1')    // Temporal AQ for better quality
          .addOutputOption('-aq-strength', '15')   // Maximum AQ strength for quality
          .addOutputOption('-rc-lookahead', '32')  // Full lookahead for best decisions
          .addOutputOption('-bf', '5')             // Maximum B-frames for compression
          .addOutputOption('-b_ref_mode', 'middle') // B-frame reference mode
          .addOutputOption('-refs', '4')           // More reference frames
          .addOutputOption('-gpu', '0')            // Use first GPU explicitly
          .addOutputOption('-2pass', '1')          // Enable 2-pass mode for quality
          .addOutputOption('-surfaces', '64')      // Maximum surfaces for GPU utilization
          .addOutputOption('-async_depth', '8')    // Higher async depth for 4070 Ti
          .addOutputOption('-forced-idr', '1')     // Better seeking
          .addOutputOption('-strict_gop', '1')     // Strict GOP for quality
          .addOutputOption('-nonref_p', '1')       // Non-reference P frames
          .addOutputOption('-zerolatency', '0');   // Disable zero latency for quality
      } else {
        logger.info('Copying video stream (no transcoding - lossless)');
        command = command.videoCodec('copy');
      }

      // Audio handling
      if (options.transcodeAudio) {
        logger.info('Transcoding audio to AAC (high quality)');
        command = command
          .audioCodec('aac')
          .audioBitrate('320k')      // Maximum AAC quality (near-lossless)
          .audioChannels(6)          // Keep 5.1 surround if available
          .audioFrequency(48000);    // Standard frequency
      } else {
        logger.info('Copying audio stream (no transcoding - lossless)');
        command = command.audioCodec('copy');
      }

      command
        .output(tempOutput)
        .on('start', (commandLine) => {
          logger.info('FFmpeg conversion started:', commandLine);
        })
        .on('progress', (progress) => {
          if (progress.percent) {
            logger.info(`Conversion progress: ${progress.percent.toFixed(1)}% | Speed: ${progress.currentFps || 0}fps`);
          }
        })
        .on('end', () => {
          logger.info('Conversion completed successfully');

          try {
            // Verify the converted file exists and has content
            const tempStats = fs.statSync(tempOutput);
            const originalStats = fs.statSync(inputPath);

            if (tempStats.size === 0) {
              throw new Error('Converted file is empty');
            }

            // Log file size comparison
            const sizeDiff = ((tempStats.size - originalStats.size) / originalStats.size * 100).toFixed(2);
            logger.info(`File size comparison: Original=${(originalStats.size / 1024 / 1024).toFixed(2)}MB, Converted=${(tempStats.size / 1024 / 1024).toFixed(2)}MB (${sizeDiff}% difference)`);

            // Delete original file
            logger.info(`Deleting original file: ${inputPath}`);
            fs.unlinkSync(inputPath);

            // Rename temp file to final name
            logger.info(`Renaming ${tempOutput} -> ${finalOutput}`);
            fs.renameSync(tempOutput, finalOutput);

            // Remove from queue
            this.conversionQueue.delete(inputPath);

            logger.info(`✅ File successfully converted and replaced: ${finalOutput}`);
            resolve(finalOutput);
          } catch (error) {
            logger.error('Error finalizing conversion:', error);
            // Clean up temp file if it exists
            if (fs.existsSync(tempOutput)) {
              fs.unlinkSync(tempOutput);
            }
            this.conversionQueue.delete(inputPath);
            reject(error);
          }
        })
        .on('error', (err) => {
          logger.error('FFmpeg conversion error:', err);

          // Check if it's a GPU error and retry with CPU
          const errorMessage = err.message || String(err);
          if (errorMessage.includes('h264_nvenc') || errorMessage.includes('nvenc') || errorMessage.includes('cuda')) {
            logger.warn('GPU encoding failed, retrying with CPU encoding...');

            // Clean up failed temp file
            try {
              if (fs.existsSync(tempOutput)) {
                fs.unlinkSync(tempOutput);
              }
            } catch (cleanupError) {
              logger.error('Error cleaning up temp file:', cleanupError);
            }

            // Retry with CPU encoding
            this.conversionQueue.delete(inputPath);
            this.convertFileWithCPU(inputPath, options)
              .then(resolve)
              .catch(reject);
            return;
          }

          // Clean up temp file if it exists
          try {
            if (fs.existsSync(tempOutput)) {
              logger.info(`Cleaning up failed conversion temp file: ${tempOutput}`);
              fs.unlinkSync(tempOutput);
            }
          } catch (cleanupError) {
            logger.error('Error cleaning up temp file:', cleanupError);
          }

          this.conversionQueue.delete(inputPath);
          reject(err);
        })
        .run();
    });
  }

  /**
   * Convert media file using CPU encoding (fallback)
   */
  private async convertFileWithCPU(
    inputPath: string,
    options: { transcodeAudio: boolean; transcodeVideo: boolean }
  ): Promise<string> {
    return new Promise((resolve, reject) => {
      this.conversionQueue.set(inputPath, true);

      const dir = path.dirname(inputPath);
      const ext = path.extname(inputPath);
      const basename = path.basename(inputPath, ext);
      const tempOutput = path.join(dir, `${basename}.converting.mp4`);
      const finalOutput = path.join(dir, `${basename}.mp4`);

      logger.info(`Starting CPU conversion (fallback): ${inputPath} -> ${finalOutput}`);

      let command = ffmpeg(inputPath)
        .outputFormat('mp4')
        .addOutputOption('-movflags', '+faststart')
        .addOutputOption('-map', '0:v:0')
        .addOutputOption('-map', '0:a:0');

      // CPU video encoding
      if (options.transcodeVideo) {
        logger.info('Transcoding video with CPU (libx264 - fast preset)');
        command = command
          .videoCodec('libx264')
          .addOutputOption('-preset', 'fast')     // Fast CPU encoding
          .addOutputOption('-crf', '18')          // Same quality
          .addOutputOption('-profile:v', 'high')
          .addOutputOption('-level', '4.1')
          .addOutputOption('-pix_fmt', 'yuv420p');
      } else {
        command = command.videoCodec('copy');
      }

      // Audio encoding (same as GPU path)
      if (options.transcodeAudio) {
        command = command
          .audioCodec('aac')
          .audioBitrate('320k')
          .audioChannels(6)
          .audioFrequency(48000);
      } else {
        command = command.audioCodec('copy');
      }

      command
        .output(tempOutput)
        .on('start', (commandLine) => {
          logger.info('FFmpeg CPU conversion started:', commandLine);
        })
        .on('progress', (progress) => {
          if (progress.percent) {
            logger.info(`CPU Conversion progress: ${progress.percent.toFixed(1)}%`);
          }
        })
        .on('end', () => {
          logger.info('CPU Conversion completed successfully');

          try {
            const tempStats = fs.statSync(tempOutput);
            const originalStats = fs.statSync(inputPath);

            if (tempStats.size === 0) {
              throw new Error('Converted file is empty');
            }

            const sizeDiff = ((tempStats.size - originalStats.size) / originalStats.size * 100).toFixed(2);
            logger.info(`File size: Original=${(originalStats.size / 1024 / 1024).toFixed(2)}MB, Converted=${(tempStats.size / 1024 / 1024).toFixed(2)}MB (${sizeDiff}%)`);

            fs.unlinkSync(inputPath);
            fs.renameSync(tempOutput, finalOutput);
            this.conversionQueue.delete(inputPath);

            logger.info(`✅ CPU conversion completed: ${finalOutput}`);
            resolve(finalOutput);
          } catch (error) {
            logger.error('Error finalizing CPU conversion:', error);
            if (fs.existsSync(tempOutput)) {
              fs.unlinkSync(tempOutput);
            }
            this.conversionQueue.delete(inputPath);
            reject(error);
          }
        })
        .on('error', (err) => {
          logger.error('FFmpeg CPU conversion error:', err);
          if (fs.existsSync(tempOutput)) {
            fs.unlinkSync(tempOutput);
          }
          this.conversionQueue.delete(inputPath);
          reject(err);
        })
        .run();
    });
  }

  /**
   * Check if a file is currently being converted
   */
  isConverting(filePath: string): boolean {
    return this.conversionQueue.has(filePath);
  }

  /**
   * Get list of files currently being converted
   */
  getConversionQueue(): string[] {
    return Array.from(this.conversionQueue.keys());
  }
}

export const mediaConverterService = new MediaConverterService();
