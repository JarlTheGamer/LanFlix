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
    const startTime = req.query.start ? parseFloat(req.query.start as string) : undefined;
    const profileId = req.query.profileId ? parseInt(req.query.profileId as string) : undefined;

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

    // Load user's transcoding preferences
    let audioTranscodingEnabled = true;
    let videoTranscodingEnabled = true;

    if (profileId) {
      try {
        const settingKey = `streamingPreferences_${profileId}`;
        const setting = await Settings.findOne({ where: { key: settingKey } });

        if (setting) {
          const prefs = JSON.parse(setting.value);
          audioTranscodingEnabled = prefs.audioTranscoding !== false;
          videoTranscodingEnabled = prefs.videoTranscoding !== false;
        }
      } catch (error) {
        logger.warn('Failed to load transcoding preferences for HEAD request:', error);
      }
    }

    const compatCheck = await mediaConverterService.checkCompatibility(filePath);
    const shouldTranscodeAudio = compatCheck.transcodeAudio && audioTranscodingEnabled;
    const shouldTranscodeVideo = compatCheck.transcodeVideo && videoTranscodingEnabled;

    if (shouldTranscodeAudio || shouldTranscodeVideo) {
      const transcodeMode = shouldTranscodeVideo ? 'video+audio' : 'audio-only';
      const { sessionId } = mediaConverterService.createHlsSession(filePath, {
        transcodeAudio: shouldTranscodeAudio,
        transcodeVideo: shouldTranscodeVideo,
        startTime,
        mediaInfo: compatCheck.mediaInfo
      });

      res.writeHead(200, {
        'Content-Type': 'application/vnd.apple.mpegurl',
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Headers': 'Range',
        'Access-Control-Expose-Headers': 'Content-Type, X-Stream-Type, X-Transcode-Mode, X-Transcode-Session, X-Direct-Play',
        'Cache-Control': 'no-cache',
        'X-Stream-Type': 'hls',
        'X-Transcode-Mode': transcodeMode,
        'X-Transcode-Session': sessionId,
        'X-Direct-Play': 'false'
      });
      res.end();
      return;
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
      'Access-Control-Expose-Headers': 'Content-Length, Accept-Ranges, X-Stream-Type, X-Transcode-Mode, X-Direct-Play',
      'Cache-Control': 'public, max-age=3600',
      'X-Stream-Type': 'file',
      'X-Transcode-Mode': 'direct-play',
      'X-Direct-Play': 'true'
    });
    res.end();
  } catch (error) {
    next(error);
  }
});

/**
 * GET /api/stream/:id
 * Stream media file with HTTP range request support
 * Supports both direct play and transcoding with seeking
 */
