using Lanflix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Infrastructure.Persistence;

/// <summary>
/// Compiled queries for frequently accessed data to improve performance
/// </summary>
public static class CompiledQueries
{
    /// <summary>
    /// Compiled query to get content by ID with episodes
    /// </summary>
    private static readonly Func<ApplicationDbContext, int, Task<Content?>>
        GetContentByIdQuery = EF.CompileAsyncQuery(
            (ApplicationDbContext context, int id) =>
                context.Contents
                    .Include(c => c.Episodes)
                    .FirstOrDefault(c => c.Id == id));

    /// <summary>
    /// Compiled query to get content by TMDB ID
    /// </summary>
    private static readonly Func<ApplicationDbContext, int, Task<Content?>>
        GetContentByTmdbIdQuery = EF.CompileAsyncQuery(
            (ApplicationDbContext context, int tmdbId) =>
                context.Contents
                    .Include(c => c.Episodes)
                    .FirstOrDefault(c => c.TmdbId == tmdbId));

    /// <summary>
    /// Compiled query to get profile by ID with preferences
    /// </summary>
    private static readonly Func<ApplicationDbContext, int, Task<Profile?>>
        GetProfileByIdQuery = EF.CompileAsyncQuery(
            (ApplicationDbContext context, int id) =>
                context.Profiles
                    .FirstOrDefault(p => p.Id == id));

    // Note: Compiled queries with collections are complex, so we'll use regular queries for these

    /// <summary>
    /// Compiled query to get watch history for a profile and content
    /// </summary>
    private static readonly Func<ApplicationDbContext, int, int, int?, Task<WatchHistory?>>
        GetWatchHistoryQuery = EF.CompileAsyncQuery(
            (ApplicationDbContext context, int profileId, int contentId, int? episodeId) =>
                context.WatchHistories
                    .FirstOrDefault(w => w.ProfileId == profileId
                                      && w.ContentId == contentId
                                      && w.EpisodeId == episodeId));





    /// <summary>
    /// Compiled query to get stream session by session ID
    /// </summary>
    private static readonly Func<ApplicationDbContext, string, Task<StreamSession?>>
        GetStreamSessionByIdQuery = EF.CompileAsyncQuery(
            (ApplicationDbContext context, string sessionId) =>
                context.StreamSessions
                    .Include(s => s.Content)
                    .Include(s => s.Episode)
                    .Include(s => s.Profile)
                    .FirstOrDefault(s => s.SessionId == sessionId));

    /// <summary>
    /// Compiled query to get episode by content ID, season, and episode number
    /// </summary>
    private static readonly Func<ApplicationDbContext, int, int, int, Task<Episode?>>
        GetEpisodeQuery = EF.CompileAsyncQuery(
            (ApplicationDbContext context, int contentId, int seasonNumber, int episodeNumber) =>
                context.Episodes
                    .FirstOrDefault(e => e.ContentId == contentId
                                      && e.SeasonNumber == seasonNumber
                                      && e.EpisodeNumber == episodeNumber));



    // Extension methods to use compiled queries

    public static Task<Content?> GetContentByIdAsync(
        this ApplicationDbContext context, int id)
    {
        return GetContentByIdQuery(context, id);
    }

    public static Task<Content?> GetContentByTmdbIdAsync(
        this ApplicationDbContext context, int tmdbId)
    {
        return GetContentByTmdbIdQuery(context, tmdbId);
    }

    public static Task<Profile?> GetProfileByIdAsync(
        this ApplicationDbContext context, int id)
    {
        return GetProfileByIdQuery(context, id);
    }

    public static Task<List<Profile>> GetAllProfilesAsync(
        this ApplicationDbContext context)
    {
        return context.Profiles.OrderBy(p => p.Name).ToListAsync();
    }

    public static Task<WatchHistory?> GetWatchHistoryAsync(
        this ApplicationDbContext context, int profileId, int contentId, int? episodeId)
    {
        return GetWatchHistoryQuery(context, profileId, contentId, episodeId);
    }

    public static Task<List<WatchHistory>> GetRecentWatchHistoryAsync(
        this ApplicationDbContext context, int profileId, int count)
    {
        return context.WatchHistories
            .Include(w => w.Content)
            .Include(w => w.Episode)
            .Where(w => w.ProfileId == profileId)
            .OrderByDescending(w => w.LastWatchedAt)
            .Take(count)
            .ToListAsync();
    }

    public static Task<List<StreamSession>> GetActiveStreamSessionsAsync(
        this ApplicationDbContext context, int profileId)
    {
        return context.StreamSessions
            .Include(s => s.Content)
            .Include(s => s.Episode)
            .Where(s => s.ProfileId == profileId && s.IsActive)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
    }

    public static Task<StreamSession?> GetStreamSessionByIdAsync(
        this ApplicationDbContext context, string sessionId)
    {
        return GetStreamSessionByIdQuery(context, sessionId);
    }

    public static Task<Episode?> GetEpisodeAsync(
        this ApplicationDbContext context, int contentId, int seasonNumber, int episodeNumber)
    {
        return GetEpisodeQuery(context, contentId, seasonNumber, episodeNumber);
    }

    public static Task<List<Watchlist>> GetWatchlistAsync(
        this ApplicationDbContext context, int profileId)
    {
        return context.Watchlists
            .Include(w => w.Content)
            .Where(w => w.ProfileId == profileId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync();
    }
}
