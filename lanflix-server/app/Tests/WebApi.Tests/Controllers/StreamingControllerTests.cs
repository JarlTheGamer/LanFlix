using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Features.Streaming.Commands.StartStream;
using Lanflix.Application.Features.Streaming.Commands.UpdateProgress;
using Lanflix.Domain.ValueObjects;

namespace Lanflix.WebApi.Tests.Controllers;

public class StreamingControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public StreamingControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StartStream_WithValidRequest_ReturnsStreamSession()
    {
        // Arrange
        var contentId = 1;
        var command = new StartStreamCommand
        {
            ContentId = contentId,
            ProfileId = 1,
            ClientCapabilities = new ClientCapabilities
            {
                SupportedVideoCodecs = new[] { "h264", "hevc" },
                SupportedAudioCodecs = new[] { "aac", "mp3" },
                SupportedContainers = new[] { "mp4", "mkv" },
                MaxBitrate = 10_000_000,
                MaxResolution = VideoResolution.HD1080p,
                SupportsHDR = false
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/stream/{contentId}/start", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<StreamSessionDto>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeNullOrEmpty();
        result.ContentId.Should().Be(contentId);
        result.ProfileId.Should().Be(1);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task StartStream_WithInvalidContentId_ReturnsNotFound()
    {
        // Arrange
        var invalidContentId = 99999;
        var command = new StartStreamCommand
        {
            ContentId = invalidContentId,
            ProfileId = 1,
            ClientCapabilities = new ClientCapabilities
            {
                SupportedVideoCodecs = new[] { "h264" },
                SupportedAudioCodecs = new[] { "aac" },
                SupportedContainers = new[] { "mp4" },
                MaxBitrate = 10_000_000,
                MaxResolution = VideoResolution.HD1080p,
                SupportsHDR = false
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/stream/{invalidContentId}/start", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartStream_WithInvalidProfileId_ReturnsNotFound()
    {
        // Arrange
        var contentId = 1;
        var command = new StartStreamCommand
        {
            ContentId = contentId,
            ProfileId = 99999,
            ClientCapabilities = new ClientCapabilities
            {
                SupportedVideoCodecs = new[] { "h264" },
                SupportedAudioCodecs = new[] { "aac" },
                SupportedContainers = new[] { "mp4" },
                MaxBitrate = 10_000_000,
                MaxResolution = VideoResolution.HD1080p,
                SupportsHDR = false
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/stream/{contentId}/start", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProgress_WithValidSession_ReturnsNoContent()
    {
        // Arrange - First create a stream session
        var contentId = 1;
        var startCommand = new StartStreamCommand
        {
            ContentId = contentId,
            ProfileId = 1,
            ClientCapabilities = new ClientCapabilities
            {
                SupportedVideoCodecs = new[] { "h264" },
                SupportedAudioCodecs = new[] { "aac" },
                SupportedContainers = new[] { "mp4" },
                MaxBitrate = 10_000_000,
                MaxResolution = VideoResolution.HD1080p,
                SupportsHDR = false
            }
        };

        var startResponse = await _client.PostAsJsonAsync($"/api/stream/{contentId}/start", startCommand);
        var session = await startResponse.Content.ReadFromJsonAsync<StreamSessionDto>();

        var updateCommand = new UpdateProgressCommand
        {
            SessionId = session!.Id,
            PositionTicks = TimeSpan.FromMinutes(15).Ticks,
            IsCompleted = false
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/stream/{session.Id}/progress", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateProgress_WithInvalidSession_ReturnsNotFound()
    {
        // Arrange
        var invalidSessionId = "invalid-session-id";
        var command = new UpdateProgressCommand
        {
            SessionId = invalidSessionId,
            PositionTicks = TimeSpan.FromMinutes(15).Ticks,
            IsCompleted = false
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/stream/{invalidSessionId}/progress", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StopStream_WithValidSession_ReturnsNoContent()
    {
        // Arrange - First create a stream session
        var contentId = 1;
        var startCommand = new StartStreamCommand
        {
            ContentId = contentId,
            ProfileId = 1,
            ClientCapabilities = new ClientCapabilities
            {
                SupportedVideoCodecs = new[] { "h264" },
                SupportedAudioCodecs = new[] { "aac" },
                SupportedContainers = new[] { "mp4" },
                MaxBitrate = 10_000_000,
                MaxResolution = VideoResolution.HD1080p,
                SupportsHDR = false
            }
        };

        var startResponse = await _client.PostAsJsonAsync($"/api/stream/{contentId}/start", startCommand);
        var session = await startResponse.Content.ReadFromJsonAsync<StreamSessionDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/stream/{session!.Id}/stop");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task StopStream_WithInvalidSession_ReturnsNotFound()
    {
        // Arrange
        var invalidSessionId = "invalid-session-id";

        // Act
        var response = await _client.DeleteAsync($"/api/stream/{invalidSessionId}/stop");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
