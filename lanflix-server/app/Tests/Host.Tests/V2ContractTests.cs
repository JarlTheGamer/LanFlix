using Xunit;
using Lanflix.Modules.Metadata;
using Lanflix.Modules.Library;

namespace Lanflix.Host.Tests;

public sealed class V2ContractTests
{
    [Fact]
    public void Media_contract_distinguishes_server_availability_from_progress()
    {
        var item = new MediaItemDto(1, 2, "movie", "Test", null, 2026, 8.4, [], null, null, null, true, null, ArtworkPaletteDto.Fallback);
        Assert.True(item.ServerAvailable);
        Assert.Null(item.ProgressPercentage);
    }

    [Fact]
    public void Page_contract_retains_offset_and_limit()
    {
        var page = new PageDto<int>([1, 2], 12, 4, 2);
        Assert.Equal(4, page.Offset);
        Assert.Equal(2, page.Limit);
    }
}
