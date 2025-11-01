# Lanflix Backend Migration Guide

This guide walks you through migrating from the legacy Node.js/TypeScript backend to the new C# ASP.NET Core backend.

## Overview

The migration process involves:
1. Migrating data from the legacy SQLite database
2. Migrating configuration from .env to appsettings.json
3. Verifying the migration was successful
4. Testing the new backend with migrated data

## Prerequisites

- [ ] .NET 9.0 SDK installed
- [ ] Access to legacy backend database (`lanflix.db`)
- [ ] Access to legacy `.env` file
- [ ] New backend compiled and ready
- [ ] Backup of legacy database created

## Step-by-Step Migration

### Step 1: Prepare for Migration

1. **Stop the legacy backend** to ensure data consistency:
   ```bash
   # Stop the Node.js backend if running
   ```

2. **Create a backup** of the legacy database:
   ```bash
   cp server/backend-old/data/lanflix.db server/backend-old/data/lanflix.db.backup
   ```

3. **Verify legacy database** is accessible:
   ```bash
   ls -lh server/backend-old/data/lanflix.db
   ```

### Step 2: Run Dry-Run Migration

Test the migration without writing data:

```bash
cd server/backend/MigrationTool
dotnet run --legacy-db ../../backend-old/data/lanflix.db --dry-run
```

**Review the output:**
- Check for any errors or warnings
- Verify record counts look correct
- Review the generated report

### Step 3: Migrate Configuration

Migrate the configuration from .env to appsettings.json:

```bash
dotnet run --legacy-db ../../backend-old/data/lanflix.db \
  --legacy-env ../../backend-old/.env \
  --dry-run
```

**Review the generated files:**
- `appsettings.migrated.json` - New configuration format
- `config-migration-report.txt` - Configuration migration details

**Update paths if needed:**
- Media paths may need adjustment for the new system
- Verify all API keys are present

### Step 4: Execute Full Migration

Run the actual migration:

```bash
dotnet run --legacy-db ../../backend-old/data/lanflix.db \
  --legacy-env ../../backend-old/.env
```

**Monitor the progress:**
- Watch for any errors during migration
- Note the final statistics
- Save the migration report for reference

### Step 5: Verify Migration

1. **Check record counts:**
   ```bash
   cd ../WebApi
   dotnet ef dbcontext info
   ```

2. **Review migration report:**
   - Open `migration-report-{timestamp}.txt`
   - Verify all records were migrated
   - Check for warnings or errors

3. **Spot-check data:**
   - Verify a few content items
   - Check profile data
   - Verify watch history

### Step 6: Update Configuration

1. **Copy migrated configuration:**
   ```bash
   cp ../MigrationTool/appsettings.migrated.json ./appsettings.Production.json
   ```

2. **Review and adjust settings:**
   - Update media paths if needed
   - Verify API keys
   - Configure Redis if used
   - Set up transcoding paths

3. **Update connection string** if using PostgreSQL:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=lanflix;Username=user;Password=pass"
     }
   }
   ```

### Step 7: Test New Backend

1. **Start the new backend:**
   ```bash
   cd ../WebApi
   dotnet run
   ```

2. **Test basic functionality:**
   - Browse library items: `GET http://localhost:5000/api/library/items`
   - Get content details: `GET http://localhost:5000/api/library/items/1`
   - List profiles: `GET http://localhost:5000/api/profiles`

3. **Test streaming:**
   - Start a stream session
   - Verify transcoding works
   - Check watch history updates

4. **Test with frontend:**
   - Connect the frontend to the new backend
   - Verify all features work
   - Check for any errors in browser console

### Step 8: Parallel Testing (Optional)

Run both backends simultaneously for comparison:

1. **Start legacy backend** on port 6129
2. **Start new backend** on port 5000
3. **Compare responses** for the same requests
4. **Monitor performance** metrics

### Step 9: Production Cutover

When ready to switch to the new backend:

1. **Schedule maintenance window**
2. **Stop legacy backend**
3. **Run final migration** to capture any new data
4. **Start new backend**
5. **Update frontend configuration** to point to new backend
6. **Monitor for issues**

### Step 10: Post-Migration

