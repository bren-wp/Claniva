#!/usr/bin/env python3
"""SQLite financial invariants and Windows/Android schema contract checks.

This tests the shared SQL behavior with realistic edge cases, but does not run
the native Android/WinUI UI and is not a substitute for device-level tests.
"""
import sqlite3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
android = (ROOT / "android/app/src/main/java/hr/brendigo/clanarina/data/LocalStore.kt").read_text(encoding="utf-8")
windows = (ROOT / "windows/Claniva/Data/LocalStore.cs").read_text(encoding="utf-8")
for field in ("period", "reference", "audit_events"):
    assert field in android and field in windows, field
for identifier in ("ux_charge_period", "ux_payment_reference"):
    assert identifier in android and identifier in windows, identifier

db = sqlite3.connect(":memory:")
db.execute("PRAGMA foreign_keys = ON")
# Legacy v1 rows: identity and cent amounts must survive migration.
db.executescript("""
CREATE TABLE members (
    id TEXT PRIMARY KEY, name TEXT NOT NULL, group_name TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL DEFAULT '', active INTEGER NOT NULL DEFAULT 1
);
CREATE TABLE charges (
    id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id),
    amount_cents INTEGER NOT NULL CHECK(amount_cents > 0),
    note TEXT NOT NULL DEFAULT '', created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE payments (
    id TEXT PRIMARY KEY, member_id TEXT NOT NULL REFERENCES members(id),
    amount_cents INTEGER NOT NULL CHECK(amount_cents > 0),
    note TEXT NOT NULL DEFAULT '', created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
INSERT INTO members(id,name) VALUES ('A','Testni član A'),('B','Testni član B'),('C','Testni član C');
INSERT INTO charges(id,member_id,amount_cents) VALUES ('a1','A',2500),('a2','A',2500),('b1','B',2500);
INSERT INTO payments(id,member_id,amount_cents) VALUES ('pa','A',2500),('pb','B',5000);
""")
old_counts = db.execute("SELECT (SELECT COUNT(*) FROM members),(SELECT COUNT(*) FROM charges),(SELECT COUNT(*) FROM payments)").fetchone()
db.executescript("""
ALTER TABLE charges ADD COLUMN period TEXT NOT NULL DEFAULT '';
ALTER TABLE payments ADD COLUMN reference TEXT NOT NULL DEFAULT '';
CREATE UNIQUE INDEX ux_charge_period ON charges(member_id,period) WHERE period <> '';
CREATE UNIQUE INDEX ux_payment_reference ON payments(member_id,reference) WHERE reference <> '';
""")
assert old_counts == (3, 3, 2)
assert db.execute("SELECT period FROM charges WHERE id='a1'").fetchone()[0] == ""
assert db.execute("SELECT reference FROM payments WHERE id='pa'").fetchone()[0] == ""

# New unique periods and references must reject repeated entries for the same member.
db.execute("INSERT INTO charges(id,member_id,amount_cents,period) VALUES ('a3','A',2500,'2026-11')")
try:
    db.execute("INSERT INTO charges(id,member_id,amount_cents,period) VALUES ('dup','A',2500,'2026-11')")
except sqlite3.IntegrityError:
    pass
else:
    raise AssertionError("Duplicate member-period charge was accepted")
db.execute("INSERT INTO payments(id,member_id,amount_cents,reference) VALUES ('p1','A',300,'DEMO-001')")
try:
    db.execute("INSERT INTO payments(id,member_id,amount_cents,reference) VALUES ('dup','A',300,'DEMO-001')")
except sqlite3.IntegrityError:
    pass
else:
    raise AssertionError("Duplicate member reference was accepted")

totals = db.execute("""
SELECT m.id,
    COALESCE((SELECT SUM(amount_cents) FROM charges WHERE member_id=m.id),0) charged,
    COALESCE((SELECT SUM(amount_cents) FROM payments WHERE member_id=m.id),0) paid
FROM members m ORDER BY m.id
""").fetchall()
debts = sum(max(0, charged-paid) for _,charged,paid in totals)
credits = sum(max(0, paid-charged) for _,charged,paid in totals)
assert debts == 4700 and credits == 2500, (debts,credits,totals)
# If we used max(sum(charges) - sum(payments), 0), we would improperly
# offset one member's prepayment against another's overdue membership.
assert debts != max(0, sum(c-p for _,c,p in totals))
assert db.execute("PRAGMA foreign_key_check").fetchall() == []
print("PASS: v1 data retention, v2 deduplication, foreign keys, per-member debt and prepayment semantics")
