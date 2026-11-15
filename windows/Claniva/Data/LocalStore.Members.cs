using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Claniva.Data;

public sealed partial class LocalStore
{
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
}
