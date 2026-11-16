#!/usr/bin/env python3
"""Static guardrails for form layout; native runtime and screenshot QA remain mandatory."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
android_ledger = (root / "android/app/src/main/java/hr/brendigo/clanarina/ui/LedgerScreens.kt").read_text(encoding="utf-8")
android_member = (root / "android/app/src/main/java/hr/brendigo/clanarina/ui/MemberScreens.kt").read_text(encoding="utf-8")
windows = (root / "windows/Claniva/MainWindow.PaymentDialogs.cs").read_text(encoding="utf-8")

for fragment in (
    'memberSearch', 'selectorExpanded', 'DropdownMenu(', 'matches.take(50)',
    'selectedId = ""', 'Odaberite člana s popisa.', 'Modifier.imePadding()',
):
    assert fragment in android_ledger, f"Missing Android payment-form guard: {fragment}"
assert 'members.forEach { member ->\n                    Row' not in android_ledger, "Unbounded radio-list returned"
for fragment in ('Modifier.imePadding()', 'KeyboardType.Email',
                 'charges.take(50)', 'payments.take(50)'):
    assert fragment in android_member, f"Missing Android member-dialog guard: {fragment}"
for fragment in ('HorizontalAlignment = HorizontalAlignment.Stretch',
                 'MaxDropDownHeight = 260', 'IsTextSearchEnabled = true'):
    assert fragment in windows, f"Missing WinUI payment-dialog guard: {fragment}"
assert 'MinWidth = 260' not in windows, "Fixed-width selection clips compact Windows dialogs"
app = (root / "android/app/src/main/java/hr/brendigo/clanarina/AppShell.kt").read_text(encoding="utf-8")
for icon in ("nav_home", "nav_members", "nav_payments", "nav_more"):
    assert f"R.drawable.{icon}" in app, f"Missing semantic navigation icon {icon}"
    assert (root / f"android/app/src/main/res/drawable/{icon}.xml").is_file()
assert 'if (page == "Početna") "Nadzorna ploča" else page' in app
for metric in ("Članovi", "Zaduženo", "Evidentirano", "Dugovanja", "Preplate"):
    assert f'StatCard("{metric}"' in app, f"Android reports label diverged: {metric}"
capture = (root / "scripts/android_capture_screens.py").read_text(encoding="utf-8")
assert "captured-unverified" in capture and "approvedPixelBaseline" in capture
assert "android_capture_screens.py" in (root / "scripts/android_launch_smoke.sh").read_text(encoding="utf-8")
win_capture = (root / "scripts/windows_capture_screens.ps1").read_text(encoding="utf-8")
assert win_capture.count("Id='") == 10, "Native WinUI gallery must cover every navigable module"
assert "approvedPixelBaseline=$false" in win_capture, "Capture reports must never imply an approved baseline"
assert "Capture-WindowsModuleScreens" in (root / "scripts/windows_launch_smoke.ps1").read_text(encoding="utf-8")
widgets = (root / "android/app/src/main/java/hr/brendigo/clanarina/DashboardWidgets.kt").read_text(encoding="utf-8")
overview = (root / "android/app/src/main/java/hr/brendigo/clanarina/DashboardOverview.kt").read_text(encoding="utf-8")
for invariant in ("heightIn(min=96.dp)", "width(5.dp).height(64.dp)",
                  "padding(horizontal=16.dp, vertical=16.dp)", "Modifier.padding(16.dp)"):
    assert invariant in widgets, f"Shared card dimension diverged: {invariant}"
assert "Column(verticalArrangement = Arrangement.spacedBy(12.dp))" in overview
assert "fontSize=30.sp" in app, "Dashboard heading typography diverged from WinUI"
brand = (root / "android/app/src/main/java/hr/brendigo/clanarina/ui/BrandTheme.kt").read_text(encoding="utf-8")
for color in ("surfaceContainer=Color.White", "surfaceContainerHigh=Color.White",
              "surfaceContainerHighest=Color.White", "onSurfaceVariant=Brand.Muted",
              "outline=Brand.Stroke"):
    assert color in brand, f"Android dialog theme reverted to default purple/gray: {color}"
for source in (android_member, android_ledger):
    assert 'containerColor = Color.White' in source
    assert 'titleContentColor = Brand.Navy' in source
    assert 'textContentColor = Brand.Ink' in source
    assert 'shape = RoundedCornerShape(20.dp)' in source
assert 'label = { Text("E-mail (neobavezno)") }' in android_member
assert 'title = { Text("Detalji člana") }' in android_member
assert 'label = { Text("Pretraži ime ili grupu") }' in android_member
smoke = (root / "scripts/android_launch_smoke.sh").read_text(encoding="utf-8")
assert 'python3 scripts/android_capture_screens.py --strict' in smoke
for spec in ("small:360x640:160", "normal:412x915:160", "tablet:800x1280:160"):
    assert spec in smoke, f"Missing viewport capture profile: {spec}"
win_app = (root / "windows/Claniva/App.xaml.cs").read_text(encoding="utf-8")
assert 'CultureInfo.GetCultureInfo("hr-HR")' in win_app
assert 'CultureInfo.DefaultThreadCurrentCulture = croatian' in win_app
windows_view = (root / "windows/Claniva/MainWindow.Views.cs").read_text(encoding="utf-8")
dashboard = (root / "android/app/src/main/java/hr/brendigo/clanarina/DashboardOverview.kt").read_text(encoding="utf-8")
assert 'ShowRecentPayments();' in windows_view
assert 'Ledger("payment").Take(5)' in windows_view
assert 'recentPayments.forEachIndexed' in dashboard
assert '"Posljednje uplate"' in dashboard and '"Posljednje uplate"' in windows_view
assert 'db.ledger("payment").take(5)' in app
print("PASS: native dialog colors, 3 screenshot viewports, locale and genuine latest-payment dashboard parity")
print("NOTE: static UI contracts are not pixel-perfect screenshot certification")
