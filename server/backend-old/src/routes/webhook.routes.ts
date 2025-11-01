import { Router, Request, Response } from 'express';
import logger from '../utils/logger';
import { LibraryService } from '../services/library.service';
import { mediaConverterService } from '../services/media-converter.service';

const router = Router();
const libraryService = new LibraryService();

/**
 * POST /api/webhook/radarr
 * Webhook endpoint for Radarr to notify when a movie download completes
 */
router.post('/radarr', async (req: Request, res: Response) => {
    try {
        const { eventType, movie, movieFile } = req.body;

        logger.info('Radarr webhook received', {
            eventType,
            movieTitle: movie?.title,
            hasMovieFile: !!movieFile,
            movieFilePath: movieFile?.path
        });

        // Log full payload for debugging
        logger.debug('Full Radarr webhook payload:', JSON.stringify(req.body, null, 2));

        // Process on download completion
        if (eventType === 'Download') {
            if (!movieFile?.path) {
                logger.warn('Download event received but no movieFile.path found');
                return res.status(200).json({ message: 'Webhook received but no file path' });
            }

            logger.info(`✅ Movie download completed: ${movie?.title || 'Unknown'}`, {
                filePath: movieFile.path,
                quality: movieFile.quality?.quality?.name
            });

            // Trigger library scan and conversion immediately (no setTimeout)
            // Run in background to not block webhook response
            setImmediate(async () => {
                try {
                    logger.info('🔄 Triggering library scan after Radarr download...');
                    const scanResult = await libraryService.scanLibraryFolder();
                    logger.info('Library scan completed', scanResult);

                    // TODO: Auto-convert the file if needed (offline transcoding not yet implemented)
                    // if (movieFile.path) {
                    //     logger.info(`🎬 Auto-converting movie: ${movieFile.path}`);
                    //     const convertedPath = await mediaConverterService.ensureCompatible(movieFile.path);
                    //     logger.info(`✅ Conversion completed: ${convertedPath}`);
                    // }
                } catch (error) {
                    logger.error('❌ Error processing Radarr webhook:', error);
                }
            });

            res.status(200).json({ message: 'Webhook received and processing started', eventType });
        } else {
            logger.info(`Ignoring Radarr event type: ${eventType}`);
            res.status(200).json({ message: 'Webhook received but event type not processed', eventType });
        }
    } catch (error) {
        logger.error('Error handling Radarr webhook:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});

/**
 * POST /api/webhook/sonarr
 * Webhook endpoint for Sonarr to notify when an episode download completes
 */
router.post('/sonarr', async (req: Request, res: Response) => {
    try {
        const { eventType, series, episodes, episodeFile } = req.body;

        logger.info('Sonarr webhook received', {
            eventType,
            seriesTitle: series?.title,
            hasEpisodeFile: !!episodeFile,
            episodeFilePath: episodeFile?.path
        });

        // Log full payload for debugging
        logger.debug('Full Sonarr webhook payload:', JSON.stringify(req.body, null, 2));

        // Process on download/import events
        if (eventType === 'Download' || eventType === 'Rename') {
            const episodeInfo = episodes?.[0];
            
            if (episodeFile?.path) {
                logger.info(`✅ Episode ${eventType.toLowerCase()} completed: ${series?.title || 'Unknown'} S${episodeInfo?.seasonNumber}E${episodeInfo?.episodeNumber}`, {
                    filePath: episodeFile.path,
                    quality: episodeFile.quality?.quality?.name
                });
            } else {
                logger.warn(`${eventType} event received but no episodeFile.path - will scan library anyway`);
            }

            // Trigger library scan and conversion immediately
            // Run in background to not block webhook response
            setImmediate(async () => {
                try {
                    logger.info('🔄 Triggering library scan after Sonarr event...');
                    const scanResult = await libraryService.scanLibraryFolder();
                    logger.info('Library scan completed', scanResult);

                    // TODO: Auto-convert the file if we have a path (offline transcoding not yet implemented)
                    // if (episodeFile?.path) {
                    //     logger.info(`🎬 Auto-converting episode: ${episodeFile.path}`);
                    //     const convertedPath = await mediaConverterService.ensureCompatible(episodeFile.path);
                    //     logger.info(`✅ Conversion completed: ${convertedPath}`);
                    // } else {
                    //     logger.info('No specific file path, scan will find and convert new files');
                    // }
                } catch (error) {
                    logger.error('❌ Error processing Sonarr webhook:', error);
                }
            });

            res.status(200).json({ message: 'Webhook received and processing started', eventType });
        } else if (eventType === 'Test') {
            logger.info('✅ Sonarr webhook test successful');
            res.status(200).json({ message: 'Webhook test successful', eventType });
        } else {
            logger.info(`Ignoring Sonarr event type: ${eventType}`);
            res.status(200).json({ message: 'Webhook received but event type not processed', eventType });
        }
    } catch (error) {
        logger.error('Error handling Sonarr webhook:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});

/**
 * GET /api/webhook/test
 * Test endpoint to verify webhooks are working
 */
router.get('/test', (req: Request, res: Response) => {
    res.json({
        message: 'Webhook endpoint is working',
        endpoints: {
            radarr: '/api/webhook/radarr',
            sonarr: '/api/webhook/sonarr'
        }
    });
});

export default router;
