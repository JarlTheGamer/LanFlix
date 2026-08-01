namespace Lanflix.SharedKernel;

public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; protected set; }

    protected void MarkUpdated() => UpdatedAtUtc = DateTime.UtcNow;
}
