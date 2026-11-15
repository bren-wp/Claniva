using Microsoft.Data.Sqlite;
using System.Globalization;
namespace Claniva.Data;

public record Member(string Id, string Name, string Group, string Email, bool Active, long ChargedCents, long PaidCents)
{
    public long BalanceCents => ChargedCents - PaidCents;
}
public record Summary(int Members, long ChargedCents, long PaidCents)
{
    public long OutstandingCents => Math.Max(0, ChargedCents - PaidCents);
}
public sealed class LocalStore
{
    private readonly string _connectionString;
    public LocalStore()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clanarina", "data");
        Directory.CreateDirectory(folder);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(folder,"clanarina.db"), Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
        using var db = Connect();
        using var command = db.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS members (id TEXT PRIMARY KEY, name TEXT NOT NULL, group_name TEXT NOT NULL DEFAULT '', email TEXT NOT NULL DEFAULT '', active INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS charges (id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id), amount_cents INTEGER NOT NULL CHECK(amount_cents>0), note TEXT NOT NULL DEFAULT '', created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
            CREATE TABLE IF NOT EXISTS payments (id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id), amount_cents INTEGER NOT NULL CHECK(amount_cents>0), note TEXT NOT NULL DEFAULT '', created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
            CREATE INDEX IF NOT EXISTS ix_charges_member ON charges(member_id);
            CREATE INDEX IF NOT EXISTS ix_payments_member ON payments(member_id);
            """;
        command.ExecuteNonQuery();
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
        while(reader.Read()) list.Add(new Member(reader.GetString(0),reader.GetString(1),reader.GetString(2),reader.GetString(3),
            reader.GetInt32(4)==1,reader.GetInt64(5),reader.GetInt64(6)));
        return list;
    }
    public Summary Totals()
    {
        var members=Members();
        return new Summary(members.Count,members.Sum(m=>m.ChargedCents),members.Sum(m=>m.PaidCents));
    }
    public void AddMember(string name,string group,string email)
    {
        name=name.Trim();
        if(name.Length<2 || name.Length>150) throw new ArgumentException("Ime mora imati 2–150 znakova.");
        using var db=Connect();
        using var cmd=db.CreateCommand();
        cmd.CommandText="INSERT INTO members(id,name,group_name,email) VALUES ($id,$name,$group,$email)";
        cmd.Parameters.AddWithValue("$id",Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("$name",name);
        cmd.Parameters.AddWithValue("$group",group.Trim()[..Math.Min(group.Trim().Length,100)]);
        cmd.Parameters.AddWithValue("$email",email.Trim()[..Math.Min(email.Trim().Length,254)]);
        cmd.ExecuteNonQuery();
    }
    private void AddAmount(string table,string memberId,long cents)
    {
        if(table != "charges" && table != "payments") throw new ArgumentException("Invalid table");
        if(cents<1 || cents>10_000_000_000L) throw new ArgumentOutOfRangeException(nameof(cents));
        using var db=Connect();
        using var cmd=db.CreateCommand();
        cmd.CommandText = "INSERT INTO " + table + "(id,member_id,amount_cents,note) VALUES($id,$member,$cents,$note)";
        cmd.Parameters.AddWithValue("$id",Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("$member",memberId);
        cmd.Parameters.AddWithValue("$cents",cents);
        cmd.Parameters.AddWithValue("$note",table=="charges"?"Članarina":"Uplata članarine");
        cmd.ExecuteNonQuery();
    }
    public void AddCharge(string memberId,long cents)=>AddAmount("charges",memberId,cents);
    public void AddPayment(string memberId,long cents)=>AddAmount("payments",memberId,cents);
    public static string Money(long cents) => (cents/100m).ToString("C", CultureInfo.GetCultureInfo("hr-HR"));
    public static long ParseMoney(string value)
    {
        if(!decimal.TryParse(value,NumberStyles.Number,CultureInfo.GetCultureInfo("hr-HR"),out var amount) || amount<=0)
            throw new ArgumentException("Unesite pozitivan iznos, npr. 25,00.");
        var cents = amount*100m;
        if(decimal.Truncate(cents)!=cents || cents>10_000_000_000m) throw new ArgumentException("Neispravan iznos.");
        return (long)cents;
    }
}
