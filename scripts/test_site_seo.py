#!/usr/bin/env python3
"""SEO and keyboard-accessibility regression contract for static shared-hosting site."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
site = root / "site"
pages = sorted(site.glob("*.html"))
assert len(pages) >= 21, "Expected all 21 advertised pages"

for page in pages:
    html = page.read_text(encoding="utf-8")
    title = re.findall(r"<title>([^<]+)</title>", html)
    descriptions = re.findall(r'<meta name="description" content="([^"]+)"', html)
    og_title = re.findall(r'<meta property="og:title" content="([^"]+)"', html)
    og_description = re.findall(r'<meta property="og:description" content="([^"]+)"', html)

    assert len(title) == 1 and len(descriptions) == 1, f"Missing/duplicate title or description: {page}"
    assert len(og_title) == len(og_description) == 1, f"Missing Open Graph metadata: {page}"
    assert og_title[0] == title[0] and og_description[0] == descriptions[0], f"Stale social metadata: {page}"
    assert '<meta property="og:locale" content="hr_HR">' in html, page
    assert '<meta property="og:type" content="website">' in html, page
    assert html.count('class="skip-link"') == 1, f"Missing accessible skip link: {page}"
    assert html.count('id="glavni-sadrzaj"') == 1, f"Missing skip target: {page}"
    assert re.search(r"<h1(?:\s[^>]*)?>.+?</h1>", html, re.S), f"Missing H1: {page}"
    assert "instalacijski paketi i Google Play objava još su u izradi" not in html, f"Stale release information: {page}"

css = (site / "style.css").read_text(encoding="utf-8")
assert css.count("\n") >= 50, "Stylesheet must remain maintainable and formatted"
assert ".skip-link:focus" in css, "Keyboard skip link must be visible on focus"
print(f"PASS: {len(pages)} static pages, matching OG metadata, H1, skip links and release-status copy")
print("NOTE: canonical URLs and sitemap await a verified public production domain")
