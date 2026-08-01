export type MediaItem = {
  id: number;
  tmdbId: number;
  type: 'movie' | 'series';
  title: string;
  overview?: string;
  year?: number;
  rating?: number;
  genres: string[];
  posterUrl?: string;
  backdropUrl?: string;
  serverAvailable: boolean;
  progressPercentage?: number;
};

export type HomeResponse = {
  continueWatching: MediaItem[];
  recentlyAdded: MediaItem[];
  hero?: MediaItem;
};

export async function getHome(): Promise<HomeResponse> {
  const response = await fetch('/api/v2/home');
  if (!response.ok) throw new Error(`Lanflix server returned ${response.status}`);
  return response.json() as Promise<HomeResponse>;
}

export function resolveArtwork(url?: string): string | undefined {
  if (!url) return undefined;
  return url.startsWith('http') ? url : `${window.location.origin}${url}`;
}
