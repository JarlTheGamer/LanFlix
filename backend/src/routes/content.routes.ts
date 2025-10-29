import { Router, Request, Response, NextFunction } from 'express';
import { contentService, downloadManager } from '../services';
import { validateQueryParam, validatePathParam, validateBody } from '../middleware/validation';
import { ApiError } from '../middleware/error-handler';

const router = Router();

/**
 * GET /api/content/discover
 * Get trending and popular content
 */
router.get('/discover', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const page = parseInt(req.query.page as string) || 1;
    const profileId = req.query.profileId ? parseInt(req.query.profileId as string) : undefined;

    // Get trending content (returns both movies and series)
    const trending = await contentService.getTrendingContent(profileId);

    // Get popular content
    const [popularMovies, popularSeries] = await Promise.all([
      contentService.getPopularContent('movie', page, profileId),
      contentService.getPopularContent('series', page, profileId)
    ]);

    res.json({
      trending,
      popular: {
        movies: popularMovies,
        series: popularSeries
      }
    });
  } catch (error) {
    next(error);
  }
});

/**
 * GET /api/content/search
 * Search for content
 */
router.get('/search', validateQueryParam('q', true), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const query = req.query.q as string;
    const type = (req.query.type as 'movie' | 'series' | 'all') || 'all';
    const profileId = req.query.profileId ? parseInt(req.query.profileId as string) : undefined;

    const results = await contentService.searchContent(query, type, profileId);

    res.json({
      query,
      type,
      results
    });
  } catch (error) {
    next(error);
  }
});

/**
 * GET /api/content/:id
 * Get detailed content information
 */
router.get('/:id', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = parseInt(req.params.id, 10);
    const type = req.query.type as 'movie' | 'series' | undefined;
    const profileId = req.query.profileId ? parseInt(req.query.profileId as string) : undefined;

    if (!type) {
      const error: ApiError = new Error('Query parameter "type" is required (movie or series)');
      error.statusCode = 400;
      error.code = 'VALIDATION_ERROR';
      return next(error);
    }

    const content = await contentService.getContentDetails(id, type, profileId);

    if (!content) {
      const error: ApiError = new Error('Content not found');
      error.statusCode = 404;
      error.code = 'NOT_FOUND';
      return next(error);
    }

    res.json(content);
  } catch (error) {
    next(error);
  }
});

/**
 * POST /api/content/:id/queue
 * Add content to download queue
 */
router.post(
  '/:id/queue',
  validatePathParam('id'),
  validateBody(['profileId', 'type', 'title']),
  async (req: Request, res: Response, next: NextFunction) => {
    try {
      const tmdbId = parseInt(req.params.id, 10);
      const { profileId, type, title, year } = req.body;

      if (!['movie', 'series'].includes(type)) {
        const error: ApiError = new Error('Type must be either "movie" or "series"');
        error.statusCode = 400;
        error.code = 'VALIDATION_ERROR';
        return next(error);
      }

      const queueItem = await downloadManager.queueDownload({
        tmdbId,
        type,
        title,
        year,
        profileId
      });

      res.status(201).json({
        message: 'Content added to download queue',
        queueItem
      });
    } catch (error) {
      next(error);
    }
  }
);

export default router;
