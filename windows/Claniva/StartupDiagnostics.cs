using System.Runtime.InteropServices;
using System.Text;

namespace Claniva;

/// <summary>
/// Local, opt-in-compatible technical startup diagnostics. No member/financial data is collected,
/// and nothing is sent over the network. Diagnostics are capped and stored separately per build.
/// </summary>
internal static class StartupDiagnostics
{
#if DEMO_BUILD
    private const string ProductFolder = "ClanarinaDemo";
#else
    private const string ProductFolder = "Clanarina";
#endif

    internal static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ProductFolder, "logs", "startup.log");

    internal static void Record(string stage, Exception ex)
    {
        try
        {
            var path = LogPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (File.Exists(path) && new FileInfo(path).Length > 512_000)
                File.Move(path, path + ".old", true);
            // Never log SQL queries, member details, license keys or user-controlled input.
            // XAML parser details are useful for diagnosing HRESULT 0x802B000A.
            // Never add arbitrary user-entered strings to this log.
            var parserDetail = stage.Contains("xaml", StringComparison.OrdinalIgnoreCase)
                ? ex.Message.Replace('\r', ' ').Replace('\n', ' ')
                : string.Empty;
            if (parserDetail.Length > 420) parserDetail = parserDetail[..420];
            var text = $"{DateTimeOffset.Now:O} stage={stage} type={ex.GetType().FullName} hresult=0x{ex.HResult:X8} detail={parserDetail}{Environment.NewLine}";
            File.AppendAllText(path, text, new UTF8Encoding(false));
        }
        catch
        {
            // Diagnostics must never create a second fatal exception.
        }
    }

    // Technical status only; never log members, payments or license keys.
    internal static void MarkReady()
    {
        try
        {
            var path = LogPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (File.Exists(path) && new FileInfo(path).Length > 512_000)
                File.Move(path, path + ".old", true);
            File.AppendAllText(path,
                $"{DateTimeOffset.UtcNow:O} stage=main-window-activated pid={Environment.ProcessId}{Environment.NewLine}",
                new UTF8Encoding(false));
        }
        catch { /* Diagnostics must not break the UI. */ }
    }

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr owner, string message, string caption, uint type);

    internal static void ShowStartupFailure(Exception ex)
    {
        Record("startup", ex);
        try
        {
            MessageBox(IntPtr.Zero,
                "Članarina se nije mogla pokrenuti. Podaci nisu obrisani.\n" +
                "Lokalni tehnički zapis nalazi se ovdje:\n" + LogPath +
                "\n\nŠifra pogreške: 0x" + ex.HResult.ToString("X8"),
                "Članarina — pogreška pri pokretanju", 0x10);
        }
        catch { }
    }
}
