using Lanflix.SharedKernel;

namespace Lanflix.Modules.Library;

public sealed class AccountWatchlistItem : Entity<long>
{
    private AccountWatchlistItem() { }
    public Guid AccountId { get; private set; }
    public int ContentId { get; private set; }

    public static AccountWatchlistItem Create(Guid accountId, int contentId) => new()
    {
        AccountId = accountId,
        ContentId = contentId
    };
}
