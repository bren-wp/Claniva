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
    private bool? _lastCompact;
    private int _visibleMembers = 30;

    public MainWindow()
    {
        // Avoid MainWindow XAML parser completely: create native WinUI controls in C#.
        try { InitializeShell(); }
        catch (Exception ex) { StartupDiagnostics.Record("native-shell-bootstrap", ex); throw; }

        try { _store = new LocalStore(); }
        catch (Exception ex) { StartupDiagnostics.Record("local-sqlite", ex); throw; }

#if DEMO_BUILD
        Title = "Članarina DEMO — by BRENDIGO";
#else
        Title = "Članarina — by BRENDIGO";
#endif
        ConfigureInitialWindowBounds();
        _ready = true;
        Navigation.SizeChanged += (_, _) =>
        {
            if (!_ready) return;
            var count = DashboardColumns();
            var compact = Navigation.ActualWidth < 740;
            if (count != _lastLayoutColumns || _lastCompact != compact)
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
            CompactModeThresholdWidth = 660,
            ExpandedModeThresholdWidth = 900,
            OpenPaneLength = 236,
            CompactPaneLength = 48,
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            IsSettingsVisible = false,
            RequestedTheme = ElementTheme.Light,
            Background = new SolidColorBrush(BrandPalette.Background)
        };
        // WinUI NavigationView paints its pane and item states from theme resources,
        // not from the outer Grid. All states require contrast-safe color overrides.
        ApplyNavigationColors();
        var header = new StackPanel
        {
            Spacing = 4,
            Padding = new Thickness(12, 16, 10, 12)
        };
        header.Children.Add(new TextBlock
        {
            Text = "Članarina", FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(BrandPalette.White),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        header.Children.Add(new TextBlock
        {
            Text = "by BRENDIGO", FontSize = 12,
            Foreground = new SolidColorBrush(BrandPalette.SidebarSecondary)
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
        {
            var item = new NavigationViewItem
            {
                Tag = tag,
                Content = name,
                Icon = NavigationIcon(tag),
                Foreground = new SolidColorBrush(BrandPalette.White),
                MinHeight = 44
            };
            // Compact mode needs recognizable native icons, not truncated labels.
            ToolTipService.SetToolTip(item, name);
            Navigation.MenuItems.Add(item);
        }

        PageContent = new StackPanel
        {
            Padding = new Thickness(22, 16, 22, 16),
            Spacing = 14,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Navigation.Content = new ScrollViewer
        {
            Background = new SolidColorBrush(BrandPalette.Background),
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

    private static FontIcon NavigationIcon(string tag) => new()
    {
        // Segoe MDL2 Assets is available offline on Windows 10 and 11.
        // Every navigation entry has an icon so the Compact pane remains usable.
        FontFamily = new FontFamily("Segoe MDL2 Assets"),
        FontSize = 18,
        Glyph = tag switch
        {
            "dashboard" => "\uE80F", // Home
            "members" => "\uE716", // People
            "dues" => "\uE8D4", // Payment
            "payments" => "\uE8F1", // Cash
            "debts" => "\uE783", // Warning
            "slips" => "\uE8A5", // Document
            "bank" => "\uE80B", // World / bank records
            "reports" => "\uE9D9", // Analytics
            "license" => "\uE8D7", // Lock
            "settings" => "\uE713", // Settings
            _ => "\uE10F"
        }
    };

    private void ApplyNavigationColors()
    {
        // WinUI lightweight styling resources are scoped to this NavigationView,
        // leaving native system high-contrast behavior available when requested.
        var colors = new Dictionary<string, Color>
        {
            ["NavigationViewExpandedPaneBackground"] = BrandPalette.Navy,
            ["NavigationViewDefaultPaneBackground"] = BrandPalette.Navy,
            ["NavigationViewItemForeground"] = BrandPalette.White,
            ["NavigationViewItemForegroundPointerOver"] = BrandPalette.White,
            ["NavigationViewItemForegroundPressed"] = BrandPalette.White,
            ["NavigationViewItemForegroundSelected"] = BrandPalette.White,
            ["NavigationViewItemForegroundSelectedPointerOver"] = BrandPalette.White,
            ["NavigationViewItemForegroundSelectedPressed"] = BrandPalette.White,
            ["NavigationViewItemBackground"] = BrandPalette.Navy,
            ["NavigationViewItemBackgroundPointerOver"] = BrandPalette.NavyLight,
            ["NavigationViewItemBackgroundPressed"] = BrandPalette.NavyLight,
            ["NavigationViewItemBackgroundSelected"] = BrandPalette.Blue,
            ["NavigationViewItemBackgroundSelectedPointerOver"] = BrandPalette.Blue,
            ["NavigationViewItemBackgroundSelectedPressed"] = BrandPalette.Blue,
            ["NavigationViewSelectionIndicatorForeground"] = BrandPalette.Teal
        };
        foreach (var pair in colors)
            Navigation.Resources[pair.Key] = new SolidColorBrush(pair.Value);
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

    private void NavigateTo(string tag)
    {
        foreach (var item in Navigation.MenuItems)
        {
            if (item is NavigationViewItem navigationItem &&
                string.Equals(navigationItem.Tag?.ToString(), tag, StringComparison.Ordinal))
            {
                Navigation.SelectedItem = navigationItem;
                return;
            }
        }
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
            _visibleMembers = 30;
            Draw();
        }
    }
}
