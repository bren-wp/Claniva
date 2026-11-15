using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Claniva.Data;

public sealed partial class LocalStore
{
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
}
