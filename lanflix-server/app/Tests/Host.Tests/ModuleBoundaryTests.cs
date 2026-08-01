using Lanflix.Modules.Identity;
using Lanflix.Modules.Library;
using Lanflix.Modules.Metadata;
using Lanflix.Modules.Music;
using Lanflix.Modules.LiveTV;
using Lanflix.Modules.Social;
using Xunit;

namespace Lanflix.Host.Tests;

public sealed class ModuleBoundaryTests
{
    [Fact]
    public void Product_modules_do_not_reference_legacy_layers()
    {
        var forbidden = new HashSet<string>
        {
            "Lanflix.Application",
            "Lanflix.Domain",
            "Lanflix.Infrastructure"
        };
        var modules = new[]
        {
            typeof(IdentityEndpoints).Assembly,
            typeof(LibraryEndpoints).Assembly,
            typeof(ArtworkPaletteService).Assembly,
            typeof(MusicEndpoints).Assembly,
            typeof(LiveTvEndpoints).Assembly,
            typeof(SocialModule).Assembly
        };

        foreach (var module in modules)
        {
            var violations = module.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .Where(name => name is not null && forbidden.Contains(name))
                .ToArray();
            Assert.True(violations.Length == 0,
                $"{module.GetName().Name} references legacy layers: {string.Join(", ", violations)}");
        }
    }
}
