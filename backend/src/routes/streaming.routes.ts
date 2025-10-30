import { Router, Request, Response, NextFunction } from 'express';
import { Content, WatchHistory, SeriesEpisode, Profile, Settings } from '../models';
import { validatePathParam, validateBody } from '../middleware/validation';
import { ApiError } from '../middleware/error-handler';
import fs from 'fs';
import path from 'path';
import logger from '../utils/logger';
import { probeMedia } from '../utils/ffmpeg';
import { mediaConverterService } from '../services/media-converter.service';

const router = Router();

/**
 * OPTIONS /api/stream/:id
 * Handle CORS preflight requests
 */
router.options('/:id', (req: Request, res: Response) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, HEAD, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Range, Content-Type');
  res.setHeader('Access-Control-Max-Age', '86400');
  res.status(204).end();
});

/**
 * HEAD /api/stream/:id
 * Get file metadata without downloading content
 */
router.head('/:id', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = parseInt(req.params.id, 10);
    const episodeId = req.query.episodeId ? parseInt(req.query.episodeId as string) : undefined;

    let filePath: string | undefined;

    if (episodeId) {
      const episode = await SeriesEpisode.findByPk(episodeId);
      if (!episode || !episode.filePath) {
        return res.status(404).end();
      }
      filePath = episode.filePath;
    } else {
      const content = await Content.findByPk(id);
      if (!content || !content.filePath) {
        return res.status(404).end();
      }
      filePath = content.filePath;
    }

    if (!fs.existsSync(filePath)) {
      return res.status(404).end();
    }

    const stat = fs.statSync(filePath);
    const ext = path.extname(filePath).toLowerCase();
    const contentTypeMap: { [key: string]: string } = {
      '.mp4': 'video/mp4',
      '.mkv': 'video/x-matroska',
      '.webm': 'video/webm',
      '.avi': 'video/x-msvideo',
      '.mov': 'video/quicktime',
      '.m4v': 'video/x-m4v',
      '.ts': 'video/mp2t'
    };
    const contentType = contentTypeMap[ext] || 'video/mp4';

    res.writeHead(200, {
      'Content-Length': stat.size,
      'Content-Type': contentType,
      'Accept-Ranges': 'bytes',
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Expose-Headers': 'Content-Length, Accept-Ranges',
      'Cache-Control': 'public, max-age=3600'
    });
    res.end();
  } catch (error) {
    next(error);
  }
});

/**
 * GET /api/stream/:id
 * Stream media file with HTTP range request support
 * Direct play only - no transcoding
 */
