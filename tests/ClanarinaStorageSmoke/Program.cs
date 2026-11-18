using Claniva.Data;
using Microsoft.Data.Sqlite;

var root = Path.Combine(Path.GetTempPath(), "clanarina-storage-test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);

long Count(string path, string query)
{
    using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
    {
        DataSource = path,
        Mode = SqliteOpenMode.ReadOnly,
        Pooling = false
    }.ToString());
    connection.Open();
    using var cmd = connection.CreateCommand();
    cmd.CommandText = query;
    return Convert.ToInt64(cmd.ExecuteScalar());
}

try
{
    var data = Path.Combine(root, "legacy");
    Directory.CreateDirectory(data);
    var dbFile = Path.Combine(data, "clanarina.db");
    using (var connection = new SqliteConnection("Data Source=" + dbFile))
    {
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            PRAGMA user_version=1;
            CREATE TABLE members(id TEXT PRIMARY KEY, name TEXT NOT NULL,
                group_name TEXT NOT NULL DEFAULT '', email TEXT NOT NULL DEFAULT '',
                active INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE charges(id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id),
                amount_cents INTEGER NOT NULL CHECK(amount_cents>0),
                note TEXT NOT NULL DEFAULT '', created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
            CREATE TABLE payments(id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id),
                amount_cents INTEGER NOT NULL CHECK(amount_cents>0),
                note TEXT NOT NULL DEFAULT '', created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
            INSERT INTO members(id,name) VALUES('m1','Demo član za migraciju');
            INSERT INTO charges(id,member_id,amount_cents) VALUES('c1','m1',2500);
            INSERT INTO payments(id,member_id,amount_cents) VALUES('p1','m1',1000);
            """;
        cmd.ExecuteNonQuery();
    }

    var migrated = new LocalStore(data);
    if (migrated.Members().Count != 1 || migrated.Members()[0].DebtCents != 1500)
        throw new Exception("Migration changed member rows or financial balances");
    if (Count(dbFile, "PRAGMA user_version") != 2)
        throw new Exception("Schema version was not upgraded to 2");
    if (Count(dbFile, "SELECT COUNT(*) FROM charges WHERE period=''") != 1 ||
        Count(dbFile, "SELECT COUNT(*) FROM payments WHERE reference=''") != 1)
        throw new Exception("Legacy entries lost their expected default fields");

    var backups = Directory.GetFiles(Path.Combine(data, "backups"), "*.db");
    if (backups.Length != 1)
        throw new Exception("Expected exactly one pre-migration SQLite backup");
    if (Count(backups[0], "PRAGMA user_version") != 1 ||
        Count(backups[0], "SELECT SUM(amount_cents) FROM charges") != 2500 ||
        Count(backups[0], "SELECT SUM(amount_cents) FROM payments") != 1000)
        throw new Exception("Backup does not contain original financial data");
    _ = new LocalStore(data);
    if (Directory.GetFiles(Path.Combine(data, "backups"), "*.db").Length != 1)
        throw new Exception("Reopening an already migrated database created duplicate backups");

    // Restore the snapshot into a second folder, then exercise the migration again.
    var restored = Path.Combine(root, "restored");
    Directory.CreateDirectory(restored);
    File.Copy(backups[0], Path.Combine(restored, "clanarina.db"));
    var recovered = new LocalStore(restored);
    if (recovered.Members().Single().DebtCents != 1500)
        throw new Exception("Restored migration did not retain original balances");

    // Future schemas must never be downgraded or overwritten.
    var newer = Path.Combine(root, "newer");
    Directory.CreateDirectory(newer);
    var futureFile = Path.Combine(newer, "clanarina.db");
    using (var connection = new SqliteConnection("Data Source=" + futureFile))
    {
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA user_version=3;";
        cmd.ExecuteNonQuery();
    }
    try
    {
        _ = new LocalStore(newer);
        throw new Exception("Newer database version was silently accepted");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("novijoj verziji"))
    {
    }
    if (Count(futureFile, "PRAGMA user_version") != 3)
        throw new Exception("Future-schema database was modified");
    // Dashboard graphs must reflect actual recorded payments and member-level debt,
    // not synthetic percentage values from a marketing mockup.
    var example = new Summary(2, 10000, 8000, 3500, 1500);
    if (Math.Abs(DashboardAnalytics.CoveredPercent(example) - 65.0) > 0.001)
        throw new Exception("Debt coverage did not respect per-member outstanding balances");
    if (DashboardAnalytics.CoveredPercent(new Summary(0, 0, 0, 0, 0)) != 0 ||
        DashboardAnalytics.CoveredPercent(new Summary(1, 5000, 9000, 0, 4000)) != 100)
        throw new Exception("Zero-charge or overpayment status incorrect");
    var clock = new DateTime(2026, 11, 18);
    var samples = new[]
    {
        new LedgerEntry("p1", "m1", "Test", 2500, "", "", "2026-11-10 13:20:00", "payment"),
        new LedgerEntry("p2", "m1", "Test", 1500, "", "", "2026-10-03 10:05:00", "payment"),
        new LedgerEntry("c1", "m1", "Test", 9000, "", "", "2026-11-12 00:00:00", "charge"),
        new LedgerEntry("p3", "m1", "Test", 900, "", "", "2026-03-01 00:00:00", "payment")
    };
    var months = DashboardAnalytics.LastSixMonths(samples, clock);
    if (months.Count != 6 || months[5].Cents != 2500 ||
        months[4].Cents != 1500 || months.Take(4).Any(x => x.Cents != 0))
        throw new Exception("Six-month monthly payment aggregation is incorrect");
    Console.WriteLine("PASS: storage restore and real-data dashboard aggregation / debt coverage");
}
finally
{
    // Isolated test data only. Never touch the real LocalAppData database.
    try { Directory.Delete(root, recursive: true); } catch (IOException) { }
}
