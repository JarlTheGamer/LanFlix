using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lanflix.Application.Common.DTOs;

namespace Lanflix.WebApi.Tests.Controllers;

public class AppUpdateControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AppUpdateControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLatestAndroidVersion_WithCurrentVersion_ReturnsOkOrNoContent()
    {
        // Arrange
        var currentVersion = "1.0.0";
        var architecture = "arm64-v8a";

        // Act
        var response = await _client.GetAsync(
            $"/api/app-updates/android/latest?currentVersion={currentVersion}&architecture={architecture}");

        // Assert
        // Should return either OK with update info or NoContent if no update available
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<AppUpdateInfo>();
            result.Should().NotBeNull();
            result!.Version.Should().NotBeNullOrEmpty();
            result.VersionCode.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task GetLatestAndroidVersion_WithoutCurrentVersion_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/app-updates/android/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetLatestAndroidVersion_WithEmptyCurrentVersion_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/app-updates/android/latest?currentVersion=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DownloadApk_WithNonExistentVersion_ReturnsNotFound()
    {
        // Arrange
        var version = "99.99.99";
        var architecture = "arm64-v8a";

        // Act
        var response = await _client.GetAsync(
            $"/api/app-updates/android/download/{version}/{architecture}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVersionHistory_ReturnsOk_WithVersionList()
    {
        // Act
        var response = await _client.GetAsync("/api/app-updates/android/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<AppUpdateInfo>>();
        result.Should().NotBeNull();
        // May be empty if no versions are uploaded
    }

    [Fact]
    public async Task UploadApk_WithoutFile_ReturnsBadRequest()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("2.0.0"), "version");
        content.Add(new StringContent("20"), "versionCode");

        // Act
        var response = await _client.PostAsync("/api/app-updates/android/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadApk_WithInvalidVersionCode_ReturnsBadRequest()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        
        // Create a fake APK file
        var fileContent = new ByteArrayContent(new byte[] { 0x50, 0x4B, 0x03, 0x04 }); // ZIP header
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.android.package-archive");
        content.Add(fileContent, "apkFile", "test.apk");
        
        content.Add(new StringContent("2.0.0"), "version");
        content.Add(new StringContent("0"), "versionCode"); // Invalid: must be positive

        // Act
        var response = await _client.PostAsync("/api/app-updates/android/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadApk_WithNonApkFile_ReturnsBadRequest()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        
        // Create a non-APK file
        var fileContent = new ByteArrayContent(new byte[] { 0x00, 0x01, 0x02, 0x03 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "apkFile", "test.txt"); // Not an APK
        
        content.Add(new StringContent("2.0.0"), "version");
        content.Add(new StringContent("20"), "versionCode");

        // Act
        var response = await _client.PostAsync("/api/app-updates/android/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
