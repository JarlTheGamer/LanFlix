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

    // Load user's transcoding preferences (if any)
    let transcodingMode = 'auto';
    let audioTranscodingEnabled = true;
    let videoTranscodingEnabled = true;

    if (profileId) {
      try {
        const settingKey = `streamingPreferences_${profileId}`;
        const setting = await Settings.findOne({ where: { key: settingKey } });

        if (setting) {
          const prefs = JSON.parse(setting.value);
          transcodingMode = prefs.transcodingMode || 'auto';
          audioTranscodingEnabled = prefs.audioTranscoding !== false;
          videoTranscodingEnabled = prefs.videoTranscoding !== false;
        }
      } catch (error) {
        logger.warn('Failed to load transcoding preferences for HEAD request, using defaults:', error);
      }
    }

    const compatCheck = await mediaConverterService.checkCompatibility(filePath);

    let actualPlaybackMode = compatCheck.playbackMode;

    if (transcodingMode !== 'auto') {
      if (transcodingMode === 'direct-play') {
        actualPlaybackMode = 'direct-play';
      } else if (transcodingMode === 'direct-stream') {
        actualPlaybackMode = 'direct-stream';
      } else if (transcodingMode === 'transcode') {
        actualPlaybackMode = 'transcode';
      }
    }

    let shouldTranscodeAudio = compatCheck.transcodeAudio && audioTranscodingEnabled;
    let shouldTranscodeVideo = compatCheck.transcodeVideo && videoTranscodingEnabled;

    if (actualPlaybackMode === 'direct-play') {
      shouldTranscodeAudio = false;
      shouldTranscodeVideo = false;
    } else if (actualPlaybackMode === 'remux') {
      shouldTranscodeAudio = false;
      shouldTranscodeVideo = false;
    } else if (actualPlaybackMode === 'direct-stream') {
      shouldTranscodeVideo = false;
      shouldTranscodeAudio = compatCheck.transcodeAudio;
    } else if (actualPlaybackMode === 'transcode') {
      shouldTranscodeAudio = compatCheck.transcodeAudio;
      shouldTranscodeVideo = compatCheck.transcodeVideo;
    }

    const exposeHeaders = new Set(['Content-Length', 'Accept-Ranges', 'X-Direct-Play']);
    const headers: Record<string, string | number> = {
      'Content-Length': stat.size,
      'Content-Type': contentType,
      'Accept-Ranges': 'bytes',
      'Access-Control-Allow-Origin': '*',
      'Cache-Control': 'public, max-age=3600'
    };

    if (actualPlaybackMode === 'remux' && !shouldTranscodeAudio && !shouldTranscodeVideo) {
      headers['X-Playback-Mode'] = 'remux';
      headers['X-Transcode-Mode'] = 'remux';
      headers['X-Direct-Play'] = 'false';
      exposeHeaders.add('X-Playback-Mode');
      exposeHeaders.add('X-Transcode-Mode');
    } else if (shouldTranscodeAudio || shouldTranscodeVideo) {
      const transcodeMode = shouldTranscodeVideo ? 'transcode' : 'direct-stream';
      headers['X-Playback-Mode'] = transcodeMode;
      headers['X-Transcode-Mode'] = transcodeMode;
      headers['X-Direct-Play'] = 'false';
      exposeHeaders.add('X-Playback-Mode');
      exposeHeaders.add('X-Transcode-Mode');
    } else {
      headers['X-Direct-Play'] = 'true';
    }

    headers['Access-Control-Expose-Headers'] = Array.from(exposeHeaders).join(', ');

    res.writeHead(200, headers);
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
    let transcodingMode = 'auto'; // auto, direct-play, direct-stream, transcode
    let audioTranscodingEnabled = true;
    let videoTranscodingEnabled = true;
    let useHardwareAccel = false;
    let transcodePreset = 'p4'; // Default preset

    if (profileId) {
      try {
        const settingKey = `streamingPreferences_${profileId}`;
        const setting = await Settings.findOne({ where: { key: settingKey } });

        if (setting) {
          const prefs = JSON.parse(setting.value);
          transcodingMode = prefs.transcodingMode || 'auto';
          audioTranscodingEnabled = prefs.audioTranscoding !== false;
          videoTranscodingEnabled = prefs.videoTranscoding !== false;
          // Check for hardware acceleration in profile preferences
          useHardwareAccel = prefs.useHardwareAccel !== false;
          transcodePreset = prefs.preset || 'p4';
          logger.info(`Profile ${profileId} transcoding preferences: mode=${transcodingMode}, audio=${audioTranscodingEnabled}, video=${videoTranscodingEnabled}, hwAccel=${useHardwareAccel}, preset=${transcodePreset}`);
        }
      } catch (error) {
        logger.warn('Failed to load transcoding preferences, using defaults:', error);
      }
    }

    // Check global hardware acceleration setting when no profile is specified
    if (!profileId) {
      try {
        const hwAccelSetting = await Settings.findOne({ where: { key: 'useHardwareAccel' } });
        if (hwAccelSetting) {
          useHardwareAccel = JSON.parse(hwAccelSetting.value) === true;
          logger.info(`Hardware acceleration (global): ${useHardwareAccel ? 'ENABLED (GPU)' : 'DISABLED (CPU)'}`);
        }
      } catch (error) {
        logger.warn('Failed to load hardware acceleration setting:', error);
      }
    }

    // Check compatibility to determine optimal playback mode
    const compatCheck = await mediaConverterService.checkCompatibility(filePath);
    const range = req.headers.range;

    // Determine actual playback mode based on user preference and compatibility
    let actualPlaybackMode = compatCheck.playbackMode;

    if (transcodingMode !== 'auto') {
      // User has forced a specific mode
      if (transcodingMode === 'direct-play') {
        actualPlaybackMode = 'direct-play';
      } else if (transcodingMode === 'direct-stream') {
        actualPlaybackMode = 'direct-stream';
      } else if (transcodingMode === 'transcode') {
        actualPlaybackMode = 'transcode';
      }
    }

    // Apply user's audio/video transcoding toggles
    let shouldTranscodeAudio = compatCheck.transcodeAudio && audioTranscodingEnabled;
    let shouldTranscodeVideo = compatCheck.transcodeVideo && videoTranscodingEnabled;

    // Override based on actual playback mode
    if (actualPlaybackMode === 'direct-play') {
      shouldTranscodeAudio = false;
      shouldTranscodeVideo = false;
    } else if (actualPlaybackMode === 'remux') {
      shouldTranscodeAudio = false;
      shouldTranscodeVideo = false;
    } else if (actualPlaybackMode === 'direct-stream') {
      shouldTranscodeVideo = false;
      shouldTranscodeAudio = compatCheck.transcodeAudio;
    } else if (actualPlaybackMode === 'transcode') {
      shouldTranscodeAudio = compatCheck.transcodeAudio;
      shouldTranscodeVideo = compatCheck.transcodeVideo;
    }

    logger.info(`Playback mode: ${actualPlaybackMode} (user pref: ${transcodingMode}, detected: ${compatCheck.playbackMode})`);
    logger.info(`Transcode flags: audio=${shouldTranscodeAudio}, video=${shouldTranscodeVideo}`);

    // Handle remux mode (container change only)
    if (actualPlaybackMode === 'remux' && !shouldTranscodeAudio && !shouldTranscodeVideo) {
      logger.info(`Remuxing for content ${id} (${compatCheck.reason})`);

      const remuxStream = mediaConverterService.createRemuxStream(filePath, {
        startTime: startTime
      });

      res.writeHead(200, {
        'Content-Type': 'video/mp4',
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Headers': 'Range',
        'Access-Control-Expose-Headers': 'Content-Type, X-Playback-Mode, X-Transcode-Mode, X-Direct-Play',
        'Cache-Control': 'no-cache',
        'Connection': 'keep-alive',
        'X-Playback-Mode': 'remux',
        'X-Transcode-Mode': 'remux',
        'X-Direct-Play': 'false'
      });

      remuxStream.pipe(res);

      req.on('close', () => {
        logger.info('Client disconnected, destroying remux stream');
        remuxStream.destroy();
      });

      remuxStream.on('error', (err) => {
        logger.error('Remux stream error:', err);
        if (!res.headersSent) {
          res.status(500).end();
        } else {
          res.end();
        }
      });

      return;
    }

    // Handle transcoding (direct-stream or full transcode)
    if (shouldTranscodeAudio || shouldTranscodeVideo) {
      const transcodeMode = shouldTranscodeVideo ? 'transcode' : 'direct-stream';
      logger.info(`${transcodeMode} for content ${id} (${compatCheck.reason}) - Using ${useHardwareAccel ? 'GPU (NVENC)' : 'CPU (libx264)'}`);

      // For transcoded streams with seeking, use startTime parameter
      // Use GPU or CPU transcoding based on hardware acceleration setting
      const transcodeStream = useHardwareAccel
        ? mediaConverterService.createTranscodeStream(filePath, {
            transcodeAudio: shouldTranscodeAudio,
            transcodeVideo: shouldTranscodeVideo,
            startTime: startTime,
            preset: transcodePreset
          })
        : mediaConverterService.createCPUTranscodeStream(filePath, {
            transcodeAudio: shouldTranscodeAudio,
            transcodeVideo: shouldTranscodeVideo,
            startTime: startTime,
            preset: transcodePreset
          });

      res.writeHead(200, {
        'Content-Type': 'video/mp4',
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Headers': 'Range',
        'Access-Control-Expose-Headers': 'Content-Type, X-Playback-Mode, X-Transcode-Mode, X-Direct-Play',
        'Cache-Control': 'no-cache',
        'Connection': 'keep-alive',
        'X-Playback-Mode': transcodeMode,
        'X-Transcode-Mode': transcodeMode,
        'X-Direct-Play': 'false'
      });

      transcodeStream.pipe(res);

      req.on('close', () => {
        logger.info('Client disconnected, destroying transcode stream');
        transcodeStream.destroy();
      });

      transcodeStream.on('error', (err) => {
        logger.error('Transcode stream error:', err);
        if (!res.headersSent) {
          res.status(500).end();
        } else {
          res.end();
        }
      });

      return;
    }

    // Direct play if audio is compatible
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
        'Access-Control-Expose-Headers': 'Content-Length, Content-Range, Accept-Ranges, X-Direct-Play',
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
        'Access-Control-Expose-Headers': 'Content-Length, Content-Range, Accept-Ranges, X-Direct-Play',
        'Cache-Control': 'public, max-age=3600',
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