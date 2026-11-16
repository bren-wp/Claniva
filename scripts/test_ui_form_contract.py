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
print("PASS: Android/WinUI compact forms, vector nav, aligned report metrics, per-screen capture coverage")
print("NOTE: static UI contracts are not pixel-perfect screenshot certification")
