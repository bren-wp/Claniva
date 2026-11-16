using Microsoft.UI.Xaml;
using System.Globalization;

namespace Claniva;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        try
        {
            InitializeComponent();
            UnhandledException += (_, args) =>
            {
                // Record the failure, but never pretend an unhandled UI fault is recovered.
                if (args.Exception is { } ex)
                    StartupDiagnostics.Record("unhandled-xaml", ex);
            };
        }
        catch (Exception ex)
        {
            StartupDiagnostics.ShowStartupFailure(ex);
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            // This iteration ships a Croatian-only UI. Match the declared
            // hr-HR visual QA profile independently of the Windows runner locale.
            // Once language switching is implemented, this becomes a persisted
            // per-installation setting rather than a hardcoded default.
            var croatian = CultureInfo.GetCultureInfo("hr-HR");
            CultureInfo.DefaultThreadCurrentCulture = croatian;
            CultureInfo.DefaultThreadCurrentUICulture = croatian;
            CultureInfo.CurrentCulture = croatian;
            CultureInfo.CurrentUICulture = croatian;
            _window = new MainWindow();
            _window.Activate();
            StartupDiagnostics.MarkReady();
        }
        catch (Exception ex)
        {
            StartupDiagnostics.ShowStartupFailure(ex);
            Exit();
        }
    }
}
