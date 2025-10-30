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
 * GET /api/content/popular
 * Get popular content
 */
router.get('/popular', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const type = (req.query.type as 'movie' | 'series') || 'movie';
    const page = parseInt(req.query.page as string) || 1;
    const profileId = req.query.profileId ? parseInt(req.query.profileId as string) : undefined;

    const results = await contentService.getPopularContent(type, page, profileId);

    res.json(results);
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
 * GET /api/content/:id/episodes
 * Get episodes for a TV series from LOCAL DATABASE ONLY
 * TMDB is only used for discovery, not for library content
 */
router.get('/:id/episodes', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const tmdbId = parseInt(req.params.id, 10);
    const seasonNumber = req.query.season ? parseInt(req.query.season as string) : null;

    // Import dependencies
    const Content = (await import('../models/Content')).default;
    const SeriesEpisode = (await import('../models/SeriesEpisode')).default;

    // Check if series exists in library
    const content = await Content.findOne({
      where: { tmdbId, type: 'series' }
    });

    if (!content) {
      const error: ApiError = new Error('Series not found in library');
      error.statusCode = 404;
      error.code = 'NOT_FOUND';
      return next(error);
    }

    // Get all episodes for this series
    const allEpisodes = await SeriesEpisode.findAll({
      where: { contentId: content.id },
      order: [['seasonNumber', 'ASC'], ['episodeNumber', 'ASC']]
    });

    if (allEpisodes.length === 0) {
      const error: ApiError = new Error('No episodes found for this series');
      error.statusCode = 404;
      error.code = 'NOT_FOUND';
      return next(error);
    }

    // If specific season requested, return only that season
    if (seasonNumber !== null) {
      const seasonEpisodes = allEpisodes.filter(ep => ep.seasonNumber === seasonNumber);

      if (seasonEpisodes.length === 0) {
        const error: ApiError = new Error(`Season ${seasonNumber} not found`);
        error.statusCode = 404;
        error.code = 'NOT_FOUND';
        return next(error);
      }

      const seasonData = {
        seasonNumber,
        episodeCount: seasonEpisodes.length,
        airDate: seasonEpisodes[0].airDate instanceof Date ? seasonEpisodes[0].airDate.toISOString() : seasonEpisodes[0].airDate,
        episodes: seasonEpisodes.map(ep => ({
          id: ep.id,
          seasonNumber: ep.seasonNumber,
          episodeNumber: ep.episodeNumber,
          title: ep.title,
          overview: ep.overview,
          airDate: ep.airDate instanceof Date ? ep.airDate.toISOString() : ep.airDate,
          stillPath: ep.stillPath ? `https://image.tmdb.org/t/p/w300${ep.stillPath}` : null,
          runtime: null
        }))
      };

      return res.json({
        tmdbId,
        title: content.title,
        numberOfSeasons: Math.max(...allEpisodes.map(ep => ep.seasonNumber)),
        numberOfEpisodes: allEpisodes.length,
        season: seasonData
      });
    }

    // Return all seasons grouped
    const seasonMap = new Map<number, typeof allEpisodes>();
    for (const episode of allEpisodes) {
      if (!seasonMap.has(episode.seasonNumber)) {
        seasonMap.set(episode.seasonNumber, []);
      }
      seasonMap.get(episode.seasonNumber)!.push(episode);
    }

    const seasons = Array.from(seasonMap.entries())
      .sort(([a], [b]) => a - b)
      .map(([seasonNum, episodes]) => ({
        seasonNumber: seasonNum,
        episodeCount: episodes.length,
        airDate: episodes[0].airDate instanceof Date ? episodes[0].airDate.toISOString() : episodes[0].airDate,
        episodes: [] // Episodes will be loaded on demand
      }));

    res.json({
      tmdbId,
      title: content.title,
      numberOfSeasons: seasons.length,
      numberOfEpisodes: allEpisodes.length,
      seasons
    });
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

/**
 * POST /api/content/:id/queue/episode
 * Add specific episode to download queue
 */
router.post(
  '/:id/queue/episode',
  validatePathParam('id'),
  validateBody(['profileId', 'title', 'seasonNumber', 'episodeNumber']),
  async (req: Request, res: Response, next: NextFunction) => {
    try {
      const tmdbId = parseInt(req.params.id, 10);
      const { profileId, title, seasonNumber, episodeNumber, year } = req.body;

      const queueItem = await downloadManager.queueEpisodeDownload({
        tmdbId,
        title,
        seasonNumber,
        episodeNumber,
        year,
        profileId
      });

      res.status(201).json({
        message: `Episode S${seasonNumber}E${episodeNumber} added to download queue`,
        queueItem
      });
    } catch (error) {
      next(error);
    }
  }
);

/**
 * POST /api/content/:id/queue/season
 * Add entire season to download queue
 */
router.post(
  '/:id/queue/season',
  validatePathParam('id'),
  validateBody(['profileId', 'title', 'seasonNumber']),
  async (req: Request, res: Response, next: NextFunction) => {
    try {
      const tmdbId = parseInt(req.params.id, 10);
      const { profileId, title, seasonNumber, year } = req.body;

      const queueItem = await downloadManager.queueSeasonDownload({
        tmdbId,
        title,
        seasonNumber,
        year,
        profileId
      });

      res.status(201).json({
        message: `Season ${seasonNumber} added to download queue`,
        queueItem
      });
    } catch (error) {
      next(error);
    }
  }
);

export default router;
