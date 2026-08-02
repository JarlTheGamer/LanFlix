using System.Security.Cryptography;
using System.Text;
using Lanflix.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Services.Image;

public class ImageCacheService : IImageCacheService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageCacheService> _logger;
    private readonly string _cacheDirectory;

    public ImageCacheService(HttpClient httpClient, ILogger<ImageCacheService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cacheDirectory = Path.Combine(AppContext.BaseDirectory, "config", "cache", "images");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<(byte[] Bytes, string ContentType)?> GetOrFetchImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(imageUrl)));
        var extension = Path.GetExtension(new Uri(imageUrl).AbsolutePath);
        if (string.IsNullOrEmpty(extension)) extension = ".jpg";

        var cachedFilePath = Path.Combine(_cacheDirectory, $"{hash}{extension}");
        var contentType = extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "image/jpeg"
        };

        if (File.Exists(cachedFilePath))
        {
            try
            {
                var cachedBytes = await File.ReadAllBytesAsync(cachedFilePath, cancellationToken);
                return (cachedBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read cached image file {Path}", cachedFilePath);
            }
        }

        try
        {
            _logger.LogInformation("Downloading and caching remote image from {Url}", imageUrl);
            var response = await _httpClient.GetAsync(imageUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await File.WriteAllBytesAsync(cachedFilePath, imageBytes, cancellationToken);
            return (imageBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching image from remote URL {Url}", imageUrl);
            return null;
        }
    }
}
