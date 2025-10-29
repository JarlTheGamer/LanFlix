import { Router, Request, Response, NextFunction } from 'express';
import { libraryService, LibraryService } from '../services';
import { validatePathParam } from '../middleware/validation';
import { ApiError } from '../middleware/error-handler';

const router = Router();

/**
 * GET /api/library/movies
 * Get all movies in library with optional filtering
 */
router.get('/movies', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const filters = {
      type: 'movie' as const,
      genre: req.query.genre as string | undefined,
      sortBy: req.query.sortBy as 'addedAt' | 'title' | 'releaseDate' | 'voteAverage' | undefined,
      sortOrder: req.query.sortOrder as 'ASC' | 'DESC' | undefined,
      limit: req.query.limit ? parseInt(req.query.limit as string) : undefined,
      offset: req.query.offset ? parseInt(req.query.offset as string) : undefined
    };

    const result = await libraryService.getLibraryItems(filters);

    res.json({
      type: 'movie',
      count: result.total,
      items: result.items
    });
  } catch (error) {
    next(error);
  }
});

/**
 * GET /api/library/series
 * Get all TV series in library with optional filtering
 */
router.get('/series', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const filters = {
      type: 'series' as const,
      genre: req.query.genre as string | undefined,
      sortBy: req.query.sortBy as 'addedAt' | 'title' | 'releaseDate' | 'voteAverage' | undefined,
      sortOrder: req.query.sortOrder as 'ASC' | 'DESC' | undefined,
      limit: req.query.limit ? parseInt(req.query.limit as string) : undefined,
      offset: req.query.offset ? parseInt(req.query.offset as string) : undefined
    };

    const result = await libraryService.getLibraryItems(filters);

    res.json({
      type: 'series',
      count: result.total,
      items: result.items
    });
  } catch (error) {
    next(error);
  }
});

/**
 * GET /api/library/recent
 * Get recently added content
 */
router.get('/recent', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const limit = req.query.limit ? parseInt(req.query.limit as string) : 20;
    const recentItems = await libraryService.getRecentlyAdded(limit);

    res.json({
      count: recentItems.length,
      items: recentItems
    });
  } catch (error) {
    next(error);
  }
});

/**
 * GET /api/library/:id
 * Get specific library item details
 */
router.get('/:id', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = parseInt(req.params.id, 10);
    const item = await libraryService.getLibraryItem(id);

    if (!item) {
      const error: ApiError = new Error('Library item not found');
      error.statusCode = 404;
      error.code = 'NOT_FOUND';
      return next(error);
    }

    res.json(item);
  } catch (error) {
    next(error);
  }
});

/**
 * DELETE /api/library/:id
 * Remove item from library
 */
router.delete('/:id', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = parseInt(req.params.id, 10);
    await libraryService.removeFromLibrary(id);

    res.json({
      message: 'Item removed from library',
      id
    });
  } catch (error) {
    next(error);
  }
});

export default router;
