import { Request, Response, NextFunction } from 'express';
import { apiStatusChecker } from '../utils/api-status';

/**
 * Middleware to inject API status into response
 * Adds a warning message if server is in offline/limited mode
 */
export const injectApiStatus = (req: Request, res: Response, next: NextFunction) => {
  const originalJson = res.json.bind(res);

  res.json = function (data: any) {
    const statusMessage = apiStatusChecker.getStatusMessage();
    
    if (statusMessage) {
      // Inject status message into response
      const enhancedData = {
        ...data,
        _serverStatus: {
          offlineMode: true,
          message: statusMessage
        }
      };
      return originalJson(enhancedData);
    }

    return originalJson(data);
  };

  next();
};
