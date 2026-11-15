#!/usr/bin/env python3
"""Validate identity-critical colors in Windows, Android and web sources."""
import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
colors=json.loads((root/"design/tokens.json").read_text(encoding="utf-8"))["colors"]
android=(root/"android/app/src/main/java/hr/brendigo/clanarina/ui/BrandTheme.kt").read_text(encoding="utf-8")
windows=(root/"windows/Claniva/BrandPalette.cs").read_text(encoding="utf-8")
site=(root/"site/style.css").read_text(encoding="utf-8")
for token,upper in {"navy":"Navy","blue":"Blue","teal":"Teal","gold":"Gold","danger":"Danger"}.items():
    hex=colors[token].upper()
    if ("val "+upper+" = Color(0xFF"+hex[1:]+")") not in android:
        raise AssertionError("Android token "+token)
    if ('const string '+upper+'Hex = "'+hex+'";') not in windows:
        raise AssertionError("Windows token "+token)
    if ("--"+token+":"+hex).lower() not in site.lower():
        raise AssertionError("Web token "+token)
print("PASS: 5 canonical colors identical across Windows Android and web")
