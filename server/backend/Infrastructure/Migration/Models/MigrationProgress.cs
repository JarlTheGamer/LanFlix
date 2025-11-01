namespace Lanflix.Infrastructure.Migration.Models;

/// <summary>
/// Represents the progress of a migration operation
/// </summary>
public class MigrationProgress
{
    /// <summary>
    /// Current phase of the migration
    /// </summary>
    public MigrationPhase Phase { get; set; }

    /// <summary>
    /// Current step description
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;

    /// <summary>
    /// Number of items processed in current phase
    /// </summary>
    public int ProcessedItems { get; set; }

    /// <summary>
    /// Total number of items to process in current phase
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Overall percentage complete (0-100)
    /// </summary>
    public double PercentageComplete { get; set; }

    /// <summary>
    /// Any warnings or non-fatal errors encountered
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Phases of the migration process
/// </summary>
public enum MigrationPhase
{
    Initializing,
    ValidatingLegacyDatabase,
    ReadingLegacyData,
    TransformingData,
    ValidatingTransformedData,
    WritingToNewDatabase,
    VerifyingDataIntegrity,
    GeneratingReport,
    Completed,
    Failed
}
