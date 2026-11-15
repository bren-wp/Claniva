using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Claniva.Data;

public record Member(
    string Id, string Name, string Group, string Email, bool Active,
    long ChargedCents, long PaidCents)
{
    public long BalanceCents => ChargedCents - PaidCents;
    public long DebtCents => Math.Max(0, BalanceCents);
    public long CreditCents => Math.Max(0, -BalanceCents);
}

public record Summary(
    int Members, long ChargedCents, long PaidCents,
    long OutstandingCents, long CreditCents);

public record LedgerEntry(
    string Id, string MemberId, string MemberName, long AmountCents,
    string Note, string Extra, string CreatedAt, string Kind);

/// <summary>
/// Offline-only SQLite store. Schema migration preserves every existing member,
/// charge, payment and identifier. Monetary values are integral cents.
/// </summary>
public sealed class LocalStore
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
        pragma.CommandText = "PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL;";
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

    public List<Member> Members()
    {
        var list = new List<Member>();
        using var db = Connect();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            SELECT m.id,m.name,m.group_name,m.email,m.active,
              COALESCE((SELECT SUM(amount_cents) FROM charges WHERE member_id=m.id),0),
              COALESCE((SELECT SUM(amount_cents) FROM payments WHERE member_id=m.id),0)
            FROM members m ORDER BY m.name COLLATE NOCASE;
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(new Member(
            reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), reader.GetInt32(4) == 1, reader.GetInt64(5),
            reader.GetInt64(6)));
        return list;
    }

    public Summary Totals()
    {
        var list = Members();
        return new Summary(
            list.Count, list.Sum(x => x.ChargedCents), list.Sum(x => x.PaidCents),
            list.Sum(x => x.DebtCents), list.Sum(x => x.CreditCents));
    }

    private static void ValidateMember(string name, string group, string email)
    {
        if (name.Trim().Length is < 2 or > 150)
            throw new ArgumentException("Ime mora imati 2–150 znakova.");
        if (group.Trim().Length > 100)
            throw new ArgumentException("Grupa smije imati najviše 100 znakova.");
        var e = email.Trim();
        if (e.Length > 254 || (e.Length != 0 && (!e.Contains('@') || !e.Split('@').Last().Contains('.'))))
            throw new ArgumentException("Provjerite e-mail adresu.");
    }

    public void AddMember(string name, string group, string email)
    {
        ValidateMember(name, group, email);
        using var db = Connect();
        using var tx = db.BeginTransaction();
        var id = Guid.NewGuid().ToString();
        using (var cmd = db.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO members(id,name,group_name,email) VALUES($id,$name,$group,$email)";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$name", name.Trim());
            cmd.Parameters.AddWithValue("$group", group.Trim());
            cmd.Parameters.AddWithValue("$email", email.Trim());
            cmd.ExecuteNonQuery();
        }
        Audit(db, tx, "member.created", id);
        tx.Commit();
    }

    public void UpdateMember(string id, string name, string group, string email, bool active)
    {
        ValidateMember(name, group, email);
        using var db = Connect();
        using var tx = db.BeginTransaction();
        using (var cmd = db.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE members SET name=$name,group_name=$group,email=$email,active=$active WHERE id=$id";
            cmd.Parameters.AddWithValue("$name", name.Trim());
            cmd.Parameters.AddWithValue("$group", group.Trim());
            cmd.Parameters.AddWithValue("$email", email.Trim());
            cmd.Parameters.AddWithValue("$active", active ? 1 : 0);
            cmd.Parameters.AddWithValue("$id", id);
            if (cmd.ExecuteNonQuery() != 1)
                throw new ArgumentException("Član nije pronađen.");
        }
        Audit(db, tx, "member.updated", id);
        tx.Commit();
    }

    private void AddAmount(string table, string memberId, long cents, string extra)
    {
        if (table != "charges" && table != "payments")
            throw new ArgumentException("Invalid table");
        if (cents is < 1 or > 10_000_000_000L)
            throw new ArgumentException("Iznos nije ispravan.");
        var value = extra.Trim();
        if (table == "charges" && value.Length != 0 &&
            (!DateTime.TryParseExact(value + "-01", "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _) || value.Length != 7))
            throw new ArgumentException("Razdoblje unesite u obliku GGGG-MM.");
        if (table == "payments" && value.Length > 100)
            throw new ArgumentException("Referenca smije imati najviše 100 znakova.");
        var field = table == "charges" ? "period" : "reference";
        using var db = Connect();
        using var tx = db.BeginTransaction();
        using (var cmd = db.CreateCommand())
        {
            cmd.Transaction = tx;
            // Both SQL identifiers are taken exclusively from the validated constants above.
            cmd.CommandText = "INSERT INTO " + table +
                "(id,member_id,amount_cents,note," + field + ") VALUES($id,$member,$cents,$note,$extra)";
            cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("$member", memberId);
            cmd.Parameters.AddWithValue("$cents", cents);
            cmd.Parameters.AddWithValue("$note", table == "charges" ? "Članarina" : "Evidentirana uplata");
            cmd.Parameters.AddWithValue("$extra", value);
            cmd.ExecuteNonQuery();
        }
        Audit(db, tx, table == "charges" ? "charge.created" : "payment.created", memberId);
        tx.Commit();
    }

    public void AddCharge(string memberId, long cents, string period = "") =>
        AddAmount("charges", memberId, cents, period);

    public void AddPayment(string memberId, long cents, string reference = "") =>
        AddAmount("payments", memberId, cents, reference);

    public List<LedgerEntry> Ledger(string kind, string? memberId = null)
    {
        if (kind != "charge" && kind != "payment")
            throw new ArgumentException("Invalid ledger type");
        var table = kind == "charge" ? "charges" : "payments";
        var extra = kind == "charge" ? "period" : "reference";
        using var db = Connect();
        using var cmd = db.CreateCommand();
        cmd.CommandText =
            "SELECT t.id,t.member_id,m.name,t.amount_cents,t.note,t." + extra + ",t.created_at " +
            "FROM " + table + " t JOIN members m ON m.id=t.member_id " +
            (memberId == null ? "" : "WHERE t.member_id=$member ") +
            "ORDER BY t.created_at DESC,t.id DESC";
        if (memberId != null) cmd.Parameters.AddWithValue("$member", memberId);
        var items = new List<LedgerEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            items.Add(new LedgerEntry(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetInt64(3), reader.GetString(4), reader.GetString(5),
                reader.GetString(6), kind));
        return items;
    }

    public static string Money(long cents) =>
        (cents / 100m).ToString("C", CultureInfo.GetCultureInfo("hr-HR"));

    public static long ParseMoney(string value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number,
            CultureInfo.GetCultureInfo("hr-HR"), out var amount) || amount <= 0)
            throw new ArgumentException("Unesite pozitivan iznos, npr. 25,00.");
        var cents = amount * 100m;
        if (decimal.Truncate(cents) != cents || cents > 10_000_000_000m)
            throw new ArgumentException("Neispravan iznos.");
        return (long)cents;
    }
}
