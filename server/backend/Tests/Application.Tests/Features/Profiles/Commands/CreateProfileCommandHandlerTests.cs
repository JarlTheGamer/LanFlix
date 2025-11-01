using FluentAssertions;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Application.Features.Profiles.Commands.CreateProfile;
using Lanflix.Domain.Entities;
using Lanflix.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Lanflix.Application.Tests.Features.Profiles.Commands;

public class CreateProfileCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly CreateProfileCommandHandler _handler;

    public CreateProfileCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _cacheService = Substitute.For<ICacheService>();
        _context.Profiles.Returns(Substitute.For<DbSet<Profile>>());
        _handler = new CreateProfileCommandHandler(_context, _cacheService);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesProfile()
    {
        // Arrange
        var command = new CreateProfileCommand
        {
            Name = "Test User",
            AvatarPath = "/avatars/test.png",
            IsKidsProfile = false,
            Preferences = new UserPreferences
            {
                PreferredAudioLanguage = "en",
                PreferredSubtitleLanguage = "en"
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test User");
        result.AvatarPath.Should().Be("/avatars/test.png");
        result.IsKidsProfile.Should().BeFalse();
        result.Preferences.Should().NotBeNull();
        result.Preferences!.PreferredAudioLanguage.Should().Be("en");
        
        _context.Profiles.Received(1).Add(Arg.Is<Profile>(p => p.Name == "Test User"));
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullPreferences_CreatesDefaultPreferences()
    {
        // Arrange
        var command = new CreateProfileCommand
        {
            Name = "Test User",
            IsKidsProfile = false,
            Preferences = null
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Preferences.Should().NotBeNull();
        _context.Profiles.Received(1).Add(Arg.Is<Profile>(p => p.Preferences != null));
    }

    [Fact]
    public async Task Handle_WithKidsProfile_SetsIsKidsProfileTrue()
    {
        // Arrange
        var command = new CreateProfileCommand
        {
            Name = "Kids Profile",
            IsKidsProfile = true
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsKidsProfile.Should().BeTrue();
        _context.Profiles.Received(1).Add(Arg.Is<Profile>(p => p.IsKidsProfile == true));
    }

    [Fact]
    public async Task Handle_SetsCreatedAtToCurrentTime()
    {
        // Arrange
        var command = new CreateProfileCommand
        {
            Name = "Test User",
            IsKidsProfile = false
        };

        var beforeTime = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        var afterTime = DateTime.UtcNow;

        // Assert
        result.CreatedAt.Should().BeOnOrAfter(beforeTime);
        result.CreatedAt.Should().BeOnOrBefore(afterTime);
    }

    [Fact]
    public async Task Handle_InvalidatesProfilesCache()
    {
        // Arrange
        var command = new CreateProfileCommand
        {
            Name = "Test User",
            IsKidsProfile = false
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _cacheService.Received(1).RemoveAsync("profiles:all", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullAvatarPath_CreatesProfileSuccessfully()
    {
        // Arrange
        var command = new CreateProfileCommand
        {
            Name = "Test User",
            AvatarPath = null,
            IsKidsProfile = false
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AvatarPath.Should().BeNull();
        _context.Profiles.Received(1).Add(Arg.Is<Profile>(p => p.AvatarPath == null));
    }

    [Fact]
    public async Task Handle_WithCustomPreferences_PreservesPreferences()
    {
        // Arrange
        var preferences = new UserPreferences
        {
            PreferredAudioLanguage = "es",
            PreferredSubtitleLanguage = "fr",
            AutoPlayNextEpisode = false,
            ForceTranscode = true
        };

        var command = new CreateProfileCommand
        {
            Name = "Test User",
            IsKidsProfile = false,
            Preferences = preferences
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Preferences!.PreferredAudioLanguage.Should().Be("es");
        result.Preferences!.PreferredSubtitleLanguage.Should().Be("fr");
        result.Preferences!.AutoPlayNextEpisode.Should().BeFalse();
        result.Preferences!.ForceTranscode.Should().BeTrue();
    }
}
