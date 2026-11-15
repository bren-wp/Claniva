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
        var scroller = new ScrollViewer
        {
            Content = content,
            MaxHeight = Math.Max(190, Math.Min(460, Navigation.ActualHeight - 180)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
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
}
