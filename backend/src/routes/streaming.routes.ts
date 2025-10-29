import { Router, Request, Response, NextFunction } from 'express';
import { Content, WatchHistory, SeriesEpisode, Profile } from '../models';
import { validatePathParam, validateBody } from '../middleware/validation';
import { ApiError } from '../middleware/error-handler';
import fs from 'fs';
import path from 'path';
import logger from '../utils/logger';

const router = Router();

/**
 * GET /api/stream/:id
 * Stream media file with HTTP range request support
 */
router.get('/:id', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = parseInt(req.params.id, 10);
    const episodeId = req.query.episodeId ? parseInt(req.query.episodeId as string) : undefined;

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
    const range = req.headers.range;

    if (range) {
      // Handle range request for seeking
      const parts = range.replace(/bytes=/, '').split('-');
      const start = parseInt(parts[0], 10);
      const end = parts[1] ? parseInt(parts[1], 10) : fileSize - 1;
      const chunkSize = (end - start) + 1;

      const fileStream = fs.createReadStream(filePath, { start, end });

      res.writeHead(206, {
        'Content-Range': `bytes ${start}-${end}/${fileSize}`,
        'Accept-Ranges': 'bytes',
        'Content-Length': chunkSize,
        'Content-Type': 'video/mp4'
      });

      fileStream.pipe(res);
    } else {
      // Stream entire file
      res.writeHead(200, {
        'Content-Length': fileSize,
        'Content-Type': 'video/mp4',
        'Accept-Ranges': 'bytes'
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
