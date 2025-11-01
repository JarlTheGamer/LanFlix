namespace Lanflix.Infrastructure.Migration.Models;

/// <summary>
/// Result of a migration operation
/// </summary>
public class MigrationResult
{
    /// <summary>
    /// Indicates whether the migration was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if migration failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detailed migration statistics
    /// </summary>
    public MigrationStatistics Statistics { get; set; } = new();

    /// <summary>
    /// List of warnings encountered during migration
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// List of errors encountered during migration
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Duration of the migration operation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Timestamp when migration started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Timestamp when migration completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Statistics about migrated data
/// </summary>
public class MigrationStatistics
{
    public int ContentRecordsRead { get; set; }
    public int ContentRecordsMigrated { get; set; }
    public int ContentRecordsFailed { get; set; }

    public int ProfileRecordsRead { get; set; }
    public int ProfileRecordsMigrated { get; set; }
    public int ProfileRecordsFailed { get; set; }

    public int WatchHistoryRecordsRead { get; set; }
    public int WatchHistoryRecordsMigrated { get; set; }
    public int WatchHistoryRecordsFailed { get; set; }

    public int EpisodeRecordsRead { get; set; }
    public int EpisodeRecordsMigrated { get; set; }
    public int EpisodeRecordsFailed { get; set; }

    public int SettingsRecordsRead { get; set; }
    public int SettingsRecordsMigrated { get; set; }

    public int TotalRecordsRead => ContentRecordsRead + ProfileRecordsRead + 
                                    WatchHistoryRecordsRead + EpisodeRecordsRead + SettingsRecordsRead;
    
    public int TotalRecordsMigrated => ContentRecordsMigrated + ProfileRecordsMigrated + 
                                        WatchHistoryRecordsMigrated + EpisodeRecordsMigrated + SettingsRecordsMigrated;
    
    public int TotalRecordsFailed => ContentRecordsFailed + ProfileRecordsFailed + 
                                      WatchHistoryRecordsFailed + EpisodeRecordsFailed;
}
