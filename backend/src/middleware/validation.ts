import { Request, Response, NextFunction } from 'express';
import { ApiError } from './error-handler';

export const validateQueryParam = (paramName: string, required: boolean = false) => {
  return (req: Request, res: Response, next: NextFunction) => {
    const value = req.query[paramName];
    
    if (required && !value) {
      const error: ApiError = new Error(`Query parameter '${paramName}' is required`);
      error.statusCode = 400;
      error.code = 'VALIDATION_ERROR';
      return next(error);
    }
    
    next();
  };
};

export const validatePathParam = (paramName: string) => {
  return (req: Request, res: Response, next: NextFunction) => {
    const value = req.params[paramName];
    
    if (!value) {
      const error: ApiError = new Error(`Path parameter '${paramName}' is required`);
      error.statusCode = 400;
      error.code = 'VALIDATION_ERROR';
      return next(error);
    }
    
    // Validate numeric IDs
    if (paramName === 'id' || paramName === 'contentId' || paramName === 'profileId') {
      const numValue = parseInt(value, 10);
      if (isNaN(numValue) || numValue <= 0) {
        const error: ApiError = new Error(`Path parameter '${paramName}' must be a positive integer`);
        error.statusCode = 400;
        error.code = 'VALIDATION_ERROR';
        return next(error);
      }
    }
    
    next();
  };
};

export const validateBody = (requiredFields: string[]) => {
  return (req: Request, res: Response, next: NextFunction) => {
    const missingFields = requiredFields.filter(field => !req.body[field]);
    
    if (missingFields.length > 0) {
      const error: ApiError = new Error(`Missing required fields: ${missingFields.join(', ')}`);
      error.statusCode = 400;
      error.code = 'VALIDATION_ERROR';
      return next(error);
    }
    
    next();
  };
};
