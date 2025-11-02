import { Router, Request, Response, NextFunction } from 'express';
import { Content, SeriesEpisode } from '../models';
import { ApiError } from '../middleware/error-handler';
import fs from 'fs';
import logger from '../utils/logger';
import { mediaConverterService } from '../services/offline-transcoder.service';
import path from 'path';


const router = Router();

/**
 * POST /api/transcode/offline
 * Start offline transcoding job (creates new file, replaces original when done)
 */
router.post('/offline', async (req: Request, res: Response, next: NextFunction) => {
    try {
        const { contentId, type, transcodeVideo, transcodeAudio, useHardware } = req.body;

        if (!contentId || !type) {
            const error: ApiError = new Error('contentId and type are required');
            error.statusCode = 400;
            error.code = 'INVALID_REQUEST';
            return next(error);
        }

        let filePath: string | undefined;
        let contentTitle: string;

        if (type === 'movie') {
            const content = await Content.findByPk(contentId);
            if (!content || !content.filePath) {
                const error: ApiError = new Error('Content not found or file path not available');
                error.statusCode = 404;
                error.code = 'NOT_FOUND';
                return next(error);
            }
            filePath = content.filePath;
            contentTitle = content.title;
        } else if (type === 'episode') {
            const episode = await SeriesEpisode.findByPk(contentId);
            if (!episode || !episode.filePath) {
                const error: ApiError = new Error('Episode not found or file path not available');
                error.statusCode = 404;
                error.code = 'NOT_FOUND';
                return next(error);
            }
            filePath = episode.filePath;
            contentTitle = `Episode ${episode.episodeNumber}`;
        } else {
            const error: ApiError = new Error('Invalid type. Must be "movie" or "episode"');
            error.statusCode = 400;
            error.code = 'INVALID_TYPE';
            return next(error);
        }

        if (!fs.existsSync(filePath)) {
            const error: ApiError = new Error('Media file not found on disk');
            error.statusCode = 404;
            error.code = 'FILE_NOT_FOUND';
            return next(error);
        }

        logger.info(`🎬 Starting offline transcode: ${filePath}`);

        // Start transcoding in background - service handles all settings
        startOfflineTranscode(filePath, contentId, type).catch(error => {
            logger.error('❌ Background transcode failed:', error);
        });

        res.json({
            message: 'Transcoding started',
            contentId,
            type,
            status: 'processing',
            note: 'Original file will be backed up with .original extension. Transcoded file will replace the original when complete.'
        });
    } catch (error) {
        next(error);
    }
});

/**
 * Start offline transcoding process
 */
async function startOfflineTranscode(inputPath: string, contentId: number, type: string) {
    try {
        logger.info('🎬 Starting offline transcode');

        // Use offlineTranscode - always transcodes, no probing
        const transcodedPath = await mediaConverterService.offlineTranscode(inputPath);

        logger.info(`✅ Offline transcode completed: ${transcodedPath}`);

        // Update database
        if (type === 'movie') {
            const content = await Content.findByPk(contentId);
            if (content) {
                const stats = fs.statSync(transcodedPath);
                await content.save();
                logger.info(`✅ Updated movie ${contentId} (${(stats.size / 1024 / 1024).toFixed(2)}MB)`);
            }
        } else if (type === 'episode') {
            const episode = await SeriesEpisode.findByPk(contentId);
            if (episode) {
                const stats = fs.statSync(transcodedPath);
                await episode.save();
                logger.info(`✅ Updated episode ${contentId} (${(stats.size / 1024 / 1024).toFixed(2)}MB)`);
            }
        }

        logger.info('✅ Database updated');
    } catch (error) {
        logger.error('❌ Offline transcode failed:', error);
        throw error;
    }
}

export default router;
