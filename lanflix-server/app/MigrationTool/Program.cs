using Lanflix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: Lanflix.MigrationTool <sqlite-database-path>");
    return 2;
}

var databasePath = Path.GetFullPath(args[0]);
var backupPath = $"{databasePath}.{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
if (File.Exists(databasePath)) File.Copy(databasePath, backupPath, overwrite: false);

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlite($"Data Source={databasePath}")
    .Options;
await using var context = new ApplicationDbContext(options);
await context.Database.MigrateAsync();
Console.WriteLine($"Lanflix database migrated. Backup: {backupPath}");
return 0;
