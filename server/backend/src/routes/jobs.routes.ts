import { Router, Request, Response } from 'express';
import { jobScheduler } from '../jobs/scheduler';
import logger from '../utils/logger';

const router = Router();

/**
 * GET /api/jobs/status
 * Get job scheduler status
 */
router.get('/status', (req: Request, res: Response) => {
  try {
    const status = jobScheduler.getStatus();
    res.json(status);
  } catch (error) {
    logger.error('Failed to get job status:', error);
    res.status(500).json({
      error: {
        code: 'JOB_STATUS_ERROR',
        message: 'Failed to get job status'
      }
    });
  }
});

/**
 * POST /api/jobs/:jobName/trigger
 * Manually trigger a specific job
 */
router.post('/:jobName/trigger', async (req: Request, res: Response) => {
  try {
    const { jobName } = req.params;

    await jobScheduler.triggerJob(jobName);

    res.json({
      message: `Job ${jobName} triggered successfully`
    });
  } catch (error) {
    logger.error('Failed to trigger job:', error);
    res.status(500).json({
      error: {
        code: 'JOB_TRIGGER_ERROR',
        message: (error as Error).message || 'Failed to trigger job'
      }
    });
  }
});

export default router;
