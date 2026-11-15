using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Claniva.Data;

/// <summary>
/// Offline-only SQLite store. Schema migration preserves every existing member,
/// charge, payment and identifier. Monetary values are integral cents.
/// </summary>
public sealed partial class LocalStore
{
    private readonly string _connectionString;
    public LocalStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            #if DEMO_BUILD
            "ClanarinaDemo", "data");
#else
            "Clanarina", "data");
#endif
        Directory.CreateDirectory(folder);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(folder, "clanarina.db"),
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
        using var db = Connect();
        using (var journal = db.CreateCommand())
        {
            journal.CommandText = "PRAGMA journal_mode=WAL;";
            journal.ExecuteNonQuery();
        }
        using var tx = db.BeginTransaction();
        Execute(db, tx, """
            CREATE TABLE IF NOT EXISTS members (
                id TEXT PRIMARY KEY, name TEXT NOT NULL, group_name TEXT NOT NULL DEFAULT '',
                email TEXT NOT NULL DEFAULT '', active INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS charges (
                id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id),
                amount_cents INTEGER NOT NULL CHECK(amount_cents > 0), note TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, period TEXT NOT NULL DEFAULT '');
            CREATE TABLE IF NOT EXISTS payments (
                id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id),
                amount_cents INTEGER NOT NULL CHECK(amount_cents > 0), note TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, reference TEXT NOT NULL DEFAULT '');
            """);
        // Older v1 installations did not track schema versions or period/reference.
        if (!HasColumn(db, tx, "charges", "period"))
            Execute(db, tx, "ALTER TABLE charges ADD COLUMN period TEXT NOT NULL DEFAULT '';");
        if (!HasColumn(db, tx, "payments", "reference"))
            Execute(db, tx, "ALTER TABLE payments ADD COLUMN reference TEXT NOT NULL DEFAULT '';");
        Execute(db, tx, """
            CREATE INDEX IF NOT EXISTS ix_charges_member ON charges(member_id);
            CREATE INDEX IF NOT EXISTS ix_payments_member ON payments(member_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_charge_period ON charges(member_id,period)
              WHERE period <> '';
            CREATE UNIQUE INDEX IF NOT EXISTS ux_payment_reference ON payments(member_id,reference)
              WHERE reference <> '';
            CREATE TABLE IF NOT EXISTS audit_events (
                id TEXT PRIMARY KEY, action TEXT NOT NULL, subject_id TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
            PRAGMA user_version=2;
            """);
        tx.Commit();
#if DEMO_BUILD
        DemoData.Seed(_connectionString);
#endif
    }

    private SqliteConnection Connect()
    {
        var db = new SqliteConnection(_connectionString);
        db.Open();
        using var pragma = db.CreateCommand();
        // Opening a read-only screen should never renegotiate the WAL journal mode.
        // Busy timeout avoids sporadic SQLITE_BUSY errors on quick successive edits.
        pragma.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        pragma.ExecuteNonQuery();
        return db;
    }

    private static bool HasColumn(SqliteConnection db, SqliteTransaction tx, string table, string column)
    {
        using var cmd = db.CreateCommand();
        cmd.Transaction = tx;
        // Hardcoded table names only; never interpolate untrusted data.
        cmd.CommandText = "PRAGMA table_info(" + table + ")";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            if (string.Equals(reader.GetString(1), column, StringComparison.Ordinal))
                return true;
        return false;
    }

    private static void Execute(SqliteConnection db, SqliteTransaction tx, string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static void Audit(SqliteConnection db, SqliteTransaction tx, string action, string subject)
    {
        using var cmd = db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "INSERT INTO audit_events(id,action,subject_id) VALUES($id,$action,$subject)";
        cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("$action", action);
        cmd.Parameters.AddWithValue("$subject", subject);
        cmd.ExecuteNonQuery();
    }
}
