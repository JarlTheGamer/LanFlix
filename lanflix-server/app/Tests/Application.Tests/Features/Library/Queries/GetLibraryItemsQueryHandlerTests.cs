using FluentAssertions;
using Lanflix.Application.Features.Library.Queries.GetLibraryItems;
using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Lanflix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Tests.Features.Library.Queries;

public class GetLibraryItemsQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GetLibraryItemsQueryHandler _handler;

    public GetLibraryItemsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _handler = new GetLibraryItemsQueryHandler(_context);
        
        // Seed test data
        SeedTestData();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SeedTestData()
    {
        var contents = CreateTestContents();
        _context.Contents.AddRange(contents);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllContent()
    {
        // Arrange
        var query = new GetLibraryItemsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithTypeFilter_ReturnsOnlyMatchingType()
    {
        // Arrange
        var query = new GetLibraryItemsQuery
        {
            Type = ContentType.Movie,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().OnlyContain(c => c.Type == ContentType.Movie);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ReturnsMatchingContent()
    {
        // Arrange
        var query = new GetLibraryItemsQuery
        {
            SearchTerm = "Inception",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().Contain(c => c.Title.Contains("Inception"));
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithGenreFilter_ReturnsMatchingContent()
    {
        // Arrange
        var query = new GetLibraryItemsQuery
        {
            Genre = "Action",
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().OnlyContain(c => c.Genres != null && c.Genres.Contains("Action"));
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var query = new GetLibraryItemsQuery
        {
            PageNumber = 2,
            PageSize = 1
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithSortByTitle_ReturnsSortedContent()
    {
        // Arrange
        var query = new GetLibraryItemsQuery
        {
            SortBy = "title",
            SortDescending = false,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeInAscendingOrder(c => c.Title);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithSortByReleaseDate_ReturnsSortedContent()
    {
        // Arrange
        var query = new GetLibraryItemsQuery
        {
            SortBy = "releasedate",
            SortDescending = true,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeInDescendingOrder(c => c.ReleaseDate);
        result.Items.Should().HaveCount(3);
    }

    // Helper methods
    private List<Content> CreateTestContents()
    {
        return new List<Content>
        {
            new Content
            {
                Id = 1,
                TmdbId = 27205,
                Type = ContentType.Movie,
                Title = "Inception",
                FilePath = "/movies/inception.mkv",
                MediaInfo = CreateMediaInfo(),
                ReleaseDate = new DateTime(2010, 7, 16),
                Genres = new[] { "Action", "Sci-Fi" },
                Rating = 8.8,
                AddedAt = DateTime.UtcNow.AddDays(-10),
                Episodes = new List<Episode>()
            },
            new Content
            {
                Id = 2,
                TmdbId = 550,
                Type = ContentType.Movie,
                Title = "Fight Club",
                FilePath = "/movies/fightclub.mkv",
                MediaInfo = CreateMediaInfo(),
                ReleaseDate = new DateTime(1999, 10, 15),
                Genres = new[] { "Drama" },
                Rating = 8.8,
                AddedAt = DateTime.UtcNow.AddDays(-5),
                Episodes = new List<Episode>()
            },
            new Content
            {
                Id = 3,
                TmdbId = 1396,
                Type = ContentType.Series,
                Title = "Breaking Bad",
                FilePath = "/series/breakingbad",
                MediaInfo = CreateMediaInfo(),
                ReleaseDate = new DateTime(2008, 1, 20),
                Genres = new[] { "Drama", "Crime" },
                Rating = 9.5,
                AddedAt = DateTime.UtcNow.AddDays(-1),
                Episodes = new List<Episode>()
            }
        };
    }

    private MediaInfo CreateMediaInfo()
    {
        return new MediaInfo
        {
            Video = new VideoStream
            {
                Codec = "h264",
                Width = 1920,
                Height = 1080,
                Bitrate = 8_000_000,
                FrameRate = 24.0,
                PixelFormat = "yuv420p",
                IsHDR = false
            },
            Audio = new List<AudioStream>
            {
                new AudioStream
                {
                    Index = 0,
                    Codec = "aac",
                    Channels = 2,
                    SampleRate = 48000,
                    Bitrate = 192_000
                }
            },
            Subtitles = new List<SubtitleStream>(),
            Duration = TimeSpan.FromMinutes(120),
            FileSize = 1_000_000_000,
            Container = "mkv",
            OverallBitrate = 8_000_000
        };
    }

}
