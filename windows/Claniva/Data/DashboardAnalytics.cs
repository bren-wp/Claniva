using System.Globalization;

namespace Claniva.Data;

/// <summary>Read-only visual analytics from recorded local ledger values.</summary>
internal static class DashboardAnalytics
{
    internal record MonthlyTotal(string Label, long Cents);

    // Based on actual outstanding balances per member, not on a count of
    // bank-confirmed payments. Credits never hide another member's debt.
    internal static double CoveredPercent(Summary summary)
    {
        if (summary.ChargedCents <= 0) return 0;
        var covered = Math.Clamp(
            summary.ChargedCents - summary.OutstandingCents, 0, summary.ChargedCents);
        return (double)covered / summary.ChargedCents * 100;
    }

    internal static IReadOnlyList<MonthlyTotal> LastSixMonths(
        IEnumerable<LedgerEntry> payments, DateTime now)
    {
        var current = new DateTime(now.Year, now.Month, 1);
        var sums = new long[6];
        foreach (var payment in payments)
        {
            if (payment.Kind != "payment" || payment.CreatedAt.Length < 10 ||
                !DateTime.TryParseExact(payment.CreatedAt[..10], "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                continue;
            var index = (date.Year - current.Year) * 12 + date.Month - current.Month + 5;
            if (index is >= 0 and < 6 && payment.AmountCents > 0)
                sums[index] = checked(sums[index] + payment.AmountCents);
        }
        var hr = CultureInfo.GetCultureInfo("hr-HR");
        return Enumerable.Range(0, 6)
            .Select(i => new MonthlyTotal(
                current.AddMonths(i - 5).ToString("MMM", hr).TrimEnd('.'), sums[i]))
            .ToArray();
    }
}