router.get('/:id', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = parseInt(req.params.id, 10);
    const episodeId = req.query.episodeId ? parseInt(req.query.episodeId as string) : undefined;
    const forceTranscode = req.query.transcode === 'true';
    const profileId = req.query.profileId ? parseInt(req.query.profileId as string) : undefined;

    let filePath: string | undefined;

    if (episodeId) {
      // Streaming an episode
      const episode = await SeriesEpisode.findByPk(episodeId);
      if (!episode || !episode.filePath) {
        const error: ApiError = new Error('Episode not found or file path not available');
        error.statusCode = 404;
        error.code = 'NOT_FOUND';
        return next(error);
      }
      filePath = episode.filePath;
    } else {
      // Streaming a movie
      const content = await Content.findByPk(id);
      if (!content || !content.filePath) {
        const error: ApiError = new Error('Content not found or file path not available');
        error.statusCode = 404;
        error.code = 'NOT_FOUND';
        return next(error);
      }
      filePath = content.filePath;
    }

    // Check if file exists
    if (!fs.existsSync(filePath)) {
      const error: ApiError = new Error('Media file not found on disk');
      error.statusCode = 404;
      error.code = 'FILE_NOT_FOUND';
      return next(error);
    }

    const stat = fs.statSync(filePath);
    const fileSize = stat.size;

    // Validate file size
    if (fileSize === 0) {
      const error: ApiError = new Error('Media file is empty');
      error.statusCode = 500;
      error.code = 'EMPTY_FILE';
      return next(error);
    }

    // Get user streaming settings
    let streamingSettings = {
      transcodingMode: 'direct-play', // 'direct-play', 'streaming', 'offline'
      audioTranscoding: true,
      videoTranscoding: true,
      preset: 'p4',
      useHardwareAccel: true
    };

    if (profileId) {
      try {
        const settingKey = `streamingPreferences_${profileId}`;
        const settings = await Settings.findOne({ where: { key: settingKey } });
        if (settings && settings.value) {
          const prefs = JSON.parse(settings.value);
          streamingSettings = {
            transcodingMode: prefs.transcodingMode || 'direct-play',
            audioTranscoding: prefs.audioTranscoding !== false,
            videoTranscoding: prefs.videoTranscoding !== false,
            preset: prefs.preset || 'p4',
            useHardwareAccel: prefs.useHardwareAccel !== false
          };
        }
      } catch (error) {
        logger.warn('Failed to load streaming settings, using defaults:', error);
      }
    }

    // Check if transcoding is needed
    const compatCheck = await mediaConverterService.checkCompatibility(filePath);
    
    // Handle seeking via query parameter (better for transcoding)
    const seekTime = req.query.start ? parseFloat(req.query.start as string) : 0;
    const range = req.headers.range;

    // Determine if we should transcode based on mode
    let shouldTranscode = false;
    
    if (forceTranscode) {
      shouldTranscode = true;
    } else if (streamingSettings.transcodingMode === 'streaming') {
      // Streaming mode: transcode incompatible files in real-time
      shouldTranscode = compatCheck.needsTranscode;
    } else if (streamingSettings.transcodingMode === 'offline') {
      // Offline mode: files should be pre-transcoded, don't transcode on-the-fly
      // This would be handled by a background job (not implemented here)
      shouldTranscode = false;
    } else {
      // Direct play mode: never transcode
      shouldTranscode = false;
    }

    // Decide which streams to transcode based on user settings
    let transcodeAudio = shouldTranscode && compatCheck.transcodeAudio && streamingSettings.audioTranscoding;
    let transcodeVideo = shouldTranscode && compatCheck.transcodeVideo && streamingSettings.videoTranscoding;

    if (shouldTranscode && (transcodeAudio || transcodeVideo)) {
      // TRANSCODE MODE: Stream with on-the-fly transcoding (Jellyfin-style)
      logger.info(`Real-time transcoding for content ${id}`, {
        audio: transcodeAudio,
        video: transcodeVideo,
        seekTime,
        hwAccel: streamingSettings.useHardwareAccel
      });

      // Try GPU transcoding first
      let transcodeStream;
      try {
        if (streamingSettings.useHardwareAccel) {
          transcodeStream = mediaConverterService.createTranscodeStream(filePath, {
            transcodeAudio,
            transcodeVideo,
            startTime: seekTime > 0 ? seekTime : undefined
          });
        } else {
          transcodeStream = mediaConverterService.createCPUTranscodeStream(filePath, {
            transcodeAudio,
            transcodeVideo,
            startTime: seekTime > 0 ? seekTime : undefined
          });
        }
      } catch (error) {
        logger.error('Failed to create transcode stream:', error);
        const apiError: ApiError = new Error('Transcoding failed');
        apiError.statusCode = 500;
        apiError.code = 'TRANSCODE_ERROR';
        return next(apiError);
      }

      // Set response headers for transcoded stream
      res.writeHead(200, {
        'Content-Type': 'video/mp4',
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Headers': 'Range',
        'Cache-Control': 'no-cache',
        'Connection': 'keep-alive',
        'X-Transcode-Mode': transcodeAudio && transcodeVideo ? 'both' : (transcodeAudio ? 'audio' : 'video'),
        'X-Hardware-Accel': streamingSettings.useHardwareAccel ? 'true' : 'false'
      });

      // Pipe transcoded stream to response
      transcodeStream.pipe(res);

      // Handle client disconnect
      req.on('close', () => {
        logger.info('Client disconnected, destroying transcode stream');
        transcodeStream.destroy();
      });

      // Handle stream errors
      transcodeStream.on('error', (err) => {
        logger.error('Transcode stream error:', err);
        if (!res.headersSent) {
          res.status(500).end();
        } else {
          res.end();
        }
      });

    } else {
      // DIRECT PLAY MODE: Serve file as-is
      logger.info(`Direct play for content ${id}`);

      const ext = path.extname(filePath).toLowerCase();
      const contentTypeMap: { [key: string]: string } = {
        '.mp4': 'video/mp4',
        '.m4v': 'video/mp4',
        '.mkv': 'video/x-matroska',
        '.webm': 'video/webm',
        '.avi': 'video/x-msvideo',
        '.mov': 'video/quicktime',
        '.ts': 'video/mp2t',
        '.m3u8': 'application/vnd.apple.mpegurl',
        '.mpd': 'application/dash+xml'
      };
      const contentType = contentTypeMap[ext] || 'video/mp4';

      if (range) {
        // Handle range request for seeking
        const parts = range.replace(/bytes=/, '').split('-');
        const start = parseInt(parts[0], 10);
        const end = parts[1] && parts[1].length > 0 ? parseInt(parts[1], 10) : fileSize - 1;

        // Validate range values
        if (isNaN(start) || start < 0 || start >= fileSize) {
          const error: ApiError = new Error('Invalid range start');
          error.statusCode = 416;
          error.code = 'INVALID_RANGE';
          return next(error);
        }

        if (isNaN(end) || end < start || end >= fileSize) {
          const error: ApiError = new Error('Invalid range end');
          error.statusCode = 416;
          error.code = 'INVALID_RANGE';
          return next(error);
        }

        const chunkSize = (end - start) + 1;
        const fileStream = fs.createReadStream(filePath, { start, end });

        res.writeHead(206, {
          'Content-Range': `bytes ${start}-${end}/${fileSize}`,
          'Accept-Ranges': 'bytes',
          'Content-Length': chunkSize,
          'Content-Type': contentType,
          'Access-Control-Allow-Origin': '*',
          'Access-Control-Allow-Headers': 'Range',
          'Access-Control-Expose-Headers': 'Content-Length, Content-Range, Accept-Ranges',
          'Cache-Control': 'public, max-age=3600',
          'X-Direct-Play': 'true'
        });

        fileStream.pipe(res);
      } else {
        // Stream entire file
        res.writeHead(200, {
          'Content-Length': fileSize,
          'Content-Type': contentType,
          'Accept-Ranges': 'bytes',
          'Access-Control-Allow-Origin': '*',
          'Access-Control-Allow-Headers': 'Range',
          'Access-Control-Expose-Headers': 'Content-Length, Content-Range, Accept-Ranges',
          'Cache-Control': 'public, max-age=3600',
          'X-Direct-Play': 'true'
        });

        const fileStream = fs.createReadStream(filePath);
        fileStream.pipe(res);
      }
    }
  } catch (error) {
    next(error);
  }
});

