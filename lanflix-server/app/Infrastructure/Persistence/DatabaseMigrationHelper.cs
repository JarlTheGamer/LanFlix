using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Persistence;

/// <summary>
/// Helper class for handling database schema updates when not using EF migrations
/// </summary>
public class DatabaseMigrationHelper
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseMigrationHelper> _logger;

    public DatabaseMigrationHelper(ApplicationDbContext context, ILogger<DatabaseMigrationHelper> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Recreate the database with the updated schema
    /// WARNING: This will delete all existing data!
    /// </summary>
    public async Task RecreateDatabase()
    {
        _logger.LogWarning("Recreating database - ALL DATA WILL BE LOST!");
        
        // Delete the database
        await _context.Database.EnsureDeletedAsync();
        _logger.LogInformation("Database deleted");
        
        // Create the database with new schema
        await _context.Database.EnsureCreatedAsync();
        _logger.LogInformation("Database recreated with updated schema");
    }

    /// <summary>
    /// Check if the database needs to be updated (has the old UNIQUE constraint)
    /// </summary>
    public async Task<bool> NeedsDatabaseUpdate()
    {
        try
        {
            // Try to create two content items with same TMDB ID but different types
            // If this fails, we have the old constraint and need to update
            
            var testMovie = new Domain.Entities.Content
            {
                TmdbId = 999999, // Use a test ID that's unlikely to exist
                Type = Domain.Enums.ContentType.Movie,
                Title = "Test Movie",
                FilePath = "/test/movie.mp4",
                AddedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var testSeries = new Domain.Entities.Content
            {
                TmdbId = 999999, // Same TMDB ID
                Type = Domain.Enums.ContentType.Series,
                Title = "Test Series",
                FilePath = "/test/series",
                AddedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Contents.Add(testMovie);
            await _context.SaveChangesAsync();

            _context.Contents.Add(testSeries);
            await _context.SaveChangesAsync();

            // If we get here, the new constraint is working
            // Clean up test data
            _context.Contents.RemoveRange(testMovie, testSeries);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Database schema is up to date");
            return false;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE constraint failed: Contents.TmdbId") == true)
        {
            // The old constraint is still in place
            _logger.LogWarning("Database has old UNIQUE constraint on TmdbId only - needs update");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database schema");
            return false;
        }
    }

    /// <summary>
    /// Manual fix for the UNIQUE constraint issue without recreating the database
    /// This attempts to update the constraint using raw SQL
    /// </summary>
    public async Task FixUniqueConstraint()
    {
        try
        {
            _logger.LogInformation("Attempting to fix UNIQUE constraint using SQL commands");

            // For SQLite, we need to recreate the table to change constraints
            // This is a simplified approach - in production you'd want to backup data first
            
            await _context.Database.ExecuteSqlRawAsync(@"
                -- Create new table with correct constraint
                CREATE TABLE Contents_New (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TmdbId INTEGER NOT NULL,
                    Type INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    OriginalTitle TEXT,
                    Overview TEXT,
                    FilePath TEXT NOT NULL,
                    MediaInfo TEXT,
                    ReleaseDate TEXT,
                    PosterPath TEXT,
                    BackdropPath TEXT,
                    Rating REAL,
                    Genres TEXT,
                    AddedAt TEXT NOT NULL,
                    IsDeleted INTEGER NOT NULL DEFAULT 0,
                    DeletedAt TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    UNIQUE(TmdbId, Type)
                );
            ");

            // Copy data from old table to new table
            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO Contents_New 
                SELECT * FROM Contents;
            ");

            // Drop old table and rename new table
            await _context.Database.ExecuteSqlRawAsync("DROP TABLE Contents;");
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE Contents_New RENAME TO Contents;");

            // Recreate indexes
            await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_Contents_TmdbId ON Contents (TmdbId);");
            await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_Contents_Type ON Contents (Type);");
            await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_Contents_AddedAt ON Contents (AddedAt);");
            await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_Contents_Type_AddedAt ON Contents (Type, AddedAt);");
            await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_Contents_Title ON Contents (Title);");
            await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_Contents_IsDeleted ON Contents (IsDeleted);");

            _logger.LogInformation("Successfully fixed UNIQUE constraint");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fix UNIQUE constraint using SQL");
            throw;
        }
    }
}