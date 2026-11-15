using Claniva.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Claniva;

public sealed partial class MainWindow : Window
{
    private readonly LocalStore _store = new();
    private string _module = "dashboard";
    private string _query = "";
    private bool _activeOnly;

    public MainWindow()
    {
        InitializeComponent();
#if DEMO_BUILD
        Title = "Članarina DEMO — by BRENDIGO";
#endif
        Navigation.SelectedItem = Navigation.MenuItems[0];
        Draw();
    }

    private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            _module = item.Tag?.ToString() ?? "dashboard";
            _query = "";
            _activeOnly = false;
            Draw();
        }
    }

    private static SolidColorBrush Brush(byte r, byte g, byte b) =>
        new(Color.FromArgb(255, r, g, b));

    private static TextBlock Text(string value, int size = 16, bool bold = false) => new()
    {
        Text = value, FontSize = size,
        FontWeight = bold ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brush(23, 36, 58)
    };

    private static Border Panel(UIElement child) => new()
    {
        Child = child, Background = Brush(255, 255, 255),
        CornerRadius = new CornerRadius(16), BorderBrush = Brush(220, 229, 240),
        BorderThickness = new Thickness(1), Padding = new Thickness(12)
    };

    private static Button Action(string title, RoutedEventHandler handler)
    {
        var b = new Button { Content = title, HorizontalAlignment = HorizontalAlignment.Left };
        b.Click += handler;
        return b;
    }

    private static StackPanel Metric(string title, string amount, byte r, byte g, byte b)
    {
        var panel = new StackPanel { Spacing = 10, Padding = new Thickness(13) };
        panel.Children.Add(Text(title, 15));
        var value = Text(amount, 23, true);
        value.Foreground = Brush(r, g, b);
        panel.Children.Add(value);
        return panel;
    }

    private void Draw()
    {
        if (PageContent is null) return;
        PageContent.Children.Clear();
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
            for (int i = 0; i < 5; i++)
                metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var cards = new[]
            {
                Metric("Članovi", summary.Members.ToString(), 37, 99, 235),
                Metric("Zaduženo", LocalStore.Money(summary.ChargedCents), 187, 130, 12),
                Metric("Evidentirano", LocalStore.Money(summary.PaidCents), 10, 148, 110),
                Metric("Dugovanja", LocalStore.Money(summary.OutstandingCents), 214, 60, 67),
                Metric("Preplate", LocalStore.Money(summary.CreditCents), 37, 99, 235)
            };
            for (int i = 0; i < cards.Length; i++)
            {
                var box = Panel(cards[i]);
                Grid.SetColumn(box, i);
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

    private void ShowFilter()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var search = new TextBox
        {
            Header = "Pretraži ime ili grupu", PlaceholderText = "Ime ili grupa",
            Text = _query, MinWidth = 280, MaxWidth = 360
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
        var list = new StackPanel { Spacing = 8 };
        foreach (var member in members)
        {
            var m = member;
            var cell = new StackPanel { Spacing = 5, Padding = new Thickness(5) };
            cell.Children.Add(Text(m.Name, 18, true));
            cell.Children.Add(Text("Grupa: " + (m.Group.Length == 0 ? "Bez grupe" : m.Group), 14));
            cell.Children.Add(Text("Zaduženo: " + LocalStore.Money(m.ChargedCents) +
                "  |  Uplaćeno: " + LocalStore.Money(m.PaidCents), 15));
            cell.Children.Add(Text("Dug: " + LocalStore.Money(m.DebtCents) +
                "  |  Preplata: " + LocalStore.Money(m.CreditCents), 15));
            if (withActions)
            {
                var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                actions.Children.Add(Action("Detalji", async (_, _) => await ShowMemberDetails(m)));
                actions.Children.Add(Action("Uredi", async (_, _) => await EditMember(m)));
                cell.Children.Add(actions);
            }
            list.Children.Add(Panel(cell));
        }
        PageContent.Children.Add(list);
    }

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

    private async System.Threading.Tasks.Task ShowMemberDetails(Member m)
    {
        var content = new StackPanel { Spacing = 6, Padding = new Thickness(5) };
        content.Children.Add(Text(m.Name, 21, true));
        content.Children.Add(Text("Grupa: " + (m.Group.Length == 0 ? "Bez grupe" : m.Group)));
        content.Children.Add(Text("E-mail: " + (m.Email.Length == 0 ? "Nije unesen" : m.Email)));
        content.Children.Add(Text("Zaduženo: " + LocalStore.Money(m.ChargedCents)));
        content.Children.Add(Text("Uplaćeno: " + LocalStore.Money(m.PaidCents)));
        content.Children.Add(Text("Dug: " + LocalStore.Money(m.DebtCents)));
        content.Children.Add(Text("Preplata: " + LocalStore.Money(m.CreditCents)));
        var charges = _store.Ledger("charge", m.Id);
        var payments = _store.Ledger("payment", m.Id);
        content.Children.Add(Text("Zaduženja (" + charges.Count + ")", 17, true));
        foreach (var charge in charges.Take(18))
            content.Children.Add(Text(charge.Extra + " · " + LocalStore.Money(charge.AmountCents), 14));
        content.Children.Add(Text("Uplate (" + payments.Count + ")", 17, true));
        foreach (var payment in payments.Take(18))
            content.Children.Add(Text(payment.Extra + " · " + LocalStore.Money(payment.AmountCents), 14));
        var scroller = new ScrollViewer { Content = content, MaxHeight = 500 };
        await new ContentDialog
        {
            Title = "Detalji člana", Content = scroller, CloseButtonText = "Zatvori",
            XamlRoot = PageContent.XamlRoot
        }.ShowAsync();
    }

    private async void AddMember_Click(object sender, RoutedEventArgs e) => await SaveMember(null);

    private async System.Threading.Tasks.Task EditMember(Member member) => await SaveMember(member);

    private async System.Threading.Tasks.Task SaveMember(Member? member)
    {
        var name = new TextBox { Header = "Ime i prezime", Text = member?.Name ?? "" };
        var group = new TextBox { Header = "Grupa", Text = member?.Group ?? "" };
        var email = new TextBox { Header = "E-mail (neobavezno)", Text = member?.Email ?? "" };
        var active = new CheckBox { Content = "Aktivan član", IsChecked = member?.Active ?? true };
        var fields = new StackPanel { Spacing = 10 };
        fields.Children.Add(name); fields.Children.Add(group);
        fields.Children.Add(email); fields.Children.Add(active);
        var dialog = Dialog(member == null ? "Novi član" : "Uredi člana", fields);
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try
        {
            if (member == null) _store.AddMember(name.Text, group.Text, email.Text);
            else _store.UpdateMember(member.Id, name.Text, group.Text, email.Text, active.IsChecked == true);
            Draw();
        }
        catch (Exception ex) { await Inform("Neuspjelo spremanje", ex.Message); }
    }

    private async void AddCharge_Click(object sender, RoutedEventArgs e) => await AddAmount(true);
    private async void AddPayment_Click(object sender, RoutedEventArgs e) => await AddAmount(false);

    private async System.Threading.Tasks.Task AddAmount(bool charge)
    {
        var members = _store.Members();
        if (members.Count == 0)
        {
            await Inform("Nema članova", "Najprije dodajte člana.");
            return;
        }
        var choice = new ComboBox
        {
            Header = "Član", ItemsSource = members, DisplayMemberPath = "Name",
            SelectedIndex = 0, MinWidth = 260
        };
        var amount = new TextBox { Header = "Iznos (€)", PlaceholderText = "25,00" };
        var extra = new TextBox
        {
            Header = charge ? "Razdoblje (GGGG-MM)" : "Referenca (neobavezno)",
            PlaceholderText = charge ? "2026-11" : "Npr. UPLATA-001"
        };
        var fields = new StackPanel { Spacing = 12 };
        fields.Children.Add(choice); fields.Children.Add(amount); fields.Children.Add(extra);
        if (await Dialog(charge ? "Novo zaduženje" : "Nova uplata", fields).ShowAsync()
            != ContentDialogResult.Primary) return;
        try
        {
            var selected = (Member)choice.SelectedItem;
            var cents = LocalStore.ParseMoney(amount.Text);
            if (charge) _store.AddCharge(selected.Id, cents, extra.Text);
            else _store.AddPayment(selected.Id, cents, extra.Text);
            Draw();
        }
        catch (Exception ex)
        {
            await Inform("Neuspjelo spremanje",
                "Provjerite iznos, razdoblje i referencu. Ista stavka možda već postoji. " + ex.Message);
        }
    }

    private ContentDialog Dialog(string title, UIElement body) => new()
    {
        Title = title, Content = body, PrimaryButtonText = "Spremi",
        CloseButtonText = "Odustani", DefaultButton = ContentDialogButton.Primary,
        XamlRoot = PageContent.XamlRoot
    };

    private async System.Threading.Tasks.Task Inform(string title, string message)
    {
        await new ContentDialog
        {
            Title = title, Content = message, CloseButtonText = "U redu",
            XamlRoot = PageContent.XamlRoot
        }.ShowAsync();
    }
}
