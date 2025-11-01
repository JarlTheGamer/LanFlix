# Lanflix Migration Tool

A command-line tool for migrating data from the legacy Node.js/TypeScript backend to the new C# ASP.NET Core backend.

## Features

- **Data Migration**: Migrates all content, profiles, watch history, episodes, and settings from the legacy SQLite database
- **Configuration Migration**: Transforms legacy .env configuration to new appsettings.json format
- **Dry Run Mode**: Validate migration without actually writing data
- **Progress Reporting**: Real-time progress updates with detailed statistics
- **Error Handling**: Continues migration even if some records fail (optional)
- **Automatic Backup**: Creates database backup before migration (optional)
- **Detailed Reports**: Generates comprehensive migration reports

## Prerequisites

- .NET 9.0 SDK
- Access to legacy backend database (lanflix.db)
- Access to legacy .env file (optional, for configuration migration)

## Installation

```bash
cd server/backend/MigrationTool
dotnet restore
```

## Usage

### Basic Migration

Migrate data from legacy database:

```bash
dotnet run --legacy-db ../backend-old/data/lanflix.db
```

### Migration with Configuration

Migrate both data and configuration:

```bash
dotnet run --legacy-db ../backend-old/data/lanflix.db --legacy-env ../backend-old/.env
```

### Dry Run

Validate migration without writing data:

```bash
dotnet run --legacy-db ../backend-old/data/lanflix.db --dry-run
```

### Advanced Options

```bash
dotnet run --legacy-db ../backend-old/data/lanflix.db \
  --legacy-env ../backend-old/.env \
  --continue-on-error \
  --batch-size 50
```

## Command Line Arguments

### Required

- `--legacy-db <path>` - Path to the legacy SQLite database file

### Optional

- `--legacy-env <path>` - Path to the legacy .env configuration file
- `--dry-run` - Validate migration without actually writing data to the database
- `--continue-on-error` - Continue migration even if some records fail validation
- `--no-backup` - Skip creating a backup of the new database before migration
- `--batch-size <number>` - Number of records to process in each batch (default: 100)

## Migration Process

The tool performs the following steps:

1. **Initialization**: Validates inputs and creates database backup (if enabled)
2. **Legacy Database Validation**: Verifies legacy database accessibility and schema
3. **Reading Legacy Data**: Reads all data from the legacy database
4. **Data Transformation**: Transforms legacy data to new schema format
5. **Validation**: Validates transformed data for consistency and referential integrity
6. **Writing to New Database**: Writes transformed data to the new database (skipped in dry-run mode)
7. **Data Integrity Verification**: Verifies that all data was written correctly
8. **Report Generation**: Generates detailed migration report

## Output Files

The tool generates the following files:

- `appsettings.migrated.json` - Transformed configuration (if --legacy-env provided)
- `config-migration-report.txt` - Configuration migration report
- `migration-report-{timestamp}.txt` - Detailed data migration report

## Migration Report

The migration report includes:

- Migration status (success/failure)
- Duration and timestamps
- Statistics for each entity type (content, profiles, episodes, watch history, settings)
- List of warnings encountered
- List of errors encountered
- Detailed error messages for failed records

## Examples

### Example 1: Full Migration

```bash
dotnet run --legacy-db D:/Projects/lanflix/server/backend-old/data/lanflix.db \
  --legacy-env D:/Projects/lanflix/server/backend-old/.env
```

### Example 2: Dry Run Validation

```bash
dotnet run --legacy-db ../backend-old/data/lanflix.db --dry-run
```

### Example 3: Migration with Error Tolerance

```bash
dotnet run --legacy-db ../backend-old/data/lanflix.db \
  --continue-on-error \
  --batch-size 200
```

## Troubleshooting

### Database Not Found

If you see "Legacy database file not found", verify the path:

```bash
# Check if file exists
ls ../backend-old/data/lanflix.db
```

### Missing Tables

If validation fails with "Missing required tables", ensure you're using the correct legacy database file.

### Permission Errors

Ensure you have read permissions for the legacy database and write permissions for the current directory.

### Out of Memory

If migrating a large database, try reducing the batch size:

```bash
dotnet run --legacy-db ../backend-old/data/lanflix.db --batch-size 50
```

## Rollback

If migration fails or produces unexpected results:

1. The tool automatically creates a backup before migration (unless --no-backup is used)
2. Backup file is named: `lanflix.db.backup.{timestamp}`
3. To rollback, simply restore the backup file

## Data Transformation Details

### Content

- Maps legacy content types ('movie', 'series') to new enum
- Converts vote_average (decimal) to rating (double)
- Parses comma-separated genres string to array
- Preserves all metadata (title, overview, release date, etc.)

### Profiles

- Creates default user preferences for each profile
- Legacy avatar colors are not migrated (new system uses avatar images)
- First profile is marked as default

### Watch History

- Converts progress from seconds to ticks (1 tick = 100 nanoseconds)
- Calculates watched percentage based on duration
- Preserves completion status and last watched timestamp

### Episodes

- Preserves season and episode numbers
- Links to parent content (series)
- Maintains file paths and metadata

### Settings

- Stored for reference but not directly migrated
- Configuration migration handles settings transformation

## Performance

- Typical migration speed: ~1000 records per second
- Batch processing prevents memory issues with large databases
- Progress updates every batch

## Support

For issues or questions:

1. Check the migration report for detailed error messages
2. Run with --dry-run to validate before actual migration
3. Review the generated reports for warnings and errors
