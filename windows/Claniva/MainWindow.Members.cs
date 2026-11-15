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
    private void ShowFilter()
    {
        var compact = Navigation.ActualWidth < 740;
        var row = new StackPanel
        {
            Orientation = compact ? Orientation.Vertical : Orientation.Horizontal,
            Spacing = 10
        };
        var search = new TextBox
        {
            Header = "Pretraži ime ili grupu", PlaceholderText = "Ime ili grupa",
            Text = _query, MinWidth = compact ? 0 : 240, MaxWidth = compact ? 1000 : 360
        };
        var active = new CheckBox
        {
            Content = "Samo aktivni", IsChecked = _activeOnly, VerticalAlignment = VerticalAlignment.Bottom
        };
        row.Children.Add(search);
        row.Children.Add(active);
        row.Children.Add(Action("Pretraži", (_, _) =>
        {
            _query = search.Text.Trim();
            _activeOnly = active.IsChecked == true;
            _visibleMembers = 30;
            Draw();
        }));
        PageContent.Children.Add(row);
    }

    private List<Member> Filter(IReadOnlyList<Member> members) =>
        members.Where(m => (!_activeOnly || m.Active) &&
            (m.Name.Contains(_query, StringComparison.OrdinalIgnoreCase) ||
             m.Group.Contains(_query, StringComparison.OrdinalIgnoreCase))).ToList();

    private void ShowMembers(IReadOnlyList<Member> members, bool withActions)
    {
        if (members.Count == 0)
        {
            PageContent.Children.Add(Panel(Text("Nema članova koji odgovaraju odabranom filtru.")));
            return;
        }
        var list = new StackPanel { Spacing = 9 };
        var compact = Navigation.ActualWidth < 740;
        foreach (var member in members.Take(_visibleMembers))
        {
            var m = member;
            var cell = new StackPanel { Spacing = 8 };
            var name = Text(m.Name, 18, true);
            name.Foreground = new SolidColorBrush(BrandPalette.Navy);
            cell.Children.Add(name);
            var group = Text("Grupa: " + (m.Group.Length == 0 ? "Bez grupe" : m.Group), 13);
            group.Foreground = new SolidColorBrush(BrandPalette.Muted);
            cell.Children.Add(group);
            var balances = new StackPanel
            {
                Orientation = compact ? Orientation.Vertical : Orientation.Horizontal,
                Spacing = compact ? 4 : 20
            };
            balances.Children.Add(Text(
                "Zaduženo: " + LocalStore.Money(m.ChargedCents) +
                "  ·  Evidentirano: " + LocalStore.Money(m.PaidCents), 14));
            var debt = Text("Dug: " + LocalStore.Money(m.DebtCents) +
                "  ·  Preplata: " + LocalStore.Money(m.CreditCents), 14, m.DebtCents > 0);
            debt.Foreground = new SolidColorBrush(m.DebtCents > 0 ? BrandPalette.Danger : BrandPalette.TealDark);
            balances.Children.Add(debt);
            cell.Children.Add(balances);
            if (withActions)
            {
                var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                actions.Children.Add(Action("Detalji", async (_, _) => await ShowMemberDetails(m)));
                actions.Children.Add(Action("Uredi", async (_, _) => await EditMember(m)));
                cell.Children.Add(actions);
            }
            list.Children.Add(Panel(cell));
        }
        if (members.Count > _visibleMembers)
        {
            list.Children.Add(Action(
                "Prikaži još članova (" + (members.Count - _visibleMembers) + ")",
                (_, _) => { _visibleMembers += 30; Draw(); }));
        }
        PageContent.Children.Add(list);
    }
}
