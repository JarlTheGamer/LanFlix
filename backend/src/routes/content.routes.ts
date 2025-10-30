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
 * Get episodes for a TV series (with progressive loading)
 * Also stores episode metadata in database for offline access
 */
router.get('/:id/episodes', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const tmdbId = parseInt(req.params.id, 10);
    const seasonNumber = req.query.season ? parseInt(req.query.season as string) : null;

    // Import dependencies
    const { tmdbClient } = await import('../clients');
    const Content = (await import('../models/Content')).default;
    const SeriesEpisode = (await import('../models/SeriesEpisode')).default;

    // Get TV details first to know how many seasons
    const tvDetails = await tmdbClient.getTVDetails(tmdbId);

    // Check if series exists in library
    const content = await Content.findOne({
      where: { tmdbId, type: 'series' }
    });

    // If specific season requested, return only that season
    if (seasonNumber !== null) {
      try {
        // First check if we have episodes in database
        if (content) {
          const dbEpisodes = await SeriesEpisode.findAll({
            where: {
              contentId: content.id,
              seasonNumber
            },
            order: [['episodeNumber', 'ASC']]
          });

          // If we have episodes with metadata in DB, use them
          if (dbEpisodes.length > 0 && dbEpisodes[0].title) {
            const seasonData = {
              seasonNumber,
              episodeCount: dbEpisodes.length,
              airDate: dbEpisodes[0].airDate?.toISOString(),
              episodes: dbEpisodes.map(ep => ({
                id: ep.id,
                seasonNumber: ep.seasonNumber,
                episodeNumber: ep.episodeNumber,
                title: ep.title,
                overview: ep.overview,
                airDate: ep.airDate?.toISOString(),
                stillPath: ep.stillPath ? `https://image.tmdb.org/t/p/w300${ep.stillPath}` : null,
                runtime: null
              }))
            };

            return res.json({
              tmdbId,
              title: tvDetails.name,
              numberOfSeasons: tvDetails.number_of_seasons,
              numberOfEpisodes: tvDetails.number_of_episodes,
              season: seasonData
            });
          }
        }

        // Fetch from TMDB if not in database
        const seasonDetails = await tmdbClient.getSeasonDetails(tmdbId, seasonNumber);
        const seasonData = {
          seasonNumber: seasonDetails.season_number,
          episodeCount: seasonDetails.episodes.length,
          airDate: seasonDetails.air_date,
          episodes: seasonDetails.episodes.map((ep: any) => ({
            id: ep.id,
            seasonNumber: ep.season_number,
            episodeNumber: ep.episode_number,
            title: ep.name,
            overview: ep.overview,
            airDate: ep.air_date,
            stillPath: ep.still_path ? `https://image.tmdb.org/t/p/w300${ep.still_path}` : null,
            runtime: ep.runtime
          }))
        };

        // Store episodes in database if content exists in library
        if (content) {
          for (const ep of seasonDetails.episodes) {
            const existing = await SeriesEpisode.findOne({
              where: {
                contentId: content.id,
                seasonNumber: ep.season_number,
                episodeNumber: ep.episode_number
              }
            });

            if (!existing) {
              await SeriesEpisode.create({
                contentId: content.id,
                seasonNumber: ep.season_number,
                episodeNumber: ep.episode_number,
                title: ep.name,
                overview: ep.overview,
                airDate: ep.air_date ? new Date(ep.air_date) : undefined,
                stillPath: ep.still_path || undefined
              });
            } else if (!existing.title) {
              // Update metadata if episode exists but metadata is missing
              await existing.update({
                title: ep.name,
                overview: ep.overview,
                airDate: ep.air_date ? new Date(ep.air_date) : undefined,
                stillPath: ep.still_path || undefined
              });
            }
          }
        }

        return res.json({
          tmdbId,
          title: tvDetails.name,
          numberOfSeasons: tvDetails.number_of_seasons,
          numberOfEpisodes: tvDetails.number_of_episodes,
          season: seasonData
        });
      } catch (error) {
        console.error(`Failed to fetch season ${seasonNumber}:`, error);
        return res.status(500).json({ error: 'Failed to fetch season details' });
      }
    }

    // Return season list without episodes (for initial load)
    const seasons = tvDetails.seasons
      .filter(season => season.season_number > 0) // Skip season 0 (specials)
      .map(season => ({
        seasonNumber: season.season_number,
        episodeCount: season.episode_count,
        airDate: season.air_date,
        episodes: [] // Episodes will be loaded on demand
      }));

    res.json({
      tmdbId,
      title: tvDetails.name,
      numberOfSeasons: tvDetails.number_of_seasons,
      numberOfEpisodes: tvDetails.number_of_episodes,
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
