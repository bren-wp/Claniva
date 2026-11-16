#!/usr/bin/env python3
"""Check shared Članarina UI design invariants (not screenshot certification)."""
import json
from pathlib import Path
r=Path(__file__).resolve().parents[1]
tokens=json.loads((r/"design/tokens.json").read_text(encoding="utf-8"))["colors"]
def luma(hex_color):
    rgb=[int(hex_color[i:i+2],16)/255 for i in (1,3,5)]
    lin=[c/12.92 if c<=0.04045 else ((c+0.055)/1.055)**2.4 for c in rgb]
    return sum(a*b for a,b in zip((.2126,.7152,.0722),lin))
def contrast(a,b):
    x,y=sorted((luma(a),luma(b)))
    return (y+.05)/(x+.05)
for label,fg,bg in [
    ("Sidebar",tokens["surface"],tokens["navy"]),
    ("Primary buttons",tokens["surface"],tokens["blue"]),
    ("Body",tokens["ink"],tokens["background"]),
    ("Secondary text",tokens["muted"],tokens["surface"]),
    ("Debt status",tokens["danger"],tokens["surface"]),
    ("Positive status",tokens["successInk"],tokens["surface"])
]:
    ratio=contrast(fg,bg)
    assert ratio>=4.5, f"{label} contrast={ratio:.2f}, expected >=4.5:1"
    print(f"PASS: {label} contrast={ratio:.2f}:1")
win=(r/"windows/Claniva/MainWindow.cs").read_text(encoding="utf-8")
views=(r/"windows/Claniva/MainWindow.Views.cs").read_text(encoding="utf-8")
controls=(r/"windows/Claniva/MainWindow.Controls.cs").read_text(encoding="utf-8")
android=(r/"android/app/src/main/java/hr/brendigo/clanarina/AppShell.kt").read_text(encoding="utf-8")
dashboard=(r/"android/app/src/main/java/hr/brendigo/clanarina/DashboardOverview.kt").read_text(encoding="utf-8")
assert 'RequestedTheme="Light"' in (r/"windows/Claniva/App.xaml").read_text(encoding="utf-8")
for key in ["NavigationViewExpandedPaneBackground", "NavigationViewDefaultPaneBackground",
            "NavigationViewItemForeground", "NavigationViewItemBackgroundSelected"]:
    assert key in win, f"Missing navigation theme resource {key}"
assert 'Background = new SolidColorBrush(BrandPalette.Background)' in win
assert 'Navigation.SelectedItem = navigationItem' in win
assert 'ExpandedModeThresholdWidth = 900' in win
assert 'CompactModeThresholdWidth = 660' in win
assert 'ContentScroller?.ActualWidth' in win, "KPI columns must follow content viewport, not full window"
assert 'width < 440 ? 1 : width < 720 ? 2 : width < 1100 ? 3 : 5' in win
assert 'ContentScroller.SizeChanged' in win, "Viewport changes must reflow KPI cards"
assert 'new Grid { ColumnSpacing = 14, MinHeight = 64 }' in controls
assert 'GridUnitType.Star' in controls, "KPI texts need bounded grid columns"
assert 'TextWrapping.WrapWholeWords' in controls
assert 'Icon = NavigationIcon(tag)' in win and 'ToolTipService.SetToolTip(item, name)' in win
for glyph in (r'"dues" => "\uE8BF"', r'"payments" => "\uE8C7"', r'"bank" => "\uE825"'):
    assert glyph in win, f"Misleading native navigation icon: {glyph}"
assert 'new Thickness(14, 62, 14, 24)' in views, "Minimal nav header must not overlap DEMO/banner"
assert 'private static FontIcon NavigationIcon' in win
assert '_lastMinimal != minimal' in win, "A 740 -> 660dp transition must trigger re-render"
assert '_lastMinimal = Navigation.ActualWidth < 660' in views
smoke = (r/"scripts/windows_launch_smoke.ps1").read_text(encoding="utf-8")
assert 'function Assert-RenderedCapture' in smoke and 'function Wait-RenderedCapture' in smoke
assert '@(700, 540)' in smoke, "The compact-to-minimal transition needs a runtime check"
money = (r/"windows/Claniva/Data/LocalStore.Money.cs").read_text(encoding="utf-8")
assert 'CurrencySymbol = "€"' in money, "Windows currency must not revert to HRK"
assert 'Currency.getInstance("EUR")' in android, "Android currency must explicitly be EUR"

assert 'shortcuts.Children.Add(Action("Novo zaduženje", AddCharge_Click))' in views
assert 'shortcuts.Children.Add(Action("Nova uplata", AddPayment_Click))' in views
assert "members.Take(_visibleMembers)" in (r/"windows/Claniva/MainWindow.Members.cs").read_text(encoding="utf-8")
for label in ["Članovi","Zaduženo","Evidentirano","Dugovanja","Preplate"]:
    assert f'Metric("{label}"' in views, f"Missing Windows card {label}"
    assert f'Triple("{label}"' in dashboard, f"Missing Android card {label}"
assert 'maxWidth < 320.dp -> 1' in dashboard, "Typical phones should receive two KPI columns"
assert 'val compactLabels = maxWidth < 280.dp' in android
assert 'maxLines=1,overflow=TextOverflow.Ellipsis' in android
assert '"small:360x640:160" "normal:412x915:160"' in (r/"scripts/android_launch_smoke.sh").read_text(encoding="utf-8")
assert "ModalBottomSheet" in android and 'moreMenu=true' in android, "The More tab must open navigation"
assert "new SolidColorBrush(BrandPalette.White)" in controls
assert 'MemberManagement' in android and 'LedgerManagement' in android
print("PASS: shared navigation, 5 KPI cards, responsive shells and active controls")
print("NOTE: static checks do not replace actual 600-state screenshot comparisons")
