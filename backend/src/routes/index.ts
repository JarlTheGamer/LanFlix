import { Router } from 'express';
import contentRoutes from './content.routes';
import libraryRoutes from './library.routes';
import profileRoutes from './profile.routes';
import streamingRoutes from './streaming.routes';
import settingsRoutes from './settings.routes';
import notificationRoutes from './notification.routes';

const router = Router();

// Mount all route modules
router.use('/content', contentRoutes);
router.use('/library', libraryRoutes);
router.use('/profiles', profileRoutes);
router.use('/stream', streamingRoutes);
router.use('/settings', settingsRoutes);
router.use('/notifications', notificationRoutes);

export default router;
