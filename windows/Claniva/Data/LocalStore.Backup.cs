using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Claniva.Data;

public sealed partial class LocalStore
{
    /// <summary>
    /// Create a consistent SQLite backup including any committed WAL transactions.
    /// Never copy the live .db file with File.Copy while SQLite is open.
    /// A failed or corrupt backup blocks the migration; the source remains untouched.
    /// </summary>
    private static void CreateVerifiedPreMigrationBackup(SqliteConnection source, string folder)
    {
        VerifyDatabase(source);
        var backupDirectory = Path.Combine(folder, "backups");
        Directory.CreateDirectory(backupDirectory);
        var filename = "clanarina-pre-v2-" +
            DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture) +
            "-" + Guid.NewGuid().ToString("N") + ".db";
        var path = Path.Combine(backupDirectory, filename);
        using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString());
        destination.Open();
        source.BackupDatabase(destination);
        VerifyDatabase(destination);
    }

    private static void VerifyDatabase(SqliteConnection db)
    {
        using var check = db.CreateCommand();
        check.CommandText = "PRAGMA quick_check;";
        if (!string.Equals(check.ExecuteScalar()?.ToString(), "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Provjera integriteta SQLite baze nije uspjela. Podaci nisu mijenjani.");
    }
}
