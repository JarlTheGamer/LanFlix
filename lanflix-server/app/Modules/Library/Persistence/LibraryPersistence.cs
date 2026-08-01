using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lanflix.Modules.Library;

public interface ILibraryDbContext
{
    DbSet<AccountWatchlistItem> AccountWatchlist { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class AccountWatchlistItemConfiguration : IEntityTypeConfiguration<AccountWatchlistItem>
{
    public void Configure(EntityTypeBuilder<AccountWatchlistItem> builder)
    {
        builder.ToTable("AccountWatchlist");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedOnAdd();
        builder.HasIndex(item => new { item.AccountId, item.ContentId }).IsUnique();
        builder.HasIndex(item => new { item.AccountId, item.CreatedAtUtc });
    }
}
