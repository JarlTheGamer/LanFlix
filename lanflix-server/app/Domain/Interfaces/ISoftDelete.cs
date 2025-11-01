namespace Lanflix.Domain.Interfaces;

/// <summary>
/// Interface for entities that support soft deletion
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Indicates whether the entity has been soft deleted
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Timestamp when the entity was soft deleted
    /// </summary>
    DateTime? DeletedAt { get; set; }
}
