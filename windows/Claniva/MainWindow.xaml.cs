using Claniva.Data;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
namespace Claniva;

public sealed partial class MainWindow : Window
{
    private readonly LocalStore _store=new();
    private string _module="dashboard";
    public MainWindow()
    {
        InitializeComponent();
#if DEMO_BUILD
        Title = "Članarina DEMO — by BRENDIGO";
#endif
        Navigation.SelectedItem=Navigation.MenuItems[0];
        Draw();
    }
    private void Navigation_SelectionChanged(NavigationView sender,NavigationViewSelectionChangedEventArgs args)
    {
        if(args.SelectedItem is NavigationViewItem item) {
            _module=item.Tag?.ToString()??"dashboard";
            Draw();
        }
    }
    private static SolidColorBrush Brush(byte r,byte g,byte b)=>new(Color.FromArgb(255,r,g,b));
    private static TextBlock Text(string value,int size=16,bool bold=false)=>new() {
        Text=value,FontSize=size,FontWeight=bold?Microsoft.UI.Text.FontWeights.Bold:Microsoft.UI.Text.FontWeights.Normal,
        TextWrapping=TextWrapping.Wrap,Foreground=Brush(23,36,58)
    };
    private StackPanel Card(string title,string value,byte r,byte g,byte b)
    {
        var content=new StackPanel{Spacing=10,Padding=new Thickness(16)};
        content.Children.Add(Text(title,15));
        var amount=Text(value,25,true);amount.Foreground=Brush(r,g,b);
        content.Children.Add(amount);
        return content;
    }
    private void Draw()
    {
        if(PageContent is null) return;
        PageContent.Children.Clear();
#if DEMO_BUILD
        PageContent.Children.Add(new Border {
            Background = Brush(245, 222, 169), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14, 10, 14, 10),
            Child = Text("DEMO IZDANJE — izmišljeni podaci · bez licence · odvojena baza", 15, true)
        });
#endif
        var titles=new Dictionary<string,string>{
            ["dashboard"]="Nadzorna ploča",["members"]="Članovi",["dues"]="Članarine",["payments"]="Uplate",
            ["debts"]="Dugovanja",["slips"]="Uplatnice",["bank"]="Bankovni izvodi",["reports"]="Izvještaji",
            ["license"]="Licenca",["settings"]="Postavke"};
        PageContent.Children.Add(Text(titles.GetValueOrDefault(_module,"Članarina"),33,true));
        var summary=_store.Totals();
        if(_module=="dashboard" || _module=="reports") {
            var metrics=new Grid{ColumnSpacing=12,RowSpacing=12};
            for(int i=0;i<4;i++) metrics.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
            var entries=new[]{
                Card("Članovi",summary.Members.ToString(),37,99,235),
                Card("Zaduženo",LocalStore.Money(summary.ChargedCents),187,130,12),
                Card("Uplaćeno",LocalStore.Money(summary.PaidCents),10,148,110),
                Card("Dugovanja",LocalStore.Money(summary.OutstandingCents),214,60,67)};
            for(int i=0;i<4;i++) {
                var border=Panel(entries[i]);Grid.SetColumn(border,i);metrics.Children.Add(border);
            }
            PageContent.Children.Add(metrics);
        }
        switch(_module) {
            case "members":
                PageContent.Children.Add(Action("Novi član",AddMember_Click));
                ShowMembers(_store.Members()); break;
            case "dues":
                PageContent.Children.Add(Action("Novo zaduženje",AddCharge_Click));
                ShowMembers(_store.Members()); break;
            case "payments":
                PageContent.Children.Add(Action("Nova uplata",AddPayment_Click));
                ShowMembers(_store.Members()); break;
            case "debts":
                ShowMembers(_store.Members().Where(m=>m.BalanceCents>0).ToList());break;
            case "dashboard":
                PageContent.Children.Add(Panel(Text("Brze radnje: otvorite Članovi, Članarine ili Uplate u navigaciji.",17)));
                ShowMembers(_store.Members().Take(8).ToList());break;
            case "reports":
                PageContent.Children.Add(Text("Sažetak lokalne evidencije. Napredni izvještaji i export su u razvoju."));break;
            case "slips": PageContent.Children.Add(Text("HUB-3 / EPC QR još nisu implementirani ni validirani."));break;
            case "bank": PageContent.Children.Add(Text("Uvoz i usklađivanje bankovnih izvoda je u razvoju."));break;
            case "license": PageContent.Children.Add(Text("Online i ručna offline aktivacija još nisu povezane s aplikacijom."));break;
            case "settings": PageContent.Children.Add(Text("Baza je na ovom uređaju u LocalAppData/Clanarina/data. Bez clouda i prijave."));break;
        }
    }
    private static Border Panel(UIElement element)=>new() {
        Child=element,Background=Brush(255,255,255),CornerRadius=new CornerRadius(16),
        BorderBrush=Brush(220,229,240),BorderThickness=new Thickness(1),Padding=new Thickness(10)
    };
    private static Button Action(string text,RoutedEventHandler click)
    {
        var button=new Button{Content=text,HorizontalAlignment=HorizontalAlignment.Left};
        button.Click+=click;return button;
    }
    private void ShowMembers(IReadOnlyList<Member> members)
    {
        if(members.Count==0) {PageContent.Children.Add(Panel(Text("Nema stavki. Dodajte prvog člana ili zaduženje.")));return;}
        var list=new StackPanel{Spacing=7};
        foreach(var m in members)
        {
            var row=new Grid{ColumnSpacing=12,Padding=new Thickness(8)};
            row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(2,GridUnitType.Star)});
            row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
            row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
            row.Children.Add(Text(m.Name,16,true));
            var group=Text(m.Group,14);Grid.SetColumn(group,1);row.Children.Add(group);
            var balance=Text("Saldo: "+LocalStore.Money(m.BalanceCents),14);
            Grid.SetColumn(balance,2);row.Children.Add(balance);
            list.Children.Add(Panel(row));
        }
        PageContent.Children.Add(list);
    }
    private async void AddMember_Click(object sender,RoutedEventArgs e)
    {
        var name=new TextBox{Header="Ime i prezime",PlaceholderText="Ime i prezime"};
        var group=new TextBox{Header="Grupa"};
        var email=new TextBox{Header="E-mail (neobavezno)"};
        var form=new StackPanel{Spacing=10};
        form.Children.Add(name);form.Children.Add(group);form.Children.Add(email);
        var dialog=Dialog("Novi član",form);
        if(await dialog.ShowAsync()!=ContentDialogResult.Primary) return;
        try {_store.AddMember(name.Text,group.Text,email.Text);Draw();}
        catch(Exception ex){await Inform("Greška",ex.Message);}
    }
    private async void AddCharge_Click(object sender,RoutedEventArgs e)=>await AddAmount(true);
    private async void AddPayment_Click(object sender,RoutedEventArgs e)=>await AddAmount(false);
    private async System.Threading.Tasks.Task AddAmount(bool charge)
    {
        var members=_store.Members();
        if(members.Count==0){await Inform("Nema članova","Najprije dodajte člana.");return;}
        var choice=new ComboBox{Header="Član",ItemsSource=members,DisplayMemberPath="Name",SelectedIndex=0,MinWidth=260};
        var amount=new TextBox{Header="Iznos (€)",PlaceholderText="25,00"};
        var layout=new StackPanel{Spacing=12};layout.Children.Add(choice);layout.Children.Add(amount);
        var dialog=Dialog(charge?"Novo zaduženje":"Nova uplata",layout);
        if(await dialog.ShowAsync()!=ContentDialogResult.Primary)return;
        try {
            var selected=(Member)choice.SelectedItem;
            var cents=LocalStore.ParseMoney(amount.Text);
            if(charge)_store.AddCharge(selected.Id,cents);else _store.AddPayment(selected.Id,cents);
            Draw();
        } catch(Exception ex){await Inform("Greška",ex.Message);}
    }
    private ContentDialog Dialog(string title,UIElement body)=>new() {
        Title=title,Content=body,PrimaryButtonText="Spremi",CloseButtonText="Odustani",DefaultButton=ContentDialogButton.Primary,
        XamlRoot=PageContent.XamlRoot
    };
    private async System.Threading.Tasks.Task Inform(string title,string message)
    {
        await new ContentDialog{Title=title,Content=message,CloseButtonText="U redu",XamlRoot=PageContent.XamlRoot}.ShowAsync();
    }
}
