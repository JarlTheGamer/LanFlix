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
  logoUrl?: string;
  serverAvailable: boolean;
  progressPercentage?: number;
  palette: ArtworkPalette;
};

export type ArtworkPalette = {
  base: string;
  depth: string;
  glow: string;
  accent: string;
  onBackground: string;
  algorithmVersion: number;
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

export type Page<T> = { items: T[]; total: number; offset: number; limit: number };

export async function getLibrary(type?: 'movie' | 'series'): Promise<Page<MediaItem>> {
  const query = type ? `?type=${type}&limit=100` : '?limit=100';
  const response = await fetch(`/api/v2/library${query}`);
  if (!response.ok) throw new Error(`Lanflix server returned ${response.status}`);
  return response.json() as Promise<Page<MediaItem>>;
}

export function resolveArtwork(url?: string): string | undefined {
  if (!url) return undefined;
  return url.startsWith('http') ? url : `${window.location.origin}${url}`;
}
