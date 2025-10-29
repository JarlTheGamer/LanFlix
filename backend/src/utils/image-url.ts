import { config } from '../config/env';
import path from 'path';
import fs from 'fs';

/**
 * Generate local image URL for cached images
 * Falls back to TMDB URL if local image doesn't exist
 */
export function getImageUrl(
  imagePath: string | null | undefined,
  type: 'poster' | 'backdrop',
  contentId?: number
): string | undefined {
  if (!imagePath) return undefined;

  // If it's already a full URL, return it
  if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
    return imagePath;
  }

  // Check if local cached image exists
  if (contentId) {
    const cacheDir = type === 'poster' ? config.media.posterCachePath : config.media.backdropCachePath;
    const fileName = `${contentId}-${type}.jpg`;
    const localPath = path.join(cacheDir, fileName);

    if (fs.existsSync(localPath)) {
      // Return local URL
      return `/images/${type}s/${fileName}`;
    }
  }

  // Fall back to TMDB URL
  const size = type === 'poster' ? 'w500' : 'original';
  return `https://image.tmdb.org/t/p/${size}${imagePath}`;
}

/**
 * Get poster URL
 */
export function getPosterUrl(posterPath: string | null | undefined, contentId?: number): string | undefined {
  return getImageUrl(posterPath, 'poster', contentId);
}

/**
 * Get backdrop URL
 */
export function getBackdropUrl(backdropPath: string | null | undefined, contentId?: number): string | undefined {
  return getImageUrl(backdropPath, 'backdrop', contentId);
}
