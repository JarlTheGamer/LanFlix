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
        logger.info(`File already being converted: ${filePath}`);
        return filePath;
      }

      // Check if file exists
      if (!fs.existsSync(filePath)) {
        throw new Error(`File not found: ${filePath}`);
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

      logger.info(`Starting conversion: ${inputPath} -> ${finalOutput}`, options);

      let command = ffmpeg(inputPath)
        .outputFormat('mp4')
        .addOutputOption('-movflags', '+faststart'); // Optimize for streaming

      // Video handling
      if (options.transcodeVideo) {
        logger.info('Transcoding video to H.264 (high quality)');
        command = command
          .videoCodec('libx264')
          .addOutputOption('-preset', 'slow')      // Better compression
          .addOutputOption('-crf', '18')           // Near-lossless quality (18 = visually lossless)
          .addOutputOption('-profile:v', 'high')   // H.264 High Profile
          .addOutputOption('-level', '4.1')        // Wide compatibility
          .addOutputOption('-pix_fmt', 'yuv420p'); // Maximum compatibility
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
            logger.info(`Conversion progress: ${progress.percent.toFixed(1)}%`);
          }
        })
        .on('end', () => {
          logger.info('Conversion completed successfully');

          try {
            // Verify the converted file exists and has content
            const stats = fs.statSync(tempOutput);
            if (stats.size === 0) {
              throw new Error('Converted file is empty');
            }

            // Delete original file
            logger.info(`Deleting original file: ${inputPath}`);
            fs.unlinkSync(inputPath);

            // Rename temp file to final name
            logger.info(`Renaming ${tempOutput} -> ${finalOutput}`);
            fs.renameSync(tempOutput, finalOutput);

            // Remove from queue
            this.conversionQueue.delete(inputPath);

            logger.info(`File successfully converted and replaced: ${finalOutput}`);
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
          
          // Clean up temp file if it exists
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
