using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Lanflix.Modules.Metadata;

public sealed class ArtworkPaletteService(IArtworkPaletteDbContext db, IHttpClientFactory httpClientFactory)
{
    public const int CurrentAlgorithmVersion = 5;

    public async Task<ArtworkPaletteDto> GetOrCreateAsync(int contentId, string? mediaPath, CancellationToken cancellationToken)
        => await GetOrCreateAsync(contentId, mediaPath, null, cancellationToken);

    public async Task<ArtworkPaletteDto> GetOrCreateAsync(
        int contentId,
        string? mediaPath,
        string? artworkReference,
        CancellationToken cancellationToken)
    {
        var artworkPath = ResolveArtworkPath(mediaPath, artworkReference)
            ?? await DownloadRemoteArtworkAsync(contentId, artworkReference, cancellationToken);
        if (artworkPath is null) return DeterministicFallback(contentId);

        var source = new FileInfo(artworkPath);
        var stored = await db.ArtworkPalettes.SingleOrDefaultAsync(x => x.ContentId == contentId, cancellationToken);
        if (stored is not null
            && stored.AlgorithmVersion == CurrentAlgorithmVersion
            && stored.SourceLength == source.Length
            && stored.SourceLastWriteUtc == source.LastWriteTimeUtc)
            return stored.ToDto();

        var colors = await AnalyzeAsync(artworkPath, cancellationToken);
        if (stored is null)
            db.ArtworkPalettes.Add(ArtworkPalette.Create(contentId, colors, source));
        else
            stored.Replace(colors, source);
        await db.SaveChangesAsync(cancellationToken);
        return colors;
    }

