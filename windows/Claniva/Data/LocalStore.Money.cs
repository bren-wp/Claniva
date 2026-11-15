using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Claniva.Data;

public sealed partial class LocalStore
{
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
