using FluentAssertions;
using Lanflix.Application.Common.Exceptions;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Features.Streaming.Commands.StartStream;
using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;
using Lanflix.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Lanflix.Application.Tests.Features.Streaming.Commands;

public class StartStreamCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly StartStreamCommandHandler _handler;

    public StartStreamCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _handler = new StartStreamCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesStreamSession()
    {
        // Arrange
        var content = CreateTestContent();
        var profile = CreateTestProfile();

        SetupContentDbSet(new List<Content> { content });
        SetupProfileDbSet(new List<Profile> { profile });

        var command = new StartStreamCommand
        {
            ContentId = content.Id,
            ProfileId = profile.Id
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ContentId.Should().Be(content.Id);
        result.ProfileId.Should().Be(profile.Id);
        result.Mode.Should().Be(StreamingMode.DirectPlay);
        result.IsActive.Should().BeTrue();
        result.StreamUrl.Should().Contain("/api/stream/");
        
        _context.StreamSessions.Received(1).Add(Arg.Any<StreamSession>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidContentId_ThrowsNotFoundException()
    {
        // Arrange
        SetupContentDbSet(new List<Content>());
        SetupProfileDbSet(new List<Profile> { CreateTestProfile() });

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
        SetupContentDbSet(new List<Content> { CreateTestContent() });
        SetupProfileDbSet(new List<Profile>());

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
        var content = CreateTestContent();
        var profile = CreateTestProfile();

        SetupContentDbSet(new List<Content> { content });
        SetupProfileDbSet(new List<Profile> { profile });

        var command = new StartStreamCommand
        {
            ContentId = content.Id,
            ProfileId = profile.Id
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
        var content = CreateTestContent();
        var profile = CreateTestProfile();

        SetupContentDbSet(new List<Content> { content });
        SetupProfileDbSet(new List<Profile> { profile });

        var command = new StartStreamCommand
        {
            ContentId = content.Id,
            ProfileId = profile.Id
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

    private void SetupContentDbSet(List<Content> contents)
    {
        var mockSet = CreateMockDbSet(contents);
        _context.Contents.Returns(mockSet);
    }

    private void SetupProfileDbSet(List<Profile> profiles)
    {
        var mockSet = CreateMockDbSet(profiles);
        _context.Profiles.Returns(mockSet);
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
