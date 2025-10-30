import { config } from '../config/env';
import path from 'path';
import fs from 'fs';

/**
 * Generate local image URL for images stored in media folders
 * Falls back to TMDB URL if local image doesn't exist
 */
export function getImageUrl(
  imagePath: string | null | undefined,
  type: 'poster' | 'backdrop',
  contentId?: number,
  filePath?: string | null
): string | undefined {
  if (!imagePath) return undefined;

  // If it's already a full URL, return it
  if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
    return imagePath;
  }

  // Get the backend URL (for absolute URLs when frontend is on different port)
  const backendUrl = process.env.BACKEND_URL || 'http://localhost:3000';

  // First, try to serve from the media folder (where the movie/series is stored)
  if (filePath) {
    const mediaFolder = path.dirname(filePath);
    const localImagePath = path.join(mediaFolder, `${type}.jpg`);

    if (fs.existsSync(localImagePath)) {
      // Return URL to serve from media folder
      // Convert to relative path from media root and normalize for URLs
      const absoluteMediaRoot = path.resolve(config.media.rootPath);
      const absoluteImagePath = path.resolve(localImagePath);
      const relativePath = path.relative(absoluteMediaRoot, absoluteImagePath);

      // Convert Windows backslashes to forward slashes for URLs
      const urlPath = relativePath.split(path.sep).join('/');

      return `${backendUrl}/media/${urlPath}`;
    }
  }

  // Fall back to cached images
  if (contentId) {
    const cacheDir = type === 'poster' ? config.media.posterCachePath : config.media.backdropCachePath;
    const fileName = `${contentId}-${type}.jpg`;
    const localPath = path.join(cacheDir, fileName);

    if (fs.existsSync(localPath)) {
      return `${backendUrl}/images/${type}s/${fileName}`;
    }
  }

  // Fall back to TMDB URL
  const size = type === 'poster' ? 'w500' : 'original';
  return `https://image.tmdb.org/t/p/${size}${imagePath}`;
}

/**
 * Get poster URL
 */
export function getPosterUrl(posterPath: string | null | undefined, contentId?: number, filePath?: string | null): string | undefined {
  return getImageUrl(posterPath, 'poster', contentId, filePath);
}

/**
 * Get backdrop URL
 */
export function getBackdropUrl(backdropPath: string | null | undefined, contentId?: number, filePath?: string | null): string | undefined {
  return getImageUrl(backdropPath, 'backdrop', contentId, filePath);
}

/**
 * Get episode still URL
 * Checks for local image in season folder first, falls back to TMDB
 */
export function getEpisodeStillUrl(
  stillPath: string | null | undefined,
  seriesFilePath?: string | null,
  seasonNumber?: number,
  episodeNumber?: number
): string | undefined {
  if (!stillPath) return undefined;

  // If it's already a full URL, return it
  if (stillPath.startsWith('http://') || stillPath.startsWith('https://')) {
    return stillPath;
  }

  const backendUrl = process.env.BACKEND_URL || 'http://localhost:3000';

  // Check if it's a local filename (e.g., "S01E01.jpg")
  if (stillPath.match(/^S\d+E\d+\.jpg$/i)) {
    // It's a local file, construct path
    if (seriesFilePath && seasonNumber !== undefined) {
      const seasonFolder = path.join(seriesFilePath, `Season ${seasonNumber}`);
      const localImagePath = path.join(seasonFolder, stillPath);

      if (fs.existsSync(localImagePath)) {
        // Return URL to serve from media folder
        const absoluteMediaRoot = path.resolve(config.media.rootPath);
        const absoluteImagePath = path.resolve(localImagePath);
        const relativePath = path.relative(absoluteMediaRoot, absoluteImagePath);
        const urlPath = relativePath.split(path.sep).join('/');
        return `${backendUrl}/media/${urlPath}`;
      }
    }
    // If it's a local filename but file doesn't exist, return undefined
    return undefined;
  }

  // Fall back to TMDB URL (only for TMDB paths starting with /)
  return `https://image.tmdb.org/t/p/w300${stillPath}`;
}
