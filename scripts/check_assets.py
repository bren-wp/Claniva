#!/usr/bin/env python3
"""Static repo hygiene; does not claim that app screenshots are pixel-perfect."""
from pathlib import Path
import json
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
tokens = json.loads((ROOT / "design/tokens.json").read_text(encoding="utf-8"))
assert tokens["name"] == "Članarina"
for key in ("navy","blue","teal","gold","background","surface","ink","danger"):
    assert re.fullmatch(r"#[A-F0-9]{6}", tokens["colors"][key])
for svg in (ROOT / "branding").glob("*.svg"):
    ET.parse(svg)
pages = list((ROOT / "site").glob("*.html"))
assert len(pages) >= 20, "Expected a multipage web presentation"
for page in pages:
    body = page.read_text(encoding="utf-8")
    assert "Članarina" in body, page
    assert "<html lang=\"hr\">" in body, page
    for target in re.findall(r'href="([^"]+\.html)"', body):
        assert (page.parent / target).exists(), (page, target)
print("PASS: token palette, branding SVGs, %d web pages and internal links" % len(pages))
