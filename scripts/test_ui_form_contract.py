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
print("PASS: Android/WinUI compact forms, searchable member picker, bounded long lists")
print("NOTE: static UI contracts are not pixel-perfect screenshot certification")
