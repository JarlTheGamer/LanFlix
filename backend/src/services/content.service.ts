import { TMDBClient, ProwlarrClient, tmdbClient, prowlarrClient } from '../clients';
import { MetadataService } from './metadata.service';
import { cacheManager } from '../utils/cache-manager';
import logger from '../utils/logger';
import Content from '../models/Content';
import Watchlist from '../models/Watchlist';

interface SearchResult {
  id: number;
  tmdbId: number;
  type: 'movie' | 'series';
  title: string;
  originalTitle: string;
  overview: string;
  releaseDate: string;
  posterUrl?: string;
  backdropUrl?: string;
  voteAverage: number;
  voteCount: number;
  genres: string[];
  inLibrary: boolean;
  inWatchlist?: boolean;
}

interface ContentDetails extends SearchResult {
  runtime?: number;
  status: string;
  cast?: Array<{ name: string; character: string }>;
  director?: string;
  numberOfSeasons?: number;
  numberOfEpisodes?: number;
  seasons?: Array<{
    seasonNumber: number;
    episodeCount: number;
    airDate: string;
  }>;
}

interface TrendingContent {
  movies: SearchResult[];
  series: SearchResult[];
}

/**
 * Service for content discovery and search
 * Handles searching via Prowlarr and TMDB, trending content, and content details
 */
export class ContentService {
  private tmdbClient: TMDBClient;
  private prowlarrClient: ProwlarrClient;
  private metadataService: MetadataService;
  private imageBaseUrl = 'https://image.tmdb.org/t/p';
  private posterSize = 'w500';
  private backdropSize = 'w1280';

  constructor(
    tmdb?: TMDBClient,
    prowlarr?: ProwlarrClient,
    metadataService?: MetadataService
  ) {
    this.tmdbClient = tmdb || tmdbClient;
    this.prowlarrClient = prowlarr || prowlarrClient;
    this.metadataService = metadataService || new MetadataService();
  }

  /**
   * Search for content using Prowlarr and TMDB
   */
  async searchContent(
    query: string,
    type: 'movie' | 'series' | 'all' = 'all',
    profileId?: number
  ): Promise<SearchResult[]> {
    try {
      logger.info(`Searching for content: ${query}, type: ${type}`);

      // Search TMDB for metadata - gracefully handle API failures
      const [movieResults, tvResults] = await Promise.all([
        type === 'series' 
          ? Promise.resolve({ results: [] }) 
          : this.tmdbClient.searchMovie(query).catch((error) => {
              logger.warn('TMDB API unavailable for movie search:', error.message);
              return { results: [] };
            }),
        type === 'movie' 
          ? Promise.resolve({ results: [] }) 
          : this.tmdbClient.searchTV(query).catch((error) => {
              logger.warn('TMDB API unavailable for TV search:', error.message);
              return { results: [] };
            })
      ]);

      // Get library content to mark what's already available
      const libraryContent = await Content.findAll({
        attributes: ['tmdbId', 'type']
      });
      const libraryMap = new Map(
        libraryContent.map(c => [`${c.type}-${c.tmdbId}`, true])
      );

      // Get watchlist if profileId provided
      let watchlistMap = new Map<string, boolean>();
      if (profileId) {
        const watchlistItems = await Watchlist.findAll({
          where: { profileId },
          include: [{ model: Content, as: 'content' }]
        });
        watchlistMap = new Map(
          watchlistItems.map(w => {
            const content = (w as any).content;
            return [`${content.type}-${content.tmdbId}`, true];
          })
        );
      }

      // Convert TMDB results to SearchResult format
      const results: SearchResult[] = [];

      // Add movie results
      for (const movie of movieResults.results) {
        const key = `movie-${movie.id}`;
        results.push({
          id: movie.id,
          tmdbId: movie.id,
          type: 'movie',
          title: movie.title,
          originalTitle: movie.original_title,
          overview: movie.overview,
          releaseDate: movie.release_date,
          posterUrl: movie.poster_path
            ? `${this.imageBaseUrl}/${this.posterSize}${movie.poster_path}`
            : undefined,
          backdropUrl: movie.backdrop_path
            ? `${this.imageBaseUrl}/${this.backdropSize}${movie.backdrop_path}`
            : undefined,
          voteAverage: movie.vote_average,
          voteCount: movie.vote_count,
          genres: [], // Will be populated when fetching details
          inLibrary: libraryMap.has(key),
          inWatchlist: watchlistMap.has(key)
        });
      }

      // Add TV results
      for (const tv of tvResults.results) {
        const key = `series-${tv.id}`;
        results.push({
          id: tv.id,
          tmdbId: tv.id,
          type: 'series',
          title: tv.name,
          originalTitle: tv.original_name,
          overview: tv.overview,
          releaseDate: tv.first_air_date,
          posterUrl: tv.poster_path
            ? `${this.imageBaseUrl}/${this.posterSize}${tv.poster_path}`
            : undefined,
          backdropUrl: tv.backdrop_path
            ? `${this.imageBaseUrl}/${this.backdropSize}${tv.backdrop_path}`
            : undefined,
          voteAverage: tv.vote_average,
          voteCount: tv.vote_count,
          genres: [],
          inLibrary: libraryMap.has(key),
          inWatchlist: watchlistMap.has(key)
        });
      }

      logger.info(`Search completed: ${results.length} results found`);
      return results;
    } catch (error) {
      logger.error('Failed to search content:', error);
      // Return empty results instead of throwing - allows UI to continue working
      return [];
    }
  }