1. **Keep legacy backend available** for 24-48 hours as backup
2. **Monitor new backend** for errors or performance issues
3. **Collect user feedback**
4. **Address any issues** that arise
5. **Decommission legacy backend** after stability confirmed

## Troubleshooting

### Migration Fails with "Database Not Found"

**Problem:** Legacy database path is incorrect

**Solution:**
```bash
# Find the correct path
find . -name "lanflix.db"

# Use absolute path
dotnet run --legacy-db /full/path/to/lanflix.db
```

### Migration Fails with "Missing Tables"

**Problem:** Database schema doesn't match expected structure

**Solution:**
- Verify you're using the correct database file
- Check if database is corrupted
- Restore from backup if needed

### Some Records Fail to Migrate

**Problem:** Data validation errors

**Solution:**
```bash
# Use continue-on-error flag
dotnet run --legacy-db ../../backend-old/data/lanflix.db --continue-on-error

# Review the migration report for specific errors
# Fix data in legacy database if needed
```

### Out of Memory During Migration

**Problem:** Large database exceeds available memory

**Solution:**
```bash
# Reduce batch size
dotnet run --legacy-db ../../backend-old/data/lanflix.db --batch-size 50
```

### Configuration Migration Issues

**Problem:** Some configuration values are missing

**Solution:**
1. Review `config-migration-report.txt` for warnings
2. Manually add missing values to `appsettings.json`
3. Check legacy `.env` file for all required values

### New Backend Won't Start

**Problem:** Configuration or database issues

**Solution:**
1. Check `appsettings.json` for syntax errors
2. Verify database connection string
3. Ensure all required API keys are present
4. Check logs for specific error messages

## Rollback Procedure

If you need to rollback to the legacy backend:

1. **Stop new backend**
2. **Restore legacy database** from backup (if modified)
3. **Start legacy backend**
4. **Update frontend** to point back to legacy backend
5. **Investigate issues** before attempting migration again

## Migration Checklist

Use this checklist to track your migration progress:

- [ ] Prerequisites verified
- [ ] Legacy database backed up
- [ ] Dry-run migration successful
- [ ] Configuration migrated and reviewed
- [ ] Full migration executed successfully
- [ ] Migration report reviewed
- [ ] Data integrity verified
- [ ] Configuration updated in new backend
- [ ] New backend tested locally
- [ ] Streaming functionality verified
- [ ] Frontend tested with new backend
- [ ] Parallel testing completed (optional)
- [ ] Production cutover planned
- [ ] Rollback procedure tested
- [ ] Monitoring in place
- [ ] Team trained on new backend

## Performance Expectations

Typical migration times:

| Database Size | Records | Estimated Time |
|--------------|---------|----------------|
| Small        | < 1,000 | < 1 minute     |
| Medium       | 1,000 - 10,000 | 1-5 minutes |
| Large        | 10,000 - 100,000 | 5-30 minutes |
| Very Large   | > 100,000 | 30+ minutes   |

## Support

If you encounter issues:

1. Check the migration report for detailed error messages
2. Review the troubleshooting section above
3. Check logs in both legacy and new backends
4. Verify all prerequisites are met
5. Try dry-run mode to isolate issues

## Best Practices

1. **Always test in a non-production environment first**
2. **Create backups before migration**
3. **Run dry-run to validate data**
4. **Review all warnings in migration report**
5. **Test thoroughly before production cutover**
6. **Keep legacy backend available for rollback**
7. **Monitor new backend closely after cutover**
8. **Document any issues and resolutions**

## Next Steps

After successful migration:

1. Configure monitoring and alerting
2. Set up automated backups
3. Optimize database indexes
4. Configure caching (Redis)
5. Set up hardware acceleration for transcoding
6. Configure SSL/TLS certificates
7. Set up reverse proxy (nginx/IIS)
8. Plan for scaling if needed

## Additional Resources

- [Migration Tool README](MigrationTool/README.md) - Detailed tool documentation
- [Migration Infrastructure README](Infrastructure/Migration/README.md) - Technical details
- [Backend README](README.md) - New backend documentation
- [API Documentation](docs/api/) - API reference
