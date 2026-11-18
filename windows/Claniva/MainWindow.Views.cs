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
    private void Draw()
    {
        if (PageContent is null) return;
        PageContent.Children.Clear();
        _lastCompact = Navigation.ActualWidth < 740;
        _lastMinimal = Navigation.ActualWidth < 660;
        _lastAnalyticsWide = (ContentScroller?.ActualWidth ?? 0) >= 640;
        // In Minimal mode the hamburger overlays the content header.
        // Reserve its 48-pixel command region instead of letting it cover DEMO.
        PageContent.Padding = _lastMinimal == true
            ? new Thickness(14, 62, 14, 24)
            : new Thickness(26, 24, 26, 28);
#if DEMO_BUILD
        PageContent.Children.Add(new Border
        {
            Background = Brush(245, 222, 169), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14, 10, 14, 10),
            Child = Text("DEMO IZDANJE — izmišljeni podaci · bez licencnog ključa · odvojena baza", 15, true)
        });
#endif
        var titles = new Dictionary<string, string>
        {
            ["dashboard"] = "Nadzorna ploča", ["members"] = "Članovi",
            ["dues"] = "Članarine", ["payments"] = "Uplate",
            ["debts"] = "Dugovanja", ["slips"] = "Uplatnice",
            ["bank"] = "Bankovni izvodi", ["reports"] = "Izvještaji",
            ["license"] = "Licenca", ["settings"] = "Postavke"
        };
        var heading = Text(titles.GetValueOrDefault(_module, "Članarina"), 30, true);
        heading.Foreground = new SolidColorBrush(BrandPalette.Navy);
        PageContent.Children.Add(heading);
        if (_module is "dashboard" or "reports")
        {
            var summary = _store.Totals();
            var metrics = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
            var columnCount = DashboardColumns();
            _lastLayoutColumns = columnCount;
            for (int i = 0; i < columnCount; i++)
                metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var rowCount = (4 + columnCount - 1) / columnCount;
            for (int i = 0; i < rowCount; i++)
                metrics.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            var condensed = columnCount == 4 && ContentScroller.ActualWidth < 860;
            var cards = new[]
            {
                Metric("Članovi", summary.Members.ToString(), BrandPalette.Blue, condensed),
                Metric("Zaduženo", LocalStore.Money(summary.ChargedCents), BrandPalette.Gold, condensed),
                Metric("Evidentirano", LocalStore.Money(summary.PaidCents), BrandPalette.Teal, condensed),
                Metric("Dugovanja", LocalStore.Money(summary.OutstandingCents), BrandPalette.Danger, condensed)
            };
            for (int i = 0; i < cards.Length; i++)
            {
                var box = Panel(cards[i]);
                if (condensed) box.Padding = new Thickness(8);
                Grid.SetColumn(box, i % columnCount);
                Grid.SetRow(box, i / columnCount);
                metrics.Children.Add(box);
            }
            PageContent.Children.Add(metrics);
            ShowReferenceAnalytics(summary);
        }

        switch (_module)
        {
            case "dashboard":
                var shortcuts = new StackPanel
                {
                    Orientation = _lastCompact == true ? Orientation.Vertical : Orientation.Horizontal,
                    Spacing = 10
                };
                shortcuts.Children.Add(Action("Članovi", (_, _) => NavigateTo("members")));
                shortcuts.Children.Add(Action("Novo zaduženje", AddCharge_Click));
                shortcuts.Children.Add(Action("Nova uplata", AddPayment_Click));
                var quick = new StackPanel { Spacing = 12 };
                quick.Children.Add(Text("Brze radnje", 18, true));
                quick.Children.Add(shortcuts);
                ShowRecentPayments();
                PageContent.Children.Add(Panel(quick));
                break;
            case "members":
                PageContent.Children.Add(Action("+ Novi član", AddMember_Click));
                ShowFilter();
                ShowMembers(Filter(_store.Members()), true);
                break;
            case "debts":
                ShowFilter();
                ShowMembers(Filter(_store.Members().Where(m => m.DebtCents > 0).ToList()), true);
                break;
            case "dues":
                PageContent.Children.Add(Action("+ Novo zaduženje", AddCharge_Click));
                PageContent.Children.Add(Text("Povijest članarina", 18, true));
                ShowLedger(_store.Ledger("charge"), true);
                break;
            case "payments":
                PageContent.Children.Add(Action("+ Nova uplata", AddPayment_Click));
                PageContent.Children.Add(Text("Povijest evidentiranih uplata", 18, true));
                ShowLedger(_store.Ledger("payment"), false);
                break;
            case "reports":
                PageContent.Children.Add(Panel(Text(
                    "Osnovni financijski pregled. Razdoblja, alokacije uplata i PDF/CSV izvoz tek su u razvoju.")));
                break;
            case "slips":
                PageContent.Children.Add(Panel(Text(
                    "HUB-3 / EPC QR još nisu implementirani ni verificirani. Nisu dostupni za stvarne uplate.")));
                break;
            case "bank":
                PageContent.Children.Add(Panel(Text(
                    "Siguran uvoz i usklađivanje CAMT.053/CSV izvoda još nisu dovršeni.")));
                break;
            case "license":
#if DEMO_BUILD
                PageContent.Children.Add(Panel(Text("DEMO ne zahtijeva licencni ključ. Koristi se samo za predstavljanje aplikacije.")));
#else
                PageContent.Children.Add(Panel(Text(
                    "Online i ručna offline aktivacija licence još nisu integrirane u aplikaciju.")));
#endif
                break;
            case "settings":
#if DEMO_BUILD
                PageContent.Children.Add(Panel(Text(
                    "DEMO baza: LocalAppData/ClanarinaDemo/data. Izmišljeni podaci; odvojeno od pune aplikacije.")));
#else
                PageContent.Children.Add(Panel(Text(
                    "Lokalna baza: LocalAppData/Clanarina/data. Bez registracije i clouda.")));
#endif
                break;
        }
    }

    /// <summary>Real recorded payments, never a fabricated chart or bank confirmation.</summary>
    private void ShowRecentPayments()
    {
        var list = _store.Ledger("payment").Take(5).ToList();
        var content = new StackPanel { Spacing = 10 };
        content.Children.Add(Text("Posljednje uplate", 18, true));
        if (list.Count == 0)
        {
            var empty = Text("Još nema evidentiranih uplata.", 14);
            empty.Foreground = new SolidColorBrush(BrandPalette.Muted);
            content.Children.Add(empty);
        }
        else
        {
            foreach (var payment in list)
            {
                var row = new Grid { ColumnSpacing = 12 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var details = new StackPanel { Spacing = 4 };
                details.Children.Add(Text(payment.MemberName, 14, true));
                var date = Text(payment.CreatedAt, 12);
                date.Foreground = new SolidColorBrush(BrandPalette.Muted);
                details.Children.Add(date);
                Grid.SetColumn(details, 0);
                row.Children.Add(details);
                var amount = Text(LocalStore.Money(payment.AmountCents), 16, true);
                amount.Foreground = new SolidColorBrush(BrandPalette.Navy);
                Grid.SetColumn(amount, 1);
                row.Children.Add(amount);
                content.Children.Add(row);
            }
        }
        PageContent.Children.Add(Panel(content));
    }
}