  /**
   * Get trending content with caching
   */
  async getTrendingContent(profileId?: number): Promise<TrendingContent> {
    const cacheKey = cacheManager.generateKey('trending', 'content');

    return cacheManager.get(
      cacheKey,
      async () => {
        logger.info('Fetching trending content from TMDB');

        // Fetch with error handling - return empty results if TMDB fails
        const [trendingMovies, trendingSeries] = await Promise.all([
          this.tmdbClient.getTrending('movie', 'week').catch((error) => {
            logger.error('Failed to fetch trending movies from TMDB:', error.message);
            return { results: [] };
          }),
          this.tmdbClient.getTrending('tv', 'week').catch((error) => {
            logger.error('Failed to fetch trending TV from TMDB:', error.message);
            return { results: [] };
          })
        ]);

        // Get library content
        const libraryContent = await Content.findAll({
          attributes: ['tmdbId', 'type']
        });
        const libraryMap = new Map(
          libraryContent.map(c => [`${c.type}-${c.tmdbId}`, true])
        );

        // Get watchlist if profileId provided
        let watchlistMap = new Map<string, boolean>();
        if (profileId) {
          const watchlistItems = await Watchlist.findAll({
            where: { profileId },
            include: [{ model: Content, as: 'content' }]
          });
          watchlistMap = new Map(
            watchlistItems.map(w => {
              const content = (w as any).content;
              return [`${content.type}-${content.tmdbId}`, true];
            })
          );
        }

        const movies: SearchResult[] = trendingMovies.results.map((movie: any) => {
          const key = `movie-${movie.id}`;
          return {
            id: movie.id,
            tmdbId: movie.id,
            type: 'movie' as const,
            title: movie.title,
            originalTitle: movie.original_title,
            overview: movie.overview,
            releaseDate: movie.release_date,
            posterUrl: movie.poster_path
              ? `${this.imageBaseUrl}/${this.posterSize}${movie.poster_path}`
              : undefined,
            backdropUrl: movie.backdrop_path
              ? `${this.imageBaseUrl}/${this.backdropSize}${movie.backdrop_path}`
              : undefined,
            voteAverage: movie.vote_average,
            voteCount: movie.vote_count,
            genres: [],
            inLibrary: libraryMap.has(key),
            inWatchlist: watchlistMap.has(key)
          };
        });

        const series: SearchResult[] = trendingSeries.results.map((tv: any) => {
          const key = `series-${tv.id}`;
          return {
            id: tv.id,
            tmdbId: tv.id,
            type: 'series' as const,
            title: tv.name,
            originalTitle: tv.original_name,
            overview: tv.overview,
            releaseDate: tv.first_air_date,
            posterUrl: tv.poster_path
              ? `${this.imageBaseUrl}/${this.posterSize}${tv.poster_path}`
              : undefined,
            backdropUrl: tv.backdrop_path
              ? `${this.imageBaseUrl}/${this.backdropSize}${tv.backdrop_path}`
              : undefined,
            voteAverage: tv.vote_average,
            voteCount: tv.vote_count,
            genres: [],
            inLibrary: libraryMap.has(key),
            inWatchlist: watchlistMap.has(key)
          };
        });

        return { movies, series };
      },
      { ttl: 6 * 60 * 60 * 1000 } // Cache for 6 hours
    );
  }

  /**
   * Get popular content with caching
   */
  async getPopularContent(
    type: 'movie' | 'series',
    page = 1,
    profileId?: number
  ): Promise<SearchResult[]> {
    const cacheKey = cacheManager.generateKey('popular', type, page);

    return cacheManager.get(
      cacheKey,
      async () => {
        logger.info(`Fetching popular ${type} from TMDB, page ${page}`);

        const tmdbType = type === 'series' ? 'tv' : 'movie';
        
        // Gracefully handle API failures
        const response = await this.tmdbClient.getPopular(tmdbType, page).catch((error) => {
          logger.warn(`TMDB API unavailable for popular ${type}:`, error.message);
          return { results: [] };
        });

        // Get library content
        const libraryContent = await Content.findAll({
          attributes: ['tmdbId', 'type']
        });
        const libraryMap = new Map(
          libraryContent.map(c => [`${c.type}-${c.tmdbId}`, true])
        );

        // Get watchlist if profileId provided
        let watchlistMap = new Map<string, boolean>();
        if (profileId) {
          const watchlistItems = await Watchlist.findAll({
            where: { profileId },
            include: [{ model: Content, as: 'content' }]
          });
          watchlistMap = new Map(
            watchlistItems.map(w => {
              const content = (w as any).content;
              return [`${content.type}-${content.tmdbId}`, true];
            })
          );
        }

        const results: SearchResult[] = response.results.map((item: any) => {
          const isMovie = type === 'movie';
          const key = `${type}-${item.id}`;

          return {
            id: item.id,
            tmdbId: item.id,
            type,
            title: isMovie ? item.title : item.name,
            originalTitle: isMovie ? item.original_title : item.original_name,
            overview: item.overview,
            releaseDate: isMovie ? item.release_date : item.first_air_date,
            posterUrl: item.poster_path
              ? `${this.imageBaseUrl}/${this.posterSize}${item.poster_path}`
              : undefined,
            backdropUrl: item.backdrop_path
              ? `${this.imageBaseUrl}/${this.backdropSize}${item.backdrop_path}`
              : undefined,
            voteAverage: item.vote_average,
            voteCount: item.vote_count,
            genres: [],
            inLibrary: libraryMap.has(key),
            inWatchlist: watchlistMap.has(key)
          };
        });

        return results;
      },
      { ttl: 6 * 60 * 60 * 1000 } // Cache for 6 hours
    );
  }

