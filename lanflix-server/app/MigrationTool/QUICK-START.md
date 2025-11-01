# Migration Tool Quick Start

Get started with migrating your Lanflix backend in 5 minutes.

## Quick Commands

### 1. Dry Run (Recommended First Step)

Test the migration without writing any data:

```bash
cd server/backend/MigrationTool
dotnet run --legacy-db ../../backend-old/data/lanflix.db --dry-run
```

### 2. Full Migration

Migrate both data and configuration:

```bash
dotnet run --legacy-db ../../backend-old/data/lanflix.db \
  --legacy-env ../../backend-old/.env
```

### 3. Data Only Migration

Migrate just the data (no configuration):

```bash
dotnet run --legacy-db ../../backend-old/data/lanflix.db
```

## What Gets Migrated?

✅ **Content** - All movies and TV series with metadata  
✅ **Profiles** - User profiles with default preferences  
✅ **Episodes** - All TV series episodes  
✅ **Watch History** - Playback progress and completion status  
✅ **Settings** - Configuration values  
✅ **Configuration** - .env to appsettings.json transformation  

## Output Files

After migration, you'll find:

- `appsettings.migrated.json` - New configuration file
- `config-migration-report.txt` - Configuration migration details
- `migration-report-{timestamp}.txt` - Detailed migration report
- `lanflix.db.backup.{timestamp}` - Automatic database backup

## Typical Output

```
╔═══════════════════════════════════════════════════════════════╗
║                    Lanflix Migration                          ║
║              Migrating from Node.js backend to C# backend     ║
╚═══════════════════════════════════════════════════════════════╝

✓ Configuration migrated to: appsettings.migrated.json
✓ Configuration report saved to: config-migration-report.txt

[████████████████████████████████████████] 100% Migrating data

╔═══════════════════════════════════════════════════════════════╗
║                    Migration Summary                          ║
╠═══════════════════════════════════════════════════════════════╣
║ Status      │ Success                                         ║
║ Duration    │ 00:00:15                                        ║
║ Started     │ 2024-01-15 10:30:00                            ║
║ Completed   │ 2024-01-15 10:30:15                            ║
╚═══════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════╗
║ Entity        │ Read  │ Migrated │ Failed                     ║
╠═══════════════════════════════════════════════════════════════╣
║ Content       │ 150   │ 150      │ 0                          ║
║ Profiles      │ 3     │ 3        │ 0                          ║
║ Episodes      │ 450   │ 450      │ 0                          ║
║ Watch History │ 89    │ 89       │ 0                          ║
║ Settings      │ 5     │ 5        │ 0                          ║
║ Total         │ 697   │ 697      │ 0                          ║
╚═══════════════════════════════════════════════════════════════╝

✓ Detailed report saved to: migration-report-20240115103015.txt
```

## Common Options

| Option | Description | Example |
|--------|-------------|---------|
| `--dry-run` | Test without writing data | `--dry-run` |
| `--continue-on-error` | Don't stop on validation errors | `--continue-on-error` |
| `--no-backup` | Skip database backup | `--no-backup` |
| `--batch-size` | Records per batch | `--batch-size 50` |

## Next Steps

After successful migration:

1. **Copy configuration:**
   ```bash
   cp appsettings.migrated.json ../WebApi/appsettings.Production.json
   ```

2. **Start new backend:**
   ```bash
   cd ../WebApi
   dotnet run
   ```

3. **Test API:**
   ```bash
   curl http://localhost:5000/api/library/items
   ```

## Need Help?

- 📖 [Full Documentation](README.md)
- 🔧 [Troubleshooting Guide](../MIGRATION-GUIDE.md#troubleshooting)
- 📊 [Migration Report](migration-report-{timestamp}.txt) - Check this first for errors

## Pro Tips

💡 **Always run dry-run first** to catch issues early  
💡 **Review the migration report** for warnings  
💡 **Keep legacy backend running** for 24-48 hours as backup  
💡 **Test thoroughly** before production cutover  
💡 **Create backups** before migration (enabled by default)  

## One-Liner for Production

```bash
dotnet run --legacy-db /path/to/lanflix.db --legacy-env /path/to/.env && \
cp appsettings.migrated.json ../WebApi/appsettings.Production.json && \
cd ../WebApi && dotnet run
```

This will:
1. Migrate data and configuration
2. Copy configuration to WebApi
3. Start the new backend

---

**Ready to migrate?** Start with a dry run! 🚀
