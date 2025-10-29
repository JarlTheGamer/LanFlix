import { Router, Request, Response, NextFunction } from 'express';
import { Profile, Watchlist, Content } from '../models';
import { validatePathParam, validateBody } from '../middleware/validation';
import { ApiError } from '../middleware/error-handler';

const router = Router();

/**
 * GET /api/profiles
 * List all profiles
 */
router.get('/', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const profiles = await Profile.findAll({
      order: [['createdAt', 'ASC']]
    });

    res.json({
      count: profiles.length,
      profiles: profiles.map(p => ({
        id: p.id,
        name: p.name,
        avatarColorPrimary: p.avatarColorPrimary,
        avatarColorSecondary: p.avatarColorSecondary,
        createdAt: p.createdAt
      }))
    });
  } catch (error) {
    next(error);
  }
});

/**
 * POST /api/profiles
 * Create new profile
 */
router.post('/', validateBody(['name', 'avatarColorPrimary', 'avatarColorSecondary']), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const { name, avatarColorPrimary, avatarColorSecondary } = req.body;

    // Validate color format (hex colors)
    const hexColorRegex = /^#[0-9A-Fa-f]{6}$/;
    if (!hexColorRegex.test(avatarColorPrimary) || !hexColorRegex.test(avatarColorSecondary)) {
      const error: ApiError = new Error('Avatar colors must be valid hex colors (e.g., #FF5733)');
      error.statusCode = 400;
      error.code = 'VALIDATION_ERROR';
      return next(error);
    }

    const profile = await Profile.create({
      name,
      avatarColorPrimary,
      avatarColorSecondary
    });

    res.status(201).json({
      message: 'Profile created successfully',
      profile: {
        id: profile.id,
        name: profile.name,
        avatarColorPrimary: profile.avatarColorPrimary,
        avatarColorSecondary: profile.avatarColorSecondary,
        createdAt: profile.createdAt
      }
    });
  } catch (error) {
    next(error);
  }
});

/**
 * GET /api/profiles/:id
 * Get profile details
 */
router.get('/:id', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = parseInt(req.params.id, 10);
    const profile = await Profile.findByPk(id);

    if (!profile) {
      const error: ApiError = new Error('Profile not found');
      error.statusCode = 404;
      error.code = 'NOT_FOUND';
      return next(error);
    }

    res.json({
      id: profile.id,
      name: profile.name,
      avatarColorPrimary: profile.avatarColorPrimary,
      avatarColorSecondary: profile.avatarColorSecondary,
      createdAt: profile.createdAt
    });
  } catch (error) {
    next(error);
  }
});

/**
 * PUT /api/profiles/:id
 * Update profile
 */
router.put('/:id', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = parseInt(req.params.id, 10);
    const { name, avatarColorPrimary, avatarColorSecondary } = req.body;

    const profile = await Profile.findByPk(id);

    if (!profile) {
      const error: ApiError = new Error('Profile not found');
      error.statusCode = 404;
      error.code = 'NOT_FOUND';
      return next(error);
    }

    // Validate color format if provided
    const hexColorRegex = /^#[0-9A-Fa-f]{6}$/;
    if (avatarColorPrimary && !hexColorRegex.test(avatarColorPrimary)) {
      const error: ApiError = new Error('avatarColorPrimary must be a valid hex color');
      error.statusCode = 400;
      error.code = 'VALIDATION_ERROR';
      return next(error);
    }
    if (avatarColorSecondary && !hexColorRegex.test(avatarColorSecondary)) {
      const error: ApiError = new Error('avatarColorSecondary must be a valid hex color');
      error.statusCode = 400;
      error.code = 'VALIDATION_ERROR';
      return next(error);
    }

    if (name) profile.name = name;
    if (avatarColorPrimary) profile.avatarColorPrimary = avatarColorPrimary;
    if (avatarColorSecondary) profile.avatarColorSecondary = avatarColorSecondary;

    await profile.save();

    res.json({
      message: 'Profile updated successfully',
      profile: {
        id: profile.id,
        name: profile.name,
        avatarColorPrimary: profile.avatarColorPrimary,
        avatarColorSecondary: profile.avatarColorSecondary,
        createdAt: profile.createdAt
      }
    });
  } catch (error) {
    next(error);
  }
});

/**
 * DELETE /api/profiles/:id
 * Delete profile
 */
router.delete('/:id', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const id = parseInt(req.params.id, 10);
    const profile = await Profile.findByPk(id);

    if (!profile) {
      const error: ApiError = new Error('Profile not found');
      error.statusCode = 404;
      error.code = 'NOT_FOUND';
      return next(error);
    }

    await profile.destroy();

    res.json({
      message: 'Profile deleted successfully',
      id
    });
  } catch (error) {
    next(error);
  }
});

/**
 * GET /api/profiles/:id/watchlist
 * Get profile's My List (watchlist)
 */
router.get('/:id/watchlist', validatePathParam('id'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const profileId = parseInt(req.params.id, 10);

    // Verify profile exists
    const profile = await Profile.findByPk(profileId);
    if (!profile) {
      const error: ApiError = new Error('Profile not found');
      error.statusCode = 404;
      error.code = 'NOT_FOUND';
      return next(error);
    }

    const watchlistItems = await Watchlist.findAll({
      where: { profileId },
      include: [{
        model: Content,
        as: 'content',
        required: true
      }],
      order: [['addedAt', 'DESC']]
    });

    res.json({
      profileId,
      count: watchlistItems.length,
      items: watchlistItems.map(item => ({
        id: item.id,
        addedAt: item.addedAt,
        content: (item as any).content
      }))
    });
  } catch (error) {
    next(error);
  }
});

/**
 * POST /api/profiles/:id/watchlist/:contentId
 * Add content to My List
 */
router.post('/:id/watchlist/:contentId', validatePathParam('id'), validatePathParam('contentId'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const profileId = parseInt(req.params.id, 10);
    const contentId = parseInt(req.params.contentId, 10);

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

    // Check if already in watchlist
    const existing = await Watchlist.findOne({
      where: { profileId, contentId }
    });

    if (existing) {
      return res.json({
        message: 'Content already in watchlist',
        watchlistItem: existing
      });
    }

    const watchlistItem = await Watchlist.create({
      profileId,
      contentId
    });

    res.status(201).json({
      message: 'Content added to watchlist',
      watchlistItem
    });
  } catch (error) {
    next(error);
  }
});

/**
 * DELETE /api/profiles/:id/watchlist/:contentId
 * Remove content from My List
 */
router.delete('/:id/watchlist/:contentId', validatePathParam('id'), validatePathParam('contentId'), async (req: Request, res: Response, next: NextFunction) => {
  try {
    const profileId = parseInt(req.params.id, 10);
    const contentId = parseInt(req.params.contentId, 10);

    const watchlistItem = await Watchlist.findOne({
      where: { profileId, contentId }
    });

    if (!watchlistItem) {
      const error: ApiError = new Error('Watchlist item not found');
      error.statusCode = 404;
      error.code = 'NOT_FOUND';
      return next(error);
    }

    await watchlistItem.destroy();

    res.json({
      message: 'Content removed from watchlist',
      profileId,
      contentId
    });
  } catch (error) {
    next(error);
  }
});

export default router;
