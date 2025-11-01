namespace Lanflix.Domain.Common;

/// <summary>
/// Base entity class with common properties for all entities
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Timestamp when the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
