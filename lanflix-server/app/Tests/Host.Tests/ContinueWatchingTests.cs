using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Common.Models;
using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;
using Lanflix.Infrastructure.Adapters.Library;
using Lanflix.Infrastructure.Persistence;
using Lanflix.Modules.Metadata;
using Lanflix.Modules.Playback;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Lanflix.Host.Tests;

public sealed class ContinueWatchingTests
{
    [Fact]
    public async Task Home_uses_incomplete_account_progress_and_deduplicates_series()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var movie = new Content { TmdbId = 101, Type = ContentType.Movie, Title = "Movie", FilePath = "", AddedAt = DateTime.UtcNow };
        var series = new Content { TmdbId = 202, Type = ContentType.Series, Title = "Series", FilePath = "", AddedAt = DateTime.UtcNow.AddMinutes(-1) };
        series.Episodes.Add(new Episode { TmdbId = 301, SeasonNumber = 1, EpisodeNumber = 1, Title = "Pilot", FilePath = "", AddedAt = DateTime.UtcNow });
        series.Episodes.Add(new Episode { TmdbId = 302, SeasonNumber = 1, EpisodeNumber = 2, Title = "Next", FilePath = "", AddedAt = DateTime.UtcNow });
        db.Contents.AddRange(movie, series);
        await db.SaveChangesAsync();

        var accountId = Guid.NewGuid();
        var movieProgress = PlaybackProgress.Create(accountId, "movie", movie.Id);
        movieProgress.Update(30_000, 100_000, false);
        var firstEpisode = PlaybackProgress.Create(accountId, "episode", series.Episodes.ElementAt(0).Id);
        firstEpisode.Update(20_000, 100_000, false);
        await Task.Delay(5);
        var latestEpisode = PlaybackProgress.Create(accountId, "episode", series.Episodes.ElementAt(1).Id);
        latestEpisode.Update(60_000, 100_000, false);
        var completedForAnotherAccount = PlaybackProgress.Create(Guid.NewGuid(), "movie", movie.Id);
        completedForAnotherAccount.Update(100_000, 100_000, true);
        db.PlaybackProgress.AddRange(movieProgress, firstEpisode, latestEpisode, completedForAnotherAccount);
        await db.SaveChangesAsync();

        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 32 });
        var palettes = new ArtworkPaletteService(db, new TestHttpClientFactory());
        var catalog = new SqliteLibraryCatalog(db, new UnusedTmdbClient(), cache, palettes);

        var home = await catalog.GetHomeAsync(accountId, 10, CancellationToken.None);

        Assert.Equal(2, home.ContinueWatching.Count);
        Assert.Equal("Series", home.ContinueWatching[0].Title);
        Assert.Equal(60, home.ContinueWatching[0].ProgressPercentage);
        Assert.Equal("Movie", home.ContinueWatching[1].Title);
        Assert.Equal(30, home.ContinueWatching[1].ProgressPercentage);
        Assert.Equal("Series", home.Hero?.Title);
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class UnusedTmdbClient : ITmdbClient
    {
        public Task<TmdbSearchResult> SearchMoviesAsync(string query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TmdbSearchResult> SearchTvSeriesAsync(string query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TmdbMovieDetails?> GetMovieDetailsAsync(int tmdbId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TmdbTvSeriesDetails?> GetTvSeriesDetailsAsync(int tmdbId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TmdbSeasonDetails?> GetSeasonDetailsAsync(int seriesId, int seasonNumber, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TmdbSearchResult> GetTrendingAsync(string mediaType = "all", string timeWindow = "week", CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TmdbSearchResult> GetPopularMoviesAsync(int page = 1, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TmdbSearchResult> GetPopularTvSeriesAsync(int page = 1, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string?> GetLogoPathAsync(int tmdbId, bool isSeries, string language = "en", CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
