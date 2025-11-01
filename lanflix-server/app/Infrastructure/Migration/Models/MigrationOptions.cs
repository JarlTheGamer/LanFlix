namespace Lanflix.Infrastructure.Migration.Models;

/// <summary>
/// Options for configuring the migration process
/// </summary>
public class MigrationOptions
{
    /// <summary>
    /// Path to the legacy SQLite database file
    /// </summary>
    public string LegacyDatabasePath { get; set; } = string.Empty;

    /// <summary>
    /// Path to the legacy .env configuration file
    /// </summary>
    public string? LegacyEnvFilePath { get; set; }

    /// <summary>
    /// If true, performs validation without actually migrating data
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// If true, continues migration even if some records fail validation
    /// </summary>
    public bool ContinueOnError { get; set; }

    /// <summary>
    /// If true, validates file paths and accessibility
    /// </summary>
    public bool ValidateFilePaths { get; set; } = true;

    /// <summary>
    /// If true, creates a backup of the new database before migration
    /// </summary>
    public bool CreateBackup { get; set; } = true;

    /// <summary>
    /// Maximum number of records to process in a single batch
    /// </summary>
    public int BatchSize { get; set; } = 100;
}
