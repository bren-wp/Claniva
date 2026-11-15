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
        Title = title,
        Content = new ScrollViewer
        {
            Content = body,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = Math.Max(190, Math.Min(430, Navigation.ActualHeight - 170))
        },
        PrimaryButtonText = "Spremi",
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