router.get('/:id', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = parseInt(req.params.id, 10);
    const episodeId = req.query.episodeId ? parseInt(req.query.episodeId as string) : undefined;
    const startTime = req.query.start ? parseFloat(req.query.start as string) : undefined;
    const profileId = req.query.profileId ? parseInt(req.query.profileId as string) : undefined;
    const sessionId = typeof req.query.session === 'string' ? req.query.session : undefined;
    const segmentName = typeof req.query.segment === 'string' ? req.query.segment : undefined;

    // Serve existing HLS session requests (manifest or segments)
    if (sessionId) {
      try {
        if (segmentName) {
          const segmentStream = await mediaConverterService.getHlsSegmentStream(sessionId, segmentName);
          res.writeHead(200, {
            'Content-Type': 'video/mp2t',
            'Cache-Control': 'no-cache',
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Expose-Headers': 'X-Stream-Type, X-Transcode-Session',
            'X-Stream-Type': 'hls',
            'X-Transcode-Session': sessionId
          });

          segmentStream.pipe(res);
          segmentStream.on('error', (err) => {
            logger.error(`HLS segment stream error for session ${sessionId}:`, err);
            if (!res.headersSent) {
              res.status(500).end();
            } else {
              res.end();
            }
          });
          return;
        }

        const manifest = await mediaConverterService.getHlsManifest(sessionId);
        const manifestMeta = mediaConverterService.getSessionMetadata(sessionId);
        const lines = manifest.split(/\r?\n/).map((line) => {
          const trimmed = line.trim();
          if (!trimmed || trimmed.startsWith('#')) {
            return line;
          }
          const encodedSegment = encodeURIComponent(trimmed);
          return `${req.baseUrl}/${id}?session=${sessionId}&segment=${encodedSegment}`;
        });

        res.writeHead(200, {
          'Content-Type': 'application/vnd.apple.mpegurl',
          'Cache-Control': 'no-cache',
          'Access-Control-Allow-Origin': '*',
          'Access-Control-Allow-Headers': 'Range',
          'Access-Control-Expose-Headers': 'Content-Type, X-Stream-Type, X-Transcode-Mode, X-Transcode-Session, X-Direct-Play, X-Start-Offset',
          'X-Stream-Type': 'hls',
          'X-Transcode-Mode': manifestMeta.transcodeVideo ? 'video+audio' : 'audio-only',
          'X-Transcode-Session': sessionId,
          'X-Direct-Play': 'false',
          'X-Start-Offset': manifestMeta.startTime.toString()
        });

        res.end(lines.join('\n'));
        return;
      } catch (error) {
        logger.error(`Failed to serve HLS session ${sessionId}:`, error);
        if (!res.headersSent) {
          res.status(404).end();
        } else {
          res.end();
        }
        return;
      }
    }

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

    // Load user's transcoding preferences
    let audioTranscodingEnabled = true;
    let videoTranscodingEnabled = true;

    if (profileId) {
      try {
        const settingKey = `streamingPreferences_${profileId}`;
        const setting = await Settings.findOne({ where: { key: settingKey } });

        if (setting) {
          const prefs = JSON.parse(setting.value);
          audioTranscodingEnabled = prefs.audioTranscoding !== false;
          videoTranscodingEnabled = prefs.videoTranscoding !== false;
          logger.info(`Profile ${profileId} transcoding preferences: audio=${audioTranscodingEnabled}, video=${videoTranscodingEnabled}`);
        }
      } catch (error) {
        logger.warn('Failed to load transcoding preferences, using defaults:', error);
      }
    }

    // Check if audio/video needs transcoding
    const compatCheck = await mediaConverterService.checkCompatibility(filePath);
    const range = req.headers.range;

    // Determine if we should transcode based on compatibility AND user preferences
    const shouldTranscodeAudio = compatCheck.transcodeAudio && audioTranscodingEnabled;
    const shouldTranscodeVideo = compatCheck.transcodeVideo && videoTranscodingEnabled;

    if (shouldTranscodeAudio || shouldTranscodeVideo) {
      const transcodeMode = shouldTranscodeVideo ? 'video+audio' : 'audio-only';
      logger.info(`Transcoding ${transcodeMode} for content ${id} (audio codec: ${compatCheck.mediaInfo.audioCodec}, video codec: ${compatCheck.mediaInfo.videoCodec})`);

      const { sessionId: newSessionId } = mediaConverterService.createHlsSession(filePath, {
        transcodeAudio: shouldTranscodeAudio,
        transcodeVideo: shouldTranscodeVideo,
        startTime,
        mediaInfo: compatCheck.mediaInfo
      });

      try {
        const manifest = await mediaConverterService.getHlsManifest(newSessionId);
        const lines = manifest.split(/\r?\n/).map((line) => {
          const trimmed = line.trim();
          if (!trimmed || trimmed.startsWith('#')) {
            return line;
          }
          const encodedSegment = encodeURIComponent(trimmed);
          return `${req.baseUrl}/${id}?session=${newSessionId}&segment=${encodedSegment}`;
        });

        res.writeHead(200, {
          'Content-Type': 'application/vnd.apple.mpegurl',
          'Cache-Control': 'no-cache',
          'Access-Control-Allow-Origin': '*',
          'Access-Control-Allow-Headers': 'Range',
          'Access-Control-Expose-Headers': 'Content-Type, X-Stream-Type, X-Transcode-Mode, X-Transcode-Session, X-Direct-Play, X-Start-Offset',
          'X-Stream-Type': 'hls',
          'X-Transcode-Mode': transcodeMode,
          'X-Transcode-Session': newSessionId,
          'X-Direct-Play': 'false',
          'X-Start-Offset': (startTime || 0).toString()
        });

        res.end(lines.join('\n'));
      } catch (error) {
        logger.error('Failed to prepare HLS manifest:', error);
        mediaConverterService.endSession(newSessionId);

        const apiError: ApiError = new Error('Failed to prepare transcoding session');
        apiError.statusCode = 500;
        apiError.code = 'TRANSCODE_FAILED';
        return next(apiError);
      }

      return;
    }

    // Direct play if codecs are compatible
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
        'Access-Control-Expose-Headers': 'Content-Length, Content-Range, Accept-Ranges, X-Stream-Type, X-Transcode-Mode, X-Direct-Play',
        'Cache-Control': 'public, max-age=3600',
        'X-Stream-Type': 'file',
        'X-Transcode-Mode': 'direct-play',
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
        'Access-Control-Expose-Headers': 'Content-Length, Content-Range, Accept-Ranges, X-Stream-Type, X-Transcode-Mode, X-Direct-Play',
        'Cache-Control': 'public, max-age=3600',
        'X-Stream-Type': 'file',
        'X-Transcode-Mode': 'direct-play',
        'X-Direct-Play': 'true'
      });

      const fileStream = fs.createReadStream(filePath);
      fileStream.pipe(res);
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
