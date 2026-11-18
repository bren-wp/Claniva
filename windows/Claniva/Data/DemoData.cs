#if DEMO_BUILD
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Claniva.Data;

/// <summary>
/// Compile-time-only demo dataset. No real names, email addresses, bank accounts or
/// license secrets. Demo runs in a separate LocalAppData database.
/// Never link or call this file in a full production build.
/// </summary>
internal static class DemoData
{
    internal static void Seed(string connectionString)
    {
        using var db = new SqliteConnection(connectionString);
        db.Open();
        using var pragma = db.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON";
        pragma.ExecuteNonQuery();
        using var tx = db.BeginTransaction();

        using (var setup = db.CreateCommand())
        {
            setup.Transaction = tx;
            setup.CommandText = """
                CREATE TABLE IF NOT EXISTS demo_seed_state (
                    version INTEGER PRIMARY KEY, seeded_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
                """;
            setup.ExecuteNonQuery();
        }
        using (var check = db.CreateCommand())
        {
            check.Transaction = tx;
            check.CommandText = "SELECT COUNT(*) FROM demo_seed_state WHERE version=1";
            if ((long)(check.ExecuteScalar() ?? 0L) > 0L)
            {
                tx.Commit();
                return;
            }
        }
        string[] groups = ["Juniori", "Seniori", "Rekreativci", "Članovi udruge"];
        for (int i = 1; i <= 24; i++)
        {
            var id = $"demo-member-{i:000}";
            using (var member = db.CreateCommand())
            {
                member.Transaction = tx;
                member.CommandText = """
                    INSERT INTO members(id,name,group_name,email,active)
                    VALUES($id,$name,$group,$email,$active)
                    """;
                member.Parameters.AddWithValue("$id", id);
                member.Parameters.AddWithValue("$name", $"Demo član {i:00}");
                member.Parameters.AddWithValue("$group", groups[(i - 1) % groups.Length]);
                member.Parameters.AddWithValue("$email", $"demo{i:00}@example.invalid");
                member.Parameters.AddWithValue("$active", i % 11 == 0 ? 0 : 1);
                member.ExecuteNonQuery();
            }
            int fee = i % 5 == 0 ? 3500 : i % 4 == 0 ? 3000 : 2500;
            for (int month = 9; month <= 11; month++)
            {
                using var charge = db.CreateCommand();
                charge.Transaction = tx;
                charge.CommandText = """
                    INSERT INTO charges(id,member_id,amount_cents,note,period)
                    VALUES($id,$member,$amount,$note,$period)
                    """;
                charge.Parameters.AddWithValue("$id", $"demo-charge-{i:000}-{month:00}");
                charge.Parameters.AddWithValue("$member", id);
                charge.Parameters.AddWithValue("$amount", fee);
                charge.Parameters.AddWithValue("$note", "Demo članarina");
                charge.Parameters.AddWithValue("$period", $"2026-{month:00}");
                charge.ExecuteNonQuery();
            }
            int numberOfPayments = (i % 6) switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                3 => 3,
                4 => 4,
                _ => 2
            };
            for (int p = 1; p <= numberOfPayments; p++)
            {
                long amount = i % 7 == 0 ? fee / 2 : fee;
                using var payment = db.CreateCommand();
                payment.Transaction = tx;
                payment.CommandText = """
                    INSERT INTO payments(id,member_id,amount_cents,note,reference,created_at)
                    VALUES($id,$member,$amount,$note,$reference,$created)
                    """;
                payment.Parameters.AddWithValue("$id", $"demo-payment-{i:000}-{p:00}");
                payment.Parameters.AddWithValue("$member", id);
                payment.Parameters.AddWithValue("$amount", amount);
                payment.Parameters.AddWithValue("$note", "Izmišljena demo uplata");
                payment.Parameters.AddWithValue("$reference", $"DEMO-2026-{i:000}-{p:00}");
                // Fictional sample history is spread across the preceding six
                // months so the real dashboard chart has meaningful demo data.
                // Existing seeded demo installations are never rewritten.
                var demoDate = DateTime.UtcNow.AddMonths(-((i + p) % 6));
                payment.Parameters.AddWithValue("$created",
                    demoDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                payment.ExecuteNonQuery();
            }
        }
        using (var marker = db.CreateCommand())
        {
            marker.Transaction = tx;
            marker.CommandText = "INSERT INTO demo_seed_state(version) VALUES(1)";
            marker.ExecuteNonQuery();
        }
        tx.Commit();
    }
}
#endif
