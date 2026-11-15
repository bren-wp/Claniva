using Claniva.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.UI;

namespace Claniva;

public sealed partial class MainWindow : Window
{
    private readonly LocalStore _store;
    private NavigationView Navigation = null!;
    private StackPanel PageContent = null!;
    private string _module = "dashboard";
    private string _query = "";
    private bool _activeOnly;
    private bool _ready;
    private int _lastLayoutColumns = -1;

    public MainWindow()
    {
        // Avoid MainWindow XAML parser completely: create native WinUI controls in C#.
        try { InitializeShell(); }
        catch (Exception ex) { StartupDiagnostics.Record("native-shell-bootstrap", ex); throw; }

        try { _store = new LocalStore(); }
        catch (Exception ex) { StartupDiagnostics.Record("local-sqlite", ex); throw; }

#if DEMO_BUILD
        Title = "Članarina DEMO — by BRENDIGO";
#endif
        ConfigureInitialWindowBounds();
        _ready = true;
        Navigation.SizeChanged += (_, _) =>
        {
            if (!_ready) return;
            var count = DashboardColumns();
            if (count != _lastLayoutColumns && (_module is "dashboard" or "reports"))
                Draw();
        };
        Navigation.SelectionChanged += Navigation_SelectionChanged;
        Navigation.SelectedItem = Navigation.MenuItems[0];
        Draw();
    }


    /// <summary>
    /// Minimum native WinUI bootstrap. No MainWindow.xaml, SVG or runtime XAML parsing.
    /// The NavigationView is created only after the Window itself exists.
    /// </summary>
    private void InitializeShell()
    {
        Navigation = new NavigationView
        {
            PaneDisplayMode = NavigationViewPaneDisplayMode.Auto,
            CompactModeThresholdWidth = 720,
            ExpandedModeThresholdWidth = 1120,
            OpenPaneLength = 238,
            CompactPaneLength = 48,
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            IsSettingsVisible = false
        };
        var header = new StackPanel
        {
            Spacing = 4,
            Padding = new Thickness(12, 16, 10, 12)
        };
        header.Children.Add(new TextBlock
        {
            Text = "Članarina", FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(BrandPalette.Navy),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        header.Children.Add(new TextBlock
        {
            Text = "by BRENDIGO", FontSize = 12,
            Foreground = new SolidColorBrush(BrandPalette.Muted)
        });
        Navigation.PaneHeader = header;
        foreach (var (tag, name) in new (string Tag, string Name)[]
        {
            ("dashboard", "Nadzorna ploča"),
            ("members", "Članovi"),
            ("dues", "Članarine"),
            ("payments", "Uplate"),
            ("debts", "Dugovanja"),
            ("slips", "Uplatnice"),
            ("bank", "Bankovni izvodi"),
            ("reports", "Izvještaji"),
            ("license", "Licenca"),
            ("settings", "Postavke")
        })
            Navigation.MenuItems.Add(new NavigationViewItem { Tag = tag, Content = name });

        PageContent = new StackPanel
        {
            Padding = new Thickness(22, 16, 22, 16),
            Spacing = 14,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Navigation.Content = new ScrollViewer
        {
            Content = PageContent,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollMode = ScrollMode.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var host = new Grid
        {
            Background = new SolidColorBrush(BrandPalette.Background)
        };
        host.Children.Add(Navigation);
        Content = host;
    }

    // Start centered and windowed, never automatically maximized or full-screen.
    private void ConfigureInitialWindowBounds()
    {
        try
        {
            var work = DisplayArea.GetFromWindowId(
                AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
            var width = Math.Min(1160, Math.Max(320, work.Width - 56));
            var height = Math.Min(780, Math.Max(320, work.Height - 56));
            AppWindow.Resize(new SizeInt32(width, height));
            AppWindow.Move(new PointInt32(
                work.X + Math.Max(0, (work.Width - width) / 2),
                work.Y + Math.Max(0, (work.Height - height) / 2)));
        }
        catch (Exception ex)
        {
            // The default Windows size is a usable fallback if monitor APIs fail.
            StartupDiagnostics.Record("window-bounds", ex);
        }
    }

    private int DashboardColumns()
    {
        var width = Navigation?.ActualWidth ?? 0;
        return width < 470 ? 1 : width < 760 ? 2 : width < 1060 ? 3 : 5;
    }

    private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        // SelectedItem may change from XAML before the SQLite store exists.
        if (!_ready) return;
        if (args.SelectedItem is NavigationViewItem item)
        {
            _module = item.Tag?.ToString() ?? "dashboard";
            _query = "";
            _activeOnly = false;
            Draw();
        }
    }
}
