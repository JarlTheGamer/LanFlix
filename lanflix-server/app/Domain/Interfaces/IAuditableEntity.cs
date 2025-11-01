namespace Lanflix.Domain.Interfaces;

/// <summary>
/// Interface for entities that track creation and modification timestamps
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Timestamp when the entity was created
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the entity was last updated
    /// </summary>
    DateTime? UpdatedAt { get; set; }
}