/**
 * POST /api/stream/:id/progress
 * Update watch progress
 */
router.post(
  '/:id/progress',
  validatePathParam('id'),
  validateBody(['profileId', 'progressSeconds']),
  async (req: Request, res: Response, next: NextFunction) => {
    try {
      const contentId = parseInt(req.params.id, 10);
      const { profileId, progressSeconds, durationSeconds, episodeId } = req.body;

      // Verify profile exists
      const profile = await Profile.findByPk(profileId);
      if (!profile) {
        const error: ApiError = new Error('Profile not found');
        error.statusCode = 404;
        error.code = 'NOT_FOUND';
        return next(error);
      }

      // Verify content exists
      const content = await Content.findByPk(contentId);
      if (!content) {
        const error: ApiError = new Error('Content not found');
        error.statusCode = 404;
        error.code = 'NOT_FOUND';
        return next(error);
      }

      // If episodeId provided, verify it exists
      if (episodeId) {
        const episode = await SeriesEpisode.findByPk(episodeId);
        if (!episode) {
          const error: ApiError = new Error('Episode not found');
          error.statusCode = 404;
          error.code = 'NOT_FOUND';
          return next(error);
        }
      }

      // Calculate if completed (watched more than 90%)
      const completed = durationSeconds ? (progressSeconds / durationSeconds) >= 0.9 : false;

      // Find existing watch history or create new
      const whereClause: any = {
        profileId,
        contentId
      };

      if (episodeId) {
        whereClause.episodeId = episodeId;
      }

      let watchHistory = await WatchHistory.findOne({ where: whereClause });

      if (watchHistory) {
        // Update existing
        watchHistory.progressSeconds = progressSeconds;
        if (durationSeconds) watchHistory.durationSeconds = durationSeconds;
        watchHistory.completed = completed;
        watchHistory.lastWatchedAt = new Date();
        await watchHistory.save();
      } else {
        // Create new
        watchHistory = await WatchHistory.create({
          profileId,
          contentId,
          episodeId: episodeId || undefined,
          progressSeconds,
          durationSeconds: durationSeconds || undefined,
          completed,
          lastWatchedAt: new Date()
        });
      }

      res.json({
        message: 'Watch progress updated',
        watchHistory: {
          id: watchHistory.id,
          progressSeconds: watchHistory.progressSeconds,
          durationSeconds: watchHistory.durationSeconds,
          completed: watchHistory.completed,
          lastWatchedAt: watchHistory.lastWatchedAt
        }
      });
    } catch (error) {
      next(error);
    }
  }
);

