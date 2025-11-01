using FluentAssertions;
using Lanflix.Application.Common.Exceptions;
using Lanflix.Application.Features.Streaming.Commands.StartStream;
using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Lanflix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Tests.Features.Streaming.Commands;

public class StartStreamCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly StartStreamCommandHandler _handler;

    public StartStreamCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _handler = new StartStreamCommandHandler(_context);
        
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
        var content = CreateTestContent();
        var profile = CreateTestProfile();
        
        _context.Contents.Add(content);
        _context.Profiles.Add(profile);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesStreamSession()
    {
        // Arrange
        var command = new StartStreamCommand
        {
            ContentId = 1,
            ProfileId = 1
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ContentId.Should().Be(1);
        result.ProfileId.Should().Be(1);
        result.Mode.Should().Be(StreamingMode.DirectPlay);
        result.IsActive.Should().BeTrue();
        result.StreamUrl.Should().Contain("/api/stream/");
        
        // Verify session was saved to database
        var session = await _context.StreamSessions.FirstOrDefaultAsync();
        session.Should().NotBeNull();
        session!.ContentId.Should().Be(1);
        session.ProfileId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithInvalidContentId_ThrowsNotFoundException()
    {
        // Arrange
        var command = new StartStreamCommand
        {
            ContentId = 999,
            ProfileId = 1
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Content*999*");
    }

    [Fact]
    public async Task Handle_WithInvalidProfileId_ThrowsNotFoundException()
    {
        // Arrange
        var command = new StartStreamCommand
        {
            ContentId = 1,
            ProfileId = 999
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Profile*999*");
    }

    [Fact]
    public async Task Handle_GeneratesUniqueSessionId()
    {
        // Arrange
        var command = new StartStreamCommand
        {
            ContentId = 1,
            ProfileId = 1
        };

        // Act
        var result1 = await _handler.Handle(command, CancellationToken.None);
        var result2 = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result1.Id.Should().NotBeNullOrEmpty();
        result2.Id.Should().NotBeNullOrEmpty();
        result1.Id.Should().NotBe(result2.Id);
    }

    [Fact]
    public async Task Handle_SetsStartedAtToCurrentTime()
    {
        // Arrange
        var command = new StartStreamCommand
        {
            ContentId = 1,
            ProfileId = 1
        };

        var beforeTime = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        var afterTime = DateTime.UtcNow;

        // Assert
        result.StartedAt.Should().BeOnOrAfter(beforeTime);
        result.StartedAt.Should().BeOnOrBefore(afterTime);
    }

    // Helper methods
    private Content CreateTestContent()
    {
        return new Content
        {
            Id = 1,
            TmdbId = 27205,
            Type = ContentType.Movie,
            Title = "Inception",
            FilePath = "/movies/inception.mkv",
            MediaInfo = new MediaInfo
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
            },
            AddedAt = DateTime.UtcNow
        };
    }

    private Profile CreateTestProfile()
    {
        return new Profile
        {
            Id = 1,
            Name = "Test User",
            IsKidsProfile = false,
            Preferences = new UserPreferences(),
            CreatedAt = DateTime.UtcNow
        };
    }

}
