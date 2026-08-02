namespace Lanflix.Application.Common.Interfaces;

public interface IImageCacheService
{
    Task<(byte[] Bytes, string ContentType)?> GetOrFetchImageAsync(string imageUrl, CancellationToken cancellationToken = default);
}
