using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Features.Library.Commands.ScanLibrary;

namespace Lanflix.WebApi.Tests.Controllers;

public class LibraryControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public LibraryControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLibraryItems_ReturnsOk_WithPaginatedContent()
    {
        // Act
        var response = await _client.GetAsync("/api/library/items?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<ContentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().HaveCountLessThanOrEqualTo(10);
        result.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetLibraryItems_WithTypeFilter_ReturnsFilteredContent()
    {
        // Act
        var response = await _client.GetAsync("/api/library/items?type=Movie&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<ContentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(c => c.Type == Domain.Enums.ContentType.Movie);
    }

    [Fact]
    public async Task GetLibraryItems_WithSearchTerm_ReturnsMatchingContent()
    {
        // Act
        var response = await _client.GetAsync("/api/library/items?searchTerm=Test Movie&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<ContentDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().Contain(c => c.Title.Contains("Test Movie", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetContentDetails_WithValidId_ReturnsContent()
    {
        // Arrange
        var contentId = 1;

        // Act
        var response = await _client.GetAsync($"/api/library/items/{contentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ContentDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(contentId);
        result.Title.Should().NotBeNullOrEmpty();
        result.MediaInfo.Should().NotBeNull();
    }

    [Fact]
    public async Task GetContentDetails_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = 99999;

        // Act
        var response = await _client.GetAsync($"/api/library/items/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveContent_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var contentId = 1;

        // Act
        var response = await _client.DeleteAsync($"/api/library/items/{contentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify content is removed
        var getResponse = await _client.GetAsync($"/api/library/items/{contentId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveContent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = 99999;

        // Act
        var response = await _client.DeleteAsync($"/api/library/items/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ScanLibrary_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var command = new ScanLibraryCommand
        {
            Path = "/test/movies",
            FullScan = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/library/scan", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ScanLibraryResult>();
        result.Should().NotBeNull();
        result!.FilesScanned.Should().BeGreaterThanOrEqualTo(0);
    }
}
