using Lanflix.Infrastructure.Migration;
using Lanflix.Infrastructure.Migration.Models;
using Lanflix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;

namespace Lanflix.MigrationTool;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Display banner
        AnsiConsole.Write(
            new FigletText("Lanflix Migration")
                .Centered()
                .Color(Color.Blue));

        AnsiConsole.MarkupLine("[grey]Migrating from Node.js backend to C# backend[/]");
        AnsiConsole.WriteLine();

        try
        {
            // Parse command line arguments
            var options = ParseArguments(args);

            if (options == null)
            {
                DisplayHelp();
                return 1;
            }

            // Setup dependency injection
            var services = ConfigureServices(options);
            var serviceProvider = services.BuildServiceProvider();

            // Run migration
            using var scope = serviceProvider.CreateScope();
            var migrationOrchestrator = scope.ServiceProvider.GetRequiredService<MigrationOrchestrator>();
            var configMigrator = scope.ServiceProvider.GetRequiredService<ConfigurationMigrator>();

            // Migrate configuration if requested
            if (!string.IsNullOrEmpty(options.LegacyEnvFilePath))
            {
                await MigrateConfigurationAsync(configMigrator, options);
            }

            // Run data migration
            var result = await RunMigrationAsync(migrationOrchestrator, options);

            // Display results
            DisplayResults(result);

            // Save report
            await SaveReportAsync(result, options);

            return result.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    static MigrationOptions? ParseArguments(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();

        var legacyDbPath = config["legacy-db"] ?? config["db"];
        if (string.IsNullOrEmpty(legacyDbPath))
        {
            AnsiConsole.MarkupLine("[red]Error: Legacy database path is required[/]");
            return null;
        }

        return new MigrationOptions
        {
            LegacyDatabasePath = legacyDbPath,
            LegacyEnvFilePath = config["legacy-env"] ?? config["env"],
            DryRun = config["dry-run"] == "true" || config["dryrun"] == "true",
            ContinueOnError = config["continue-on-error"] == "true",
            ValidateFilePaths = config["validate-paths"] != "false",
            CreateBackup = config["no-backup"] != "true",
            BatchSize = int.TryParse(config["batch-size"], out var batchSize) ? batchSize : 100
        };
    }

    static ServiceCollection ConfigureServices(MigrationOptions options)
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Database context
        services.AddDbContext<ApplicationDbContext>(opts =>
        {
            opts.UseSqlite("Data Source=lanflix.db");
        });

        // Migration services
        services.AddScoped<MigrationOrchestrator>();
        services.AddScoped<ConfigurationMigrator>();

        return services;
    }

    static async Task MigrateConfigurationAsync(ConfigurationMigrator configMigrator, MigrationOptions options)
    {
        await AnsiConsole.Status()
            .StartAsync("Migrating configuration...", async ctx =>
            {
                if (string.IsNullOrEmpty(options.LegacyEnvFilePath))
                    return;

                ctx.Status("Reading legacy .env file...");
                var legacyConfig = configMigrator.ReadLegacyEnvFile(options.LegacyEnvFilePath);

                ctx.Status("Transforming configuration...");
                var newConfig = configMigrator.TransformToAppSettings(legacyConfig);

                if (!options.DryRun)
                {
                    ctx.Status("Writing appsettings.json...");
                    var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.migrated.json");
                    configMigrator.WriteAppSettingsFile(newConfig, outputPath);
                    AnsiConsole.MarkupLine($"[green]✓[/] Configuration migrated to: {outputPath}");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Dry run - configuration not written[/]");
                }

                // Generate report
                var report = configMigrator.GenerateConfigurationReport(legacyConfig, newConfig);
                var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "config-migration-report.txt");
                await File.WriteAllTextAsync(reportPath, report);
                AnsiConsole.MarkupLine($"[green]✓[/] Configuration report saved to: {reportPath}");
            });

        AnsiConsole.WriteLine();
    }

    static async Task<MigrationResult> RunMigrationAsync(MigrationOrchestrator orchestrator, MigrationOptions options)
    {
        MigrationResult? result = null;

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[blue]Migrating data[/]");

                var progress = new Progress<MigrationProgress>(p =>
                {
                    task.Description = $"[blue]{p.Phase}[/]: {p.CurrentStep}";
                    if (p.TotalItems > 0)
                    {
                        task.MaxValue = p.TotalItems;
                        task.Value = p.ProcessedItems;
                    }
                });

                result = await orchestrator.ExecuteMigrationAsync(options, progress);
            });

        return result!;
    }

    static void DisplayResults(MigrationResult result)
    {
        AnsiConsole.WriteLine();
        
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(result.Success ? Color.Green : Color.Red);

        table.AddColumn(new TableColumn("[bold]Migration Summary[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Value[/]").Centered());

        table.AddRow("Status", result.Success ? "[green]Success[/]" : "[red]Failed[/]");
        table.AddRow("Duration", result.Duration.ToString(@"hh\:mm\:ss"));
        table.AddRow("Started", result.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        table.AddRow("Completed", result.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Statistics table
        var statsTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue);

        statsTable.AddColumn("[bold]Entity[/]");
        statsTable.AddColumn("[bold]Read[/]");
        statsTable.AddColumn("[bold]Migrated[/]");
        statsTable.AddColumn("[bold]Failed[/]");

        var stats = result.Statistics;
        statsTable.AddRow("Content", stats.ContentRecordsRead.ToString(), 
            stats.ContentRecordsMigrated.ToString(), stats.ContentRecordsFailed.ToString());
        statsTable.AddRow("Profiles", stats.ProfileRecordsRead.ToString(), 
            stats.ProfileRecordsMigrated.ToString(), stats.ProfileRecordsFailed.ToString());
        statsTable.AddRow("Episodes", stats.EpisodeRecordsRead.ToString(), 
            stats.EpisodeRecordsMigrated.ToString(), stats.EpisodeRecordsFailed.ToString());
        statsTable.AddRow("Watch History", stats.WatchHistoryRecordsRead.ToString(), 
            stats.WatchHistoryRecordsMigrated.ToString(), stats.WatchHistoryRecordsFailed.ToString());
        statsTable.AddRow("Settings", stats.SettingsRecordsRead.ToString(), 
            stats.SettingsRecordsMigrated.ToString(), "0");
        statsTable.AddRow("[bold]Total[/]", $"[bold]{stats.TotalRecordsRead}[/]", 
            $"[bold]{stats.TotalRecordsMigrated}[/]", $"[bold]{stats.TotalRecordsFailed}[/]");

        AnsiConsole.Write(statsTable);
        AnsiConsole.WriteLine();

        // Warnings
        if (result.Warnings.Any())
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ {result.Warnings.Count} warnings:[/]");
            foreach (var warning in result.Warnings.Take(10))
            {
                AnsiConsole.MarkupLine($"  [yellow]•[/] {warning}");
            }
            if (result.Warnings.Count > 10)
            {
                AnsiConsole.MarkupLine($"  [grey]... and {result.Warnings.Count - 10} more warnings[/]");
            }
            AnsiConsole.WriteLine();
        }

        // Errors
        if (result.Errors.Any())
        {
            AnsiConsole.MarkupLine($"[red]✗ {result.Errors.Count} errors:[/]");
            foreach (var error in result.Errors.Take(10))
            {
                AnsiConsole.MarkupLine($"  [red]•[/] {error}");
            }
            if (result.Errors.Count > 10)
            {
                AnsiConsole.MarkupLine($"  [grey]... and {result.Errors.Count - 10} more errors[/]");
            }
            AnsiConsole.WriteLine();
        }
    }

    static async Task SaveReportAsync(MigrationResult result, MigrationOptions options)
    {
        var reportPath = Path.Combine(Directory.GetCurrentDirectory(), 
            $"migration-report-{DateTime.UtcNow:yyyyMMddHHmmss}.txt");

        var report = GenerateTextReport(result, options);
        await File.WriteAllTextAsync(reportPath, report);

        AnsiConsole.MarkupLine($"[green]✓[/] Detailed report saved to: {reportPath}");
    }

    static string GenerateTextReport(MigrationResult result, MigrationOptions options)
    {
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("=".PadRight(80, '='));
        report.AppendLine("LANFLIX MIGRATION REPORT");
        report.AppendLine("=".PadRight(80, '='));
        report.AppendLine();
        report.AppendLine($"Migration Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine($"Status: {(result.Success ? "SUCCESS" : "FAILED")}");
        report.AppendLine($"Duration: {result.Duration}");
        report.AppendLine($"Dry Run: {options.DryRun}");
        report.AppendLine();

        report.AppendLine("MIGRATION OPTIONS");
        report.AppendLine("-".PadRight(80, '-'));
        report.AppendLine($"Legacy Database: {options.LegacyDatabasePath}");
        report.AppendLine($"Legacy Env File: {options.LegacyEnvFilePath ?? "Not provided"}");
        report.AppendLine($"Continue on Error: {options.ContinueOnError}");
        report.AppendLine($"Validate File Paths: {options.ValidateFilePaths}");
        report.AppendLine($"Create Backup: {options.CreateBackup}");
        report.AppendLine($"Batch Size: {options.BatchSize}");
        report.AppendLine();

        report.AppendLine("STATISTICS");
        report.AppendLine("-".PadRight(80, '-'));
        var stats = result.Statistics;
        report.AppendLine($"Content:       {stats.ContentRecordsMigrated,6} / {stats.ContentRecordsRead,6} migrated ({stats.ContentRecordsFailed,4} failed)");
        report.AppendLine($"Profiles:      {stats.ProfileRecordsMigrated,6} / {stats.ProfileRecordsRead,6} migrated ({stats.ProfileRecordsFailed,4} failed)");
        report.AppendLine($"Episodes:      {stats.EpisodeRecordsMigrated,6} / {stats.EpisodeRecordsRead,6} migrated ({stats.EpisodeRecordsFailed,4} failed)");
        report.AppendLine($"Watch History: {stats.WatchHistoryRecordsMigrated,6} / {stats.WatchHistoryRecordsRead,6} migrated ({stats.WatchHistoryRecordsFailed,4} failed)");
        report.AppendLine($"Settings:      {stats.SettingsRecordsMigrated,6} / {stats.SettingsRecordsRead,6} migrated");
        report.AppendLine($"TOTAL:         {stats.TotalRecordsMigrated,6} / {stats.TotalRecordsRead,6} migrated ({stats.TotalRecordsFailed,4} failed)");
        report.AppendLine();

        if (result.Warnings.Any())
        {
            report.AppendLine($"WARNINGS ({result.Warnings.Count})");
            report.AppendLine("-".PadRight(80, '-'));
            foreach (var warning in result.Warnings)
            {
                report.AppendLine($"  • {warning}");
            }
            report.AppendLine();
        }

        if (result.Errors.Any())
        {
            report.AppendLine($"ERRORS ({result.Errors.Count})");
            report.AppendLine("-".PadRight(80, '-'));
            foreach (var error in result.Errors)
            {
                report.AppendLine($"  • {error}");
            }
            report.AppendLine();
        }

        if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
        {
            report.AppendLine("FATAL ERROR");
            report.AppendLine("-".PadRight(80, '-'));
            report.AppendLine(result.ErrorMessage);
            report.AppendLine();
        }

        report.AppendLine("=".PadRight(80, '='));
        report.AppendLine("END OF REPORT");
        report.AppendLine("=".PadRight(80, '='));

        return report.ToString();
    }

    static void DisplayHelp()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Usage:[/]");
        AnsiConsole.MarkupLine("  dotnet run --legacy-db <path> [options]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Required Arguments:[/]");
        AnsiConsole.MarkupLine("  --legacy-db <path>        Path to legacy SQLite database");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Optional Arguments:[/]");
        AnsiConsole.MarkupLine("  --legacy-env <path>       Path to legacy .env file");
        AnsiConsole.MarkupLine("  --dry-run                 Validate without migrating data");
        AnsiConsole.MarkupLine("  --continue-on-error       Continue migration even if some records fail");
        AnsiConsole.MarkupLine("  --no-backup               Skip database backup before migration");
        AnsiConsole.MarkupLine("  --batch-size <number>     Number of records per batch (default: 100)");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Examples:[/]");
        AnsiConsole.MarkupLine("  dotnet run --legacy-db ../backend-old/data/lanflix.db");
        AnsiConsole.MarkupLine("  dotnet run --legacy-db ../backend-old/data/lanflix.db --legacy-env ../backend-old/.env");
        AnsiConsole.MarkupLine("  dotnet run --legacy-db ../backend-old/data/lanflix.db --dry-run");
        AnsiConsole.WriteLine();
    }
}
