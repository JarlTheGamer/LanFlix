using Lanflix.Modules.Metadata;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Lanflix.Host.Tests;

public sealed class ArtworkPaletteTests
{
    [Fact]
    public async Task Red_and_gold_artwork_produces_distinct_layer_colors()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lanflix-palette-{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new Image<Rgba32>(120, 120))
            {
                image.ProcessPixelRows(rows =>
                {
                    for (var y = 0; y < rows.Height; y++)
                    {
                        var row = rows.GetRowSpan(y);
                        for (var x = 0; x < row.Length; x++)
                            row[x] = x < 80 ? new Rgba32(155, 18, 30) : new Rgba32(235, 170, 45);
                    }
                });
                await image.SaveAsPngAsync(path);
            }

            var palette = await ArtworkPaletteService.AnalyzeAsync(path, CancellationToken.None);

            Assert.NotEqual(palette.Base, palette.Depth);
            Assert.NotEqual(palette.Glow, palette.Accent);
            Assert.Equal(ArtworkPaletteService.CurrentAlgorithmVersion, palette.AlgorithmVersion);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
