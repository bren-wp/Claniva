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
        PageContent.Padding = Navigation.ActualWidth < 680 ? new Thickness(12) : new Thickness(22);
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
        PageContent.Children.Add(Text(titles.GetValueOrDefault(_module, "Članarina"), 32, true));
        var summary = _store.Totals();
        if (_module is "dashboard" or "reports")
        {
            var metrics = new Grid { ColumnSpacing = 10, RowSpacing = 10 };
            var columnCount = DashboardColumns();
            _lastLayoutColumns = columnCount;
            for (int i = 0; i < columnCount; i++)
                metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var rowCount = (5 + columnCount - 1) / columnCount;
            for (int i = 0; i < rowCount; i++)
                metrics.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            var cards = new[]
            {
                Metric("Članovi", summary.Members.ToString(), BrandPalette.Blue),
                Metric("Zaduženo", LocalStore.Money(summary.ChargedCents), BrandPalette.Gold),
                Metric("Evidentirano", LocalStore.Money(summary.PaidCents), BrandPalette.Teal),
                Metric("Dugovanja", LocalStore.Money(summary.OutstandingCents), BrandPalette.Danger),
                Metric("Preplate", LocalStore.Money(summary.CreditCents), BrandPalette.Blue)
            };
            for (int i = 0; i < cards.Length; i++)
            {
                var box = Panel(cards[i]);
                Grid.SetColumn(box, i % columnCount);
                Grid.SetRow(box, i / columnCount);
                metrics.Children.Add(box);
            }
            PageContent.Children.Add(metrics);
            PageContent.Children.Add(Text(
                "Evidentirane uplate nisu automatski bankovno potvrđene. Dugovi i preplate računaju se po pojedinom članu.", 14));
        }

        switch (_module)
        {
            case "dashboard":
                PageContent.Children.Add(Panel(Text("Brze radnje: Članovi · Članarine · Uplate", 17)));
                ShowMembers(_store.Members().Take(8).ToList(), false);
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
                ShowLedger(_store.Ledger("charge"), true);
                break;
            case "payments":
                PageContent.Children.Add(Action("+ Nova uplata", AddPayment_Click));
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
                    "Online i ručna offline aktivacija još nisu integrirane u Windows aplikaciju.")));
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
}
