using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lanflix.Infrastructure.Persistence;

public sealed class StartupDatabaseMigrator(
    ApplicationDbContext context,
    ILogger<StartupDatabaseMigrator> logger)
{
    public const string BaselineMigrationId = "20260801170003_ModularIdentityBaseline";

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The SQLite connection string is missing.");
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            await context.Database.MigrateAsync(cancellationToken);
            return;
        }

        var databasePath = Path.GetFullPath(dataSource);
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await ExecuteAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken);
        await ExecuteAsync(connection, "PRAGMA busy_timeout = 30000;", cancellationToken);
        await ExecuteAsync(connection, "PRAGMA journal_mode = WAL;", cancellationToken);

        var isLegacy = await TableExistsAsync(connection, "Contents", cancellationToken)
            && !await TableExistsAsync(connection, "__EFMigrationsHistory", cancellationToken);

        if (isLegacy)
        {
            var backupPath = await CreateBackupAsync(connection, databasePath, cancellationToken);
            logger.LogInformation("Created pre-migration database backup at {BackupPath}", backupPath);
            await BaselineLegacyDatabaseAsync(connection, cancellationToken);
        }

        await connection.CloseAsync();
        await context.Database.MigrateAsync(cancellationToken);

        // Ensure CastJson column exists in Contents table
        await using (var castConnection = new SqliteConnection(connectionString))
        {
            await castConnection.OpenAsync(cancellationToken);
            await using (var transaction = await castConnection.BeginTransactionAsync(cancellationToken))
            {
                await EnsureColumnAsync(castConnection, transaction, "Contents", "CastJson", "TEXT NULL", cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            await castConnection.CloseAsync();
        }

        var schema = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).LastOrDefault() ?? BaselineMigrationId;
        logger.LogInformation("SQLite database is at schema {MigrationId}", schema);
    }

    private static async Task<string> CreateBackupAsync(SqliteConnection source, string databasePath, CancellationToken cancellationToken)
    {
        var backupDirectory = Path.Combine(Path.GetDirectoryName(databasePath)!, "backups");
        Directory.CreateDirectory(backupDirectory);
        var backupPath = Path.Combine(backupDirectory, $"lanflix-{DateTime.UtcNow:yyyyMMdd-HHmmss}-pre-v2.db");
        await using var destination = new SqliteConnection($"Data Source={backupPath}");
        await destination.OpenAsync(cancellationToken);
        source.BackupDatabase(destination);
        return backupPath;
    }

    private static async Task BaselineLegacyDatabaseAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await EnsureColumnAsync(connection, transaction, "Episodes", "IntroStartTime", "REAL NULL", cancellationToken);
        await EnsureColumnAsync(connection, transaction, "Episodes", "IntroEndTime", "REAL NULL", cancellationToken);
        await EnsureColumnAsync(connection, transaction, "Episodes", "CreditsStartTime", "REAL NULL", cancellationToken);
        await EnsureColumnAsync(connection, transaction, "Contents", "CollectionId", "INTEGER NULL", cancellationToken);
        await EnsureColumnAsync(connection, transaction, "Contents", "CollectionName", "TEXT NULL", cancellationToken);

        const string identitySchema = """
            CREATE TABLE IF NOT EXISTS Accounts (
                Id TEXT NOT NULL CONSTRAINT PK_Accounts PRIMARY KEY,
                Username TEXT NOT NULL,
                NormalizedUsername TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                Role TEXT NOT NULL,
                IsDisabled INTEGER NOT NULL,
                FailedLoginCount INTEGER NOT NULL,
                LockedUntilUtc TEXT NULL,
                LastLoginAtUtc TEXT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Accounts_NormalizedUsername ON Accounts (NormalizedUsername);

            CREATE TABLE IF NOT EXISTS RefreshSessions (
                Id TEXT NOT NULL CONSTRAINT PK_RefreshSessions PRIMARY KEY,
                AccountId TEXT NOT NULL,
                TokenHash TEXT NOT NULL,
                DeviceName TEXT NOT NULL,
                ExpiresAtUtc TEXT NOT NULL,
                AbsoluteExpiresAtUtc TEXT NOT NULL,
                RevokedAtUtc TEXT NULL,
                ReplacedBySessionId TEXT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NULL,
                CONSTRAINT FK_RefreshSessions_Accounts_AccountId FOREIGN KEY (AccountId) REFERENCES Accounts (Id) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_RefreshSessions_TokenHash ON RefreshSessions (TokenHash);
            CREATE INDEX IF NOT EXISTS IX_RefreshSessions_AccountId_RevokedAtUtc ON RefreshSessions (AccountId, RevokedAtUtc);

            CREATE TABLE IF NOT EXISTS Invitations (
                Id TEXT NOT NULL CONSTRAINT PK_Invitations PRIMARY KEY,
                CodeHash TEXT NOT NULL,
                Role TEXT NOT NULL,
                CreatedByAccountId TEXT NOT NULL,
                ExpiresAtUtc TEXT NOT NULL,
                UsedAtUtc TEXT NULL,
                RevokedAtUtc TEXT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Invitations_CodeHash ON Invitations (CodeHash);

            CREATE TABLE IF NOT EXISTS AuditRecords (
                Id INTEGER NOT NULL CONSTRAINT PK_AuditRecords PRIMARY KEY AUTOINCREMENT,
                AccountId TEXT NULL,
                Action TEXT NOT NULL,
                Subject TEXT NULL,
                IpAddress TEXT NULL,
                DetailsJson TEXT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_AuditRecords_AccountId ON AuditRecords (AccountId);
            CREATE INDEX IF NOT EXISTS IX_AuditRecords_CreatedAtUtc ON AuditRecords (CreatedAtUtc);

            CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
                ProductVersion TEXT NOT NULL
            );
            INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion)
            VALUES ('20260801170003_ModularIdentityBaseline', '10.0.0');
            """;

        await ExecuteAsync(connection, identitySchema, cancellationToken, transaction);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task EnsureColumnAsync(
        SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        string table,
        string column,
        string definition,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, table, cancellationToken, transaction)) return;

        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = $"PRAGMA table_info(\"{table}\");";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase)) return;
        }
        await reader.DisposeAsync();
        await ExecuteAsync(connection, $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition};", cancellationToken, transaction);
    }

    private static async Task<bool> TableExistsAsync(
        SqliteConnection connection,
        string table,
        CancellationToken cancellationToken,
        System.Data.Common.DbTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction?)transaction;
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";
        command.Parameters.AddWithValue("$name", table);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken,
        System.Data.Common.DbTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction?)transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
