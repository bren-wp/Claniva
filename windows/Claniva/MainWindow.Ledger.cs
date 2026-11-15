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
        foreach (var entry in entries)
        {
            var cell = new StackPanel { Spacing = 4, Padding = new Thickness(5) };
            cell.Children.Add(Text(entry.MemberName, 17, true));
            cell.Children.Add(Text(LocalStore.Money(entry.AmountCents), 21, true));
            cell.Children.Add(Text((charges ? "Razdoblje: " : "Referenca: ") +
                (entry.Extra.Length == 0 ? "Nije navedeno" : entry.Extra), 14));
            cell.Children.Add(Text("Evidentirano: " + entry.CreatedAt, 13));
            PageContent.Children.Add(Panel(cell));
        }
    }
}