  /**
   * Get content details with metadata enrichment
   */
  async getContentDetails(
    tmdbId: number,
    type: 'movie' | 'series',
    profileId?: number
  ): Promise<ContentDetails | null> {
    try {
      logger.info(`Fetching content details: ${type} ${tmdbId}`);

      // Fetch metadata - gracefully handle API failures
      const metadata = await this.metadataService.getMetadata(tmdbId, type).catch((error) => {
        logger.warn(`TMDB API unavailable for content details ${type} ${tmdbId}:`, error.message);
        return null;
      });
      
      if (!metadata) {
        return null;
      }

      // Check if in library
      const libraryContent = await Content.findOne({
        where: { tmdbId, type }
      });

      // Check if in watchlist
      let inWatchlist = false;
      if (profileId && libraryContent) {
        const watchlistItem = await Watchlist.findOne({
          where: {
            profileId,
            contentId: libraryContent.id
          }
        });
        inWatchlist = !!watchlistItem;
      }

      const details: ContentDetails = {
        id: tmdbId,
        tmdbId,
        type,
        title: metadata.title,
        originalTitle: metadata.originalTitle,
        overview: metadata.overview,
        releaseDate: type === 'movie'
          ? (metadata as any).releaseDate
          : (metadata as any).firstAirDate,
        posterUrl: metadata.posterPath
          ? `${this.imageBaseUrl}/${this.posterSize}${metadata.posterPath}`
          : undefined,
        backdropUrl: metadata.backdropPath
          ? `${this.imageBaseUrl}/${this.backdropSize}${metadata.backdropPath}`
          : undefined,
        voteAverage: metadata.voteAverage,
        voteCount: metadata.voteCount,
        genres: metadata.genres,
        status: metadata.status,
        inLibrary: !!libraryContent,
        inWatchlist
      };

      // Add type-specific fields
      if (type === 'movie') {
        const movieMetadata = metadata as any;
        details.runtime = movieMetadata.runtime;
        details.cast = movieMetadata.cast;
        details.director = movieMetadata.director;
      } else {
        const seriesMetadata = metadata as any;
        details.numberOfSeasons = seriesMetadata.numberOfSeasons;
        details.numberOfEpisodes = seriesMetadata.numberOfEpisodes;
        details.seasons = seriesMetadata.seasons;
      }

      return details;
    } catch (error) {
      logger.error(`Failed to get content details for ${type} ${tmdbId}:`, error);
      throw error;
    }
  }

  /**
   * Detect content type from search query or TMDB ID
   */
  async detectContentType(tmdbId: number): Promise<'movie' | 'series' | null> {
    try {
      // Try to fetch as movie first
      try {
        await this.tmdbClient.getMovieDetails(tmdbId);
        return 'movie';
      } catch (movieError) {
        // If movie fetch fails, try TV
        try {
          await this.tmdbClient.getTVDetails(tmdbId);
          return 'series';
        } catch (tvError) {
          logger.warn(`Could not determine content type for TMDB ID ${tmdbId}`);
          return null;
        }
      }
    } catch (error) {
      logger.error(`Failed to detect content type for TMDB ID ${tmdbId}:`, error);
      return null;
    }
  }

  /**
   * Search for content availability via Prowlarr
   */
  async searchAvailability(
    title: string,
    type: 'movie' | 'series'
  ): Promise<any[]> {
    try {
      logger.info(`Searching availability for: ${title} (${type})`);

      const prowlarrType = type === 'series' ? 'tv' : 'movie';
      const results = await this.prowlarrClient.search(title, prowlarrType);

      const normalized = this.prowlarrClient.normalizeSearchResults(results);
      const filtered = this.prowlarrClient.filterAndSortResults(normalized, {
        minSeeders: 5,
        sortBy: 'seeders'
      });

      logger.info(`Found ${filtered.length} available sources for ${title}`);
      return filtered;
    } catch (error) {
      logger.error(`Failed to search availability for ${title}:`, error);
      throw error;
    }
  }
}

export default new ContentService();
