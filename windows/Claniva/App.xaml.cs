using Microsoft.UI.Xaml;

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
