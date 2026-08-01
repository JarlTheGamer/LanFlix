using Lanflix.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Modules.Metadata;

public sealed record ArtworkPaletteDto(
    string Base,
    string Depth,
    string Glow,
    string Accent,
    string OnBackground,
    int AlgorithmVersion)
{
    public static ArtworkPaletteDto Fallback { get; } = new("#173B57", "#07131F", "#287CA4", "#F59E0B", "#FFFFFF", 3);
}

public sealed class ArtworkPalette : Entity<int>
{
    private ArtworkPalette() { }

    public int ContentId { get; private set; }
    public string Base { get; private set; } = string.Empty;
    public string Depth { get; private set; } = string.Empty;
    public string Glow { get; private set; } = string.Empty;
    public string Accent { get; private set; } = string.Empty;
    public string OnBackground { get; private set; } = "#FFFFFF";
    public int AlgorithmVersion { get; private set; }
    public long SourceLength { get; private set; }
    public DateTime SourceLastWriteUtc { get; private set; }

    public ArtworkPaletteDto ToDto() => new(Base, Depth, Glow, Accent, OnBackground, AlgorithmVersion);

    public static ArtworkPalette Create(int contentId, ArtworkPaletteDto colors, FileInfo source)
        => new()
        {
            ContentId = contentId,
            Base = colors.Base,
            Depth = colors.Depth,
            Glow = colors.Glow,
            Accent = colors.Accent,
            OnBackground = colors.OnBackground,
            AlgorithmVersion = colors.AlgorithmVersion,
            SourceLength = source.Length,
            SourceLastWriteUtc = source.LastWriteTimeUtc
        };

    public void Replace(ArtworkPaletteDto colors, FileInfo source)
    {
        Base = colors.Base;
        Depth = colors.Depth;
        Glow = colors.Glow;
        Accent = colors.Accent;
        OnBackground = colors.OnBackground;
        AlgorithmVersion = colors.AlgorithmVersion;
        SourceLength = source.Length;
        SourceLastWriteUtc = source.LastWriteTimeUtc;
        MarkUpdated();
    }
}

public interface IArtworkPaletteDbContext
{
    DbSet<ArtworkPalette> ArtworkPalettes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class ArtworkPaletteConfiguration : IEntityTypeConfiguration<ArtworkPalette>
{
    public void Configure(EntityTypeBuilder<ArtworkPalette> builder)
    {
        builder.ToTable("ArtworkPalettes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => x.ContentId).IsUnique();
        builder.Property(x => x.Base).HasMaxLength(9).IsRequired();
        builder.Property(x => x.Depth).HasMaxLength(9).IsRequired();
        builder.Property(x => x.Glow).HasMaxLength(9).IsRequired();
        builder.Property(x => x.Accent).HasMaxLength(9).IsRequired();
        builder.Property(x => x.OnBackground).HasMaxLength(9).IsRequired();
    }
}