    public static async Task<ArtworkPaletteDto> AnalyzeAsync(string path, CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync<Rgba32>(path, cancellationToken);
        image.Mutate(operation => operation.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(96, 96),
            Sampler = KnownResamplers.Bicubic
        }));

        var buckets = new Dictionary<int, ColorBucket>();
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    if (pixel.A < 160) continue;
                    var color = Rgb.From(pixel.R, pixel.G, pixel.B);
                    var luminance = color.RelativeLuminance;
                    if (luminance < 0.015 || luminance > 0.97) continue;
                    var key = (pixel.R >> 4) << 8 | (pixel.G >> 4) << 4 | pixel.B >> 4;
                    if (!buckets.TryGetValue(key, out var bucket)) buckets[key] = bucket = new ColorBucket();
                    bucket.Add(color);
                }
            }
        });

        var candidates = buckets.Values
            .Where(x => x.Count >= 2)
            .Select(x => x.Average)
            .OrderByDescending(x => (x.Saturation > 0.25 ? x.Saturation * 3.0 : x.Saturation) + Math.Log(x.Weight))
            .Take(24)
            .ToArray();
        if (candidates.Length == 0) return ArtworkPaletteDto.Fallback;

        // Select signature color: pick the most vibrant swatch with good presence
        var signature = candidates.MaxBy(x => (x.Saturation > 0.25 ? x.Saturation * 3.0 : x.Saturation) + Math.Min(1.0, x.Weight / 100d));

        // Ultra-vibrant Plex-style color tuning matching client-side extraction:
        var baseColor = signature.WithLightness(Math.Clamp(signature.Lightness * 0.60, 0.22, 0.28)).WithSaturation(Math.Clamp(signature.Saturation * 1.2, 0.65, 0.85));
        var depth = signature.WithLightness(Math.Clamp(signature.Lightness * 0.35, 0.12, 0.16)).WithSaturation(Math.Clamp(signature.Saturation * 1.0, 0.55, 0.75));
        var glowColor = signature.WithLightness(Math.Clamp(signature.Lightness * 1.3, 0.42, 0.65)).WithSaturation(Math.Max(0.78, signature.Saturation));
        var accentColor = signature.RotateHue(25).WithLightness(Math.Clamp(signature.Lightness * 1.5, 0.52, 0.78)).WithSaturation(Math.Max(0.75, signature.Saturation));

        return new ArtworkPaletteDto(
            baseColor.Hex,
            depth.Hex,
            glowColor.Hex,
            accentColor.Hex,
            "#FFFFFF",
            CurrentAlgorithmVersion);
    }

    private static ArtworkPaletteDto DeterministicFallback(int contentId)
    {
        var hue = Math.Abs(contentId * 47 % 360);
        var seed = Rgb.FromHsl(hue, 0.28, 0.10);
        return new(seed.Hex, seed.WithLightness(0.04).Hex, seed.RotateHue(34).WithLightness(0.35).Hex,
            seed.RotateHue(155).WithLightness(0.55).WithSaturation(0.65).Hex, "#FFFFFF", CurrentAlgorithmVersion);
    }

    private static string? ResolveArtworkPath(string? mediaPath, string? artworkReference)
    {
        if (!string.IsNullOrWhiteSpace(artworkReference) && File.Exists(artworkReference)) return artworkReference;
        if (string.IsNullOrWhiteSpace(mediaPath)) return null;
        var folder = Directory.Exists(mediaPath) ? mediaPath : Path.GetDirectoryName(mediaPath);
        if (string.IsNullOrWhiteSpace(folder)) return null;
        foreach (var name in new[] { "backdrop.jpg", "backdrop.png", "poster.jpg", "poster.png" })
        {
            var candidate = Path.Combine(folder, name);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private async Task<string?> DownloadRemoteArtworkAsync(int contentId, string? artworkReference, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(artworkReference, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.Equals(uri.Host, "image.tmdb.org", StringComparison.OrdinalIgnoreCase))
            return null;

        var cacheDirectory = Path.Combine(AppContext.BaseDirectory, "cache", "palette-sources");
        Directory.CreateDirectory(cacheDirectory);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uri.AbsoluteUri))).ToLowerInvariant();
        var path = Path.Combine(cacheDirectory, $"{contentId}-{fingerprint[..16]}.img");
        if (File.Exists(path)) return path;

        var temporaryPath = path + ".tmp";
        try
        {
            using var response = await httpClientFactory.CreateClient("artwork-palettes")
                .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > 8_388_608) return null;

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destination = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                total += read;
                if (total > 8_388_608) return null;
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
            await destination.FlushAsync(cancellationToken);
            File.Move(temporaryPath, path, false);
            return path;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            return null;
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static double Distance(Rgb left, Rgb right)
    {
        var hueDistance = Math.Abs(left.Hue - right.Hue);
        hueDistance = Math.Min(hueDistance, 360 - hueDistance) / 180;
        return hueDistance * 0.55 + Math.Abs(left.Saturation - right.Saturation) * 0.25 + Math.Abs(left.Lightness - right.Lightness) * 0.2;
    }

    private sealed class ColorBucket
    {
        private double _red;
        private double _green;
        private double _blue;
        public int Count { get; private set; }
        public void Add(Rgb color) { _red += color.Red; _green += color.Green; _blue += color.Blue; Count++; }
        public Rgb Average => new(_red / Count, _green / Count, _blue / Count, Count);
    }

    private readonly record struct Rgb(double Red, double Green, double Blue, int Weight = 1)
    {
        public static Rgb From(byte red, byte green, byte blue) => new(red / 255d, green / 255d, blue / 255d);
        public static Rgb FromHsl(double hue, double saturation, double lightness)
        {
            var chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            var section = hue / 60;
            var component = chroma * (1 - Math.Abs(section % 2 - 1));
            var (r, g, b) = section switch
            {
                < 1 => (chroma, component, 0d),
                < 2 => (component, chroma, 0d),
                < 3 => (0d, chroma, component),
                < 4 => (0d, component, chroma),
                < 5 => (component, 0d, chroma),
                _ => (chroma, 0d, component)
            };
            var match = lightness - chroma / 2;
            return new(r + match, g + match, b + match);
        }

        public double Max => Math.Max(Red, Math.Max(Green, Blue));
        public double Min => Math.Min(Red, Math.Min(Green, Blue));
        public double Lightness => (Max + Min) / 2;
        public double Saturation => Max == Min ? 0 : (Max - Min) / (1 - Math.Abs(2 * Lightness - 1));
        public double Hue
        {
            get
            {
                var delta = Max - Min;
                if (delta == 0) return 0;
                var hue = Max == Red ? 60 * (((Green - Blue) / delta) % 6)
                    : Max == Green ? 60 * ((Blue - Red) / delta + 2)
                    : 60 * ((Red - Green) / delta + 4);
                return hue < 0 ? hue + 360 : hue;
            }
        }
        public double RelativeLuminance => 0.2126 * Linear(Red) + 0.7152 * Linear(Green) + 0.0722 * Linear(Blue);
        public double Score => Weight * (0.55 + Saturation * 0.9) * (0.6 + Math.Min(0.65, Lightness));
        public string Hex => $"#{(int)Math.Round(Math.Clamp(Red, 0, 1) * 255):X2}{(int)Math.Round(Math.Clamp(Green, 0, 1) * 255):X2}{(int)Math.Round(Math.Clamp(Blue, 0, 1) * 255):X2}";
        public Rgb WithLightness(double value) => FromHsl(Hue, Saturation, value);
        public Rgb WithSaturation(double value) => FromHsl(Hue, Math.Clamp(value, 0, 1), Lightness);
        public Rgb RotateHue(double degrees) => FromHsl((Hue + degrees) % 360, Saturation, Lightness);
        public double Contrast(Rgb other) => (Math.Max(RelativeLuminance, other.RelativeLuminance) + 0.05) / (Math.Min(RelativeLuminance, other.RelativeLuminance) + 0.05);
        private static double Linear(double value) => value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static readonly Rgb ColorWhite = new(1, 1, 1);
}

public static class MetadataModuleServiceCollectionExtensions
{
    public static IServiceCollection AddMetadataModule(this IServiceCollection services)
    {
        services.AddScoped<ArtworkPaletteService>();
        return services;
    }
}
