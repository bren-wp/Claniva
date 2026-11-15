#!/usr/bin/env python3
"""Static release invariants. Not a substitute for real WinUI runtime tests."""
from pathlib import Path
import json
root=Path(__file__).resolve().parents[1]
def read(path):
    return (root/path).read_text(encoding="utf-8")
project=read("windows/Claniva/Claniva.csproj")
store=read("windows/Claniva/Data/LocalStore.cs")
demo=read("windows/Claniva/Data/DemoData.cs")
window=read("windows/Claniva/MainWindow.Views.cs")
nsis=read("windows/installer/Clanarina.nsi")
release=read(".github/workflows/release.yml")
manifest=json.loads(read("qa/visual-manifest.json"))
assert "DEMO_BUILD" in project and "ClanarinaDemo" in project
assert '"ClanarinaDemo", "data"' in store
assert '"Clanarina", "data"' in store
assert "DemoData.Seed(_connectionString)" in store
assert "demo_seed_state" in demo and "for (int i = 1; i <= 24; i++)" in demo
assert "example.invalid" in demo
assert "DEMO IZDANJE" in window
for token in ("DEMO_BUILD","ClanarinaDemo.exe","Clanarina-Demo","APP_FOLDER"):
    assert token in nsis, token
for f in ("Clanarina-Demo-Setup.exe","Clanarina-Demo-Portable.exe",
          "Clanarina-Setup.exe","Clanarina-Portable.exe","Clanarina-Android.apk",
          "Clanarina-Android.aab","Clanarina-Full-Source.zip","Clanarina-Web-Source.zip"):
    assert f in release, f
assert len(manifest["screens"]) >= 40
for screen in manifest["screens"]:
    assert screen["platforms"] == ["windows","android","windows-demo"]
    assert screen["status"] == "unverified"
print("PASS: compile-time demo, independent storage, synthetic data, six app artifacts and 600 QA states")
