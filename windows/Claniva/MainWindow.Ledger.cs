using Claniva.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.UI;

namespace Claniva;

public sealed partial class MainWindow
{
    private void ShowLedger(IReadOnlyList<LedgerEntry> entries, bool charges)
    {
        if (entries.Count == 0)
        {
            PageContent.Children.Add(Panel(Text("Nema evidentiranih transakcija.")));
            return;
        }
        var list = new StackPanel { Spacing = 10 };
        var visible = 0;
        foreach (var entry in entries.Take(60))
        {
            var cell = new StackPanel { Spacing = 7 };
            var heading = Text(entry.MemberName, 17, true);
            heading.Foreground = new SolidColorBrush(BrandPalette.Navy);
            cell.Children.Add(heading);
            var amount = Text(LocalStore.Money(entry.AmountCents), 21, true);
            amount.Foreground = new SolidColorBrush(BrandPalette.Ink);
            cell.Children.Add(amount);
            cell.Children.Add(Text((charges ? "Razdoblje: " : "Referenca: ") +
                (entry.Extra.Length == 0 ? "Nije navedeno" : entry.Extra), 14));
            var date = Text("Evidentirano: " + entry.CreatedAt, 13);
            date.Foreground = new SolidColorBrush(BrandPalette.Muted);
            cell.Children.Add(date);
            list.Children.Add(Panel(cell));
            visible++;
        }
        if (entries.Count > visible)
            list.Children.Add(Text(
                "Prikazano " + visible + " od " + entries.Count +
                " stavki. Za potpuni pregled koristite filtre u sljedećoj iteraciji.", 14));
        PageContent.Children.Add(list);
    }
}