/**
 * GET /api/stream/:id/info
 * Get media file information (codecs, streams, etc.)
 */
router.get('/:id/info', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = parseInt(req.params.id, 10);
    const episodeId = req.query.episodeId ? parseInt(req.query.episodeId as string) : undefined;

    let filePath: string | undefined;

    if (episodeId) {
      const episode = await SeriesEpisode.findByPk(episodeId);
      if (!episode || !episode.filePath) {
        const error: ApiError = new Error('Episode not found or file path not available');
        error.statusCode = 404;
        error.code = 'NOT_FOUND';
        return next(error);
      }
      filePath = episode.filePath;
    } else {
      const content = await Content.findByPk(id);
      if (!content || !content.filePath) {
        const error: ApiError = new Error('Content not found or file path not available');
        error.statusCode = 404;
        error.code = 'NOT_FOUND';
        return next(error);
      }
      filePath = content.filePath;
    }

    if (!fs.existsSync(filePath)) {
      const error: ApiError = new Error('Media file not found on disk');
      error.statusCode = 404;
      error.code = 'FILE_NOT_FOUND';
      return next(error);
    }

    const mediaInfo = await probeMedia(filePath);

    res.json({
      contentId: id,
      episodeId: episodeId || null,
      filePath,
      mediaInfo
    });
  } catch (error) {
    next(error);
  }
});

/**
 * GET /api/stream/:id/subtitles
 * List available subtitles for content
 */
router.get('/:id/subtitles', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = parseInt(req.params.id, 10);
    const episodeId = req.query.episodeId ? parseInt(req.query.episodeId as string) : undefined;

    let filePath: string | undefined;

    if (episodeId) {
      const episode = await SeriesEpisode.findByPk(episodeId);
      if (!episode || !episode.filePath) {
        const error: ApiError = new Error('Episode not found or file path not available');
        error.statusCode = 404;
        error.code = 'NOT_FOUND';
        return next(error);
      }
      filePath = episode.filePath;
    } else {
      const content = await Content.findByPk(id);
      if (!content || !content.filePath) {
        const error: ApiError = new Error('Content not found or file path not available');
        error.statusCode = 404;
        error.code = 'NOT_FOUND';
        return next(error);
      }
      filePath = content.filePath;
    }

    // Look for subtitle files in the same directory
    const dir = path.dirname(filePath);
    const baseName = path.basename(filePath, path.extname(filePath));

    const subtitles: Array<{ language: string; path: string; format: string }> = [];

    if (fs.existsSync(dir)) {
      const files = fs.readdirSync(dir);

      // Look for subtitle files matching the video file name
      const subtitleExtensions = ['.srt', '.vtt', '.ass', '.ssa'];

      files.forEach(file => {
        const ext = path.extname(file).toLowerCase();
        if (subtitleExtensions.includes(ext) && file.startsWith(baseName)) {
          // Extract language code from filename (e.g., movie.en.srt -> en)
          const match = file.match(/\.([a-z]{2})\.(?:srt|vtt|ass|ssa)$/i);
          const language = match ? match[1] : 'unknown';

          subtitles.push({
            language,
            path: `/api/stream/${id}/subtitle/${file}`,
            format: ext.substring(1)
          });
        }
      });
    }

    res.json({
      contentId: id,
      episodeId: episodeId || null,
      count: subtitles.length,
      subtitles
    });
  } catch (error) {
    next(error);
  }
});

export default router;
