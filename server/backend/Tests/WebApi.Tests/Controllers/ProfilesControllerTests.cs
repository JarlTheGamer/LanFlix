using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Features.Profiles.Commands.CreateProfile;
using Lanflix.Application.Features.Profiles.Commands.UpdateProfile;

namespace Lanflix.WebApi.Tests.Controllers;

public class ProfilesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProfilesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProfiles_ReturnsOk_WithProfileList()
    {
        // Act
        var response = await _client.GetAsync("/api/profiles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<ProfileDto>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().HaveCountGreaterThanOrEqualTo(2); // We seeded 2 profiles
    }

    [Fact]
    public async Task CreateProfile_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var command = new CreateProfileCommand
        {
            Name = "New Test Profile",
            IsKidsProfile = false,
            Preferences = new Domain.ValueObjects.UserPreferences
            {
                PreferredAudioLanguage = "eng",
                PreferredSubtitleLanguage = "eng",
                AutoPlayNextEpisode = true,
                MaxResolution = "1080p"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/profiles", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ProfileDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Test Profile");
        result.IsKidsProfile.Should().BeFalse();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateProfile_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreateProfileCommand
        {
            Name = "",
            IsKidsProfile = false,
            Preferences = new Domain.ValueObjects.UserPreferences
            {
                PreferredAudioLanguage = "eng",
                PreferredSubtitleLanguage = "eng",
                AutoPlayNextEpisode = true,
                MaxResolution = "1080p"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/profiles", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var profileId = 1;
        var command = new UpdateProfileCommand
        {
            Id = profileId,
            Name = "Updated Profile Name",
            IsKidsProfile = false,
            Preferences = new Domain.ValueObjects.UserPreferences
            {
                PreferredAudioLanguage = "spa",
                PreferredSubtitleLanguage = "spa",
                AutoPlayNextEpisode = false,
                MaxResolution = "4K"
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/profiles/{profileId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ProfileDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(profileId);
        result.Name.Should().Be("Updated Profile Name");
        result.Preferences!.PreferredAudioLanguage.Should().Be("spa");
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = 99999;
        var command = new UpdateProfileCommand
        {
            Id = invalidId,
            Name = "Updated Profile",
            IsKidsProfile = false,
            Preferences = new Domain.ValueObjects.UserPreferences
            {
                PreferredAudioLanguage = "eng",
                PreferredSubtitleLanguage = "eng",
                AutoPlayNextEpisode = true,
                MaxResolution = "1080p"
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/profiles/{invalidId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWatchHistory_WithValidProfileId_ReturnsOk()
    {
        // Arrange
        var profileId = 1;

        // Act
        var response = await _client.GetAsync($"/api/profiles/{profileId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<WatchHistoryDto>>();
        result.Should().NotBeNull();
        // May be empty or contain seeded data
    }

    [Fact]
    public async Task GetWatchHistory_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var profileId = 1;
        var limit = 5;

        // Act
        var response = await _client.GetAsync($"/api/profiles/{profileId}/history?limit={limit}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<WatchHistoryDto>>();
        result.Should().NotBeNull();
        result!.Count.Should().BeLessThanOrEqualTo(limit);
    }

    [Fact]
    public async Task GetWatchHistory_WithInvalidProfileId_ReturnsNotFound()
    {
        // Arrange
        var invalidProfileId = 99999;

        // Act
        var response = await _client.GetAsync($"/api/profiles/{invalidProfileId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWatchlist_WithValidProfileId_ReturnsOk()
    {
        // Arrange
        var profileId = 1;

        // Act
        var response = await _client.GetAsync($"/api/profiles/{profileId}/watchlist");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<ContentDto>>();
        result.Should().NotBeNull();
        // May be empty or contain seeded data
    }

    [Fact]
    public async Task GetWatchlist_WithInvalidProfileId_ReturnsNotFound()
    {
        // Arrange
        var invalidProfileId = 99999;

        // Act
        var response = await _client.GetAsync($"/api/profiles/{invalidProfileId}/watchlist");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
