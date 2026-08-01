using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;
using Xunit;

namespace Lanflix.Application.Tests;

public sealed class MediaAvailabilityTests
{
    [Fact]
    public void Movie_without_a_file_is_metadata_only()
    {
        var content = new Content { Id = 42, Title = "Metadata only", Type = ContentType.Movie, FilePath = string.Empty };
        Assert.True(string.IsNullOrWhiteSpace(content.FilePath));
    }

    [Fact]
    public void Movie_with_a_file_path_is_server_available_candidate()
    {
        var content = new Content { Id = 42, Title = "Available", Type = ContentType.Movie, FilePath = "media/movie.mkv" };
        Assert.False(string.IsNullOrWhiteSpace(content.FilePath));
    }
}
