using FluentAssertions;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Features.Library.Queries.GetLibraryItems;
using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Lanflix.Application.Tests.Features.Library.Queries;

public class GetLibraryItemsQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly GetLibraryItemsQueryHandler _handler;

    public GetLibraryItemsQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _handler = new GetLibraryItemsQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllContent()
    {
        // Arrange
        var contents = CreateTestContents();
        SetupDbSet(contents);

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
        var contents = CreateTestContents();
        SetupDbSet(contents);

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
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ReturnsMatchingContent()
    {
        // Arrange
        var contents = CreateTestContents();
        SetupDbSet(contents);

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
    }

    [Fact]
    public async Task Handle_WithGenreFilter_ReturnsMatchingContent()
    {
        // Arrange
        var contents = CreateTestContents();
        SetupDbSet(contents);

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
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var contents = CreateTestContents();
        SetupDbSet(contents);

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
    }

    [Fact]
    public async Task Handle_WithSortByTitle_ReturnsSortedContent()
    {
        // Arrange
        var contents = CreateTestContents();
        SetupDbSet(contents);

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
    }

    [Fact]
    public async Task Handle_WithSortByReleaseDate_ReturnsSortedContent()
    {
        // Arrange
        var contents = CreateTestContents();
        SetupDbSet(contents);

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

    private void SetupDbSet(List<Content> contents)
    {
        var mockSet = CreateMockDbSet(contents);
        _context.Contents.Returns(mockSet);
    }

    private DbSet<T> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = Substitute.For<DbSet<T>, IQueryable<T>>();
        
        ((IQueryable<T>)mockSet).Provider.Returns(queryable.Provider);
        ((IQueryable<T>)mockSet).Expression.Returns(queryable.Expression);
        ((IQueryable<T>)mockSet).ElementType.Returns(queryable.ElementType);
        ((IQueryable<T>)mockSet).GetEnumerator().Returns(queryable.GetEnumerator());
        
        return mockSet;
    }
}
