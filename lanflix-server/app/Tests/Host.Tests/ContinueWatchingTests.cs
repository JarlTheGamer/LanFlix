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
        var catalog = new SqliteLibraryCatalog(db, new UnusedTmdbClient(), new UnusedMetadataService(), cache, palettes);

        var home = await catalog.GetHomeAsync(accountId, 10, CancellationToken.None);

        Assert.Equal(2, home.ContinueWatching.Count);
        Assert.Equal("Series", home.ContinueWatching[0].Title);
        Assert.Equal(60, home.ContinueWatching[0].ProgressPercentage);
        Assert.Equal("Movie", home.ContinueWatching[1].Title);
        Assert.Equal(30, home.ContinueWatching[1].ProgressPercentage);
        Assert.Equal("Series", home.Hero?.Title);
    }

    [Fact]
    public async Task Episode_artwork_uses_canonical_season_still_and_repairs_database_path()
    {
        var root = Path.Combine(Path.GetTempPath(), "lanflix-still-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var video = Path.Combine(root, "episode.mkv");
            var still = Path.Combine(root, "S01E02.jpg");
            await File.WriteAllBytesAsync(video, [0]);
            await File.WriteAllBytesAsync(still, [0xFF, 0xD8, 0xFF, 0xD9]);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("Data Source=:memory:").Options;
            await using var db = new ApplicationDbContext(options);
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();
            var series = new Content { TmdbId = 202, Type = ContentType.Series, Title = "Series", FilePath = root, AddedAt = DateTime.UtcNow };
            var episode = new Episode { TmdbId = 302, SeasonNumber = 1, EpisodeNumber = 2, Title = "Next", FilePath = video, AddedAt = DateTime.UtcNow };
            series.Episodes.Add(episode);
            db.Contents.Add(series);
            await db.SaveChangesAsync();

            using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 32 });
            var catalog = new SqliteLibraryCatalog(db, new UnusedTmdbClient(), new UnusedMetadataService(), cache, new ArtworkPaletteService(db, new TestHttpClientFactory()));
            var artwork = await catalog.GetEpisodeArtworkAsync(episode.Id, CancellationToken.None);

            Assert.NotNull(artwork);
            Assert.Equal("image/jpeg", artwork!.ContentType);
            Assert.Equal(still, (await db.Episodes.SingleAsync(item => item.Id == episode.Id)).StillPath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
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
        public Task<TmdbCreditsResult?> GetMovieCreditsAsync(int tmdbId, CancellationToken cancellationToken = default) => Task.FromResult<TmdbCreditsResult?>(null);
        public Task<TmdbCreditsResult?> GetTvCreditsAsync(int tmdbId, CancellationToken cancellationToken = default) => Task.FromResult<TmdbCreditsResult?>(null);
    }

    private sealed class UnusedMetadataService : IMetadataService
    {
        public Task SaveMetadataToMediaFolderAsync(int contentId, string mediaFolderPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string?> DownloadPosterAsync(string posterPath, string mediaFolderPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string?> DownloadBackdropAsync(string backdropPath, string mediaFolderPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string?> DownloadEpisodeStillAsync(string stillPath, string seasonFolderPath, int seasonNumber, int episodeNumber, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<object?> LoadMetadataFromMediaFolderAsync(string mediaFolderPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TmdbSearchItem?> SearchMovieWithVariationsAsync(string folderName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TmdbSearchItem?> SearchSeriesWithVariationsAsync(string folderName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TmdbSeasonDetails?> FetchSeasonDetailsAsync(int tmdbId, int seasonNumber, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task FetchAndStoreEpisodeMetadataAsync(int contentId, int tmdbId, string seriesFolder, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DownloadSubtitlesAsync(int contentId, string mediaFolderPath, string languageCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
