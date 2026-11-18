using Claniva.Data;
using Microsoft.Data.Sqlite;

var root = Path.Combine(Path.GetTempPath(), "clanarina-demo-smoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = Path.Combine(root, "clanarina-demo-test.db")
}.ToString();

try
{
    // Use the same EUR formatter as the distributed Windows application.
    if (LocalStore.Money(2500) != "25,00 €" ||
        LocalStore.Money(123456) != "1.234,56 €" ||
        LocalStore.Money(0).Contains("HRK", StringComparison.OrdinalIgnoreCase))
        throw new Exception("Money must use Croatian formatting and an explicit EUR symbol");

    using (var db = new SqliteConnection(connectionString))
    {
        db.Open();
        using var setup = db.CreateCommand();
        setup.CommandText = """
            CREATE TABLE members (
                id TEXT PRIMARY KEY, name TEXT NOT NULL, group_name TEXT NOT NULL,
                email TEXT NOT NULL, active INTEGER NOT NULL);
            CREATE TABLE charges (
                id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id),
                amount_cents INTEGER NOT NULL CHECK(amount_cents>0),
                note TEXT NOT NULL, created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                period TEXT NOT NULL DEFAULT '');
            CREATE TABLE payments (
                id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id),
                amount_cents INTEGER NOT NULL CHECK(amount_cents>0),
                note TEXT NOT NULL, created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                reference TEXT NOT NULL DEFAULT '');
            """;
        setup.ExecuteNonQuery();
    }
    DemoData.Seed(connectionString);
    DemoData.Seed(connectionString); // Idempotent after first launch.
    using var testDb = new SqliteConnection(connectionString);
    testDb.Open();

    long Scalar(string sql)
    {
        using var cmd = testDb.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
    }

    if (Scalar("SELECT COUNT(*) FROM members") != 24)
        throw new Exception("Demo must have exactly 24 fictional members");
    if (Scalar("SELECT COUNT(*) FROM charges") != 72)
        throw new Exception("Demo must have 3 charged periods per member");
    if (Scalar("SELECT COUNT(*) FROM payments") < 20)
        throw new Exception("Demo payments are missing");
    if (Scalar("SELECT COUNT(DISTINCT substr(created_at,1,7)) FROM payments") < 4)
        throw new Exception("Demo payment dates must cover distinct chart months");
    if (Scalar("SELECT COUNT(*) FROM members WHERE name NOT LIKE 'Demo član %'") != 0)
        throw new Exception("Demo name is not clearly fictional");
    if (Scalar("SELECT COUNT(*) FROM members WHERE email NOT LIKE '%@example.invalid'") != 0)
        throw new Exception("Demo member email is not reserved for examples");
    if (Scalar("SELECT COUNT(*) FROM demo_seed_state WHERE version=1") != 1)
        throw new Exception("Seed marker missing");
    if (Scalar("""
        SELECT COUNT(*) FROM (
            SELECT m.id,
              COALESCE((SELECT SUM(c.amount_cents) FROM charges c WHERE c.member_id=m.id),0) charged,
              COALESCE((SELECT SUM(p.amount_cents) FROM payments p WHERE p.member_id=m.id),0) paid
            FROM members m
        ) WHERE paid>charged
        """) == 0)
        throw new Exception("Demo should illustrate at least one prepayment");
    if (Scalar("PRAGMA foreign_key_check") != 0)
        throw new Exception("Demo contains orphaned financial records");

    Console.WriteLine("PASS: demo dataset and idempotent seeding, no real member addresses, realistic dues/payments");
}
finally
{
    // Test data only. The production/demo application databases are never touched.
    try { Directory.Delete(root, recursive: true); } catch (IOException) { }
}
