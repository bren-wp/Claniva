#!/usr/bin/env python3
"""Native boot / split code invariants: mechanical checks, not device certification."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def load(path): return (root/path).read_text(encoding="utf-8")
win=root/"windows/Claniva"
assert not (win/"MainWindow.xaml").exists(), "Old startup XAML still in build"
assert not (win/"MainWindow.xaml.cs").exists()
assert 'XamlControlsResources' in load('windows/Claniva/App.xaml'), 'Default WinUI Fluent styles missing'
core=load("windows/Claniva/MainWindow.cs")
assert "InitializeShell();" in core and "InitializeComponent();" not in core
assert "Content = host;" in core and "new NavigationView" in core
assert "Navigation.SelectionChanged += Navigation_SelectionChanged" in core
assert core.index("InitializeShell();") < core.index("_store = new LocalStore();")
assert "DEMO_BUILD" in core
parts=[
 "MainWindow.Controls.cs","MainWindow.Views.cs","MainWindow.Members.cs",
 "MainWindow.Ledger.cs","MainWindow.MemberDialogs.cs","MainWindow.PaymentDialogs.cs"
]
for f in parts:
    text=(win/f).read_text(encoding="utf-8")
    assert "partial class MainWindow" in text, f
    assert len(text.splitlines()) <= 180, f
assert "DEMO IZDANJE" in load("windows/Claniva/MainWindow.Views.cs")
assert "ClanarinaDemo" in load("windows/Claniva/Data/LocalStore.cs")
assert "Clanarina" in load("windows/Claniva/Data/LocalStore.cs")
assert "ux_charge_period" in load("windows/Claniva/Data/LocalStore.cs")
assert "new NavigationView" in core
android=root/"android/app/src/main/java/hr/brendigo/clanarina"
for f in ["MainActivity.kt","AppShell.kt","DashboardWidgets.kt","ui/MemberScreens.kt",
          "ui/LedgerScreens.kt","ui/FinanceFormatting.kt"]:
    assert (android/f).is_file(), f
assert not (android/"ui/EnhancedScreens.kt").exists()
shell=load("android/app/src/main/java/hr/brendigo/clanarina/AppShell.kt")
assert 'form=="charge"' not in shell
assert "MemberTile" not in shell
assert "LedgerManagement" in shell and "MemberManagement" in shell
manifest=load("android/app/src/main/AndroidManifest.xml")
assert 'android:allowBackup="false"' in manifest
assert 'android:usesCleartextTraffic="false"' in manifest
assert 'android.permission.INTERNET' not in manifest, "Online licensing not production-ready"
release=load(".github/workflows/release.yml")
assert "android-launch, windows-launch" in release
assert "windows-2022, windows-2025" in release
print("PASS: native programmatic shell, separate DEMO/production, 7 WinUI modules, Android module cleanup and release launch gates")
