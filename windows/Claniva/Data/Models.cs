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

