$dbPath = "F:\Programming\Flix\lanflix-server\lanflix.db"

Add-Type -Path "F:\Programming\Flix\lanflix-server\publish\Microsoft.Data.Sqlite.dll"

$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$dbPath")
$conn.Open()

Write-Host "=== Contents table columns ===" -ForegroundColor Cyan
$cmd = $conn.CreateCommand()
$cmd.CommandText = "PRAGMA table_info(Contents)"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    $name = $reader["name"]
    $type = $reader["type"]
    Write-Host "  $name  ($type)"
}
$reader.Close()

Write-Host ""
Write-Host "=== Episodes table columns ===" -ForegroundColor Cyan
$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = "PRAGMA table_info(Episodes)"
$reader2 = $cmd2.ExecuteReader()
while ($reader2.Read()) {
    $name = $reader2["name"]
    $type = $reader2["type"]
    Write-Host "  $name  ($type)"
}
$reader2.Close()

$conn.Close()
