#!/usr/bin/env python3
"""Capture each implemented Android navigation page on a real emulator.

Outputs screenshots and a machine-readable coverage report. This is NOT a
pixel-perfect approval: no screenshot is a reference baseline by itself.
Only test a disposable emulator/database; never mutate member data on devices.
"""
import argparse
import json
import re
import subprocess
import time
import xml.etree.ElementTree as ET
from pathlib import Path

SCREENS = [
    ("Početna", "Nadzorna ploča", "dashboard"),
    ("Članovi", "Članovi", "members"),
    ("Članarine", "Članarine", "dues"),
    ("Uplate", "Uplate", "payments"),
    ("Dugovanja", "Dugovanja", "debts"),
    ("Uplatnice", "Uplatnice", "slips"),
    ("Bankovni izvodi", "Bankovni izvodi", "bank"),
    ("Izvještaji", "Izvještaji", "reports"),
    ("Licenca", "Licenca", "license"),
    ("Postavke", "Postavke", "settings"),
]
BOUNDS = re.compile(r"\[(\d+),(\d+)\]\[(\d+),(\d+)\]")


def adb(*parts, timeout=25, binary=False):
    result = subprocess.run(["adb", *parts], capture_output=True, timeout=timeout)
    if result.returncode:
        raise RuntimeError(f"adb {' '.join(parts)}: {result.stderr.decode(errors='replace')[:200]}")
    return result.stdout if binary else result.stdout.decode("utf-8", errors="replace")


def bounds(node):
    match = BOUNDS.fullmatch(node.attrib.get("bounds", ""))
    if not match:
        return None
    left, top, right, bottom = map(int, match.groups())
    if right <= left or bottom <= top:
        return None
    return left, top, right, bottom


def center(box):
    x1, y1, x2, y2 = box
    return (x1 + x2) // 2, (y1 + y2) // 2


def window_tree():
    last_error = ""
    for attempt in range(3):
        try:
            adb("shell", "uiautomator", "dump", "/sdcard/clanarina-ui.xml", timeout=35)
            raw = adb("shell", "cat", "/sdcard/clanarina-ui.xml")
            tree = ET.fromstring(raw[raw.index("<?xml"):])
            return list(tree.iter("node"))
        except (ValueError, ET.ParseError, RuntimeError, subprocess.TimeoutExpired) as exc:
            last_error = str(exc)
            time.sleep(1)
    raise RuntimeError(f"Android accessibility tree unavailable: {last_error}")


def find_node(nodes, text, *, preferred=None):
    results = []
    for node in nodes:
        if text not in (node.attrib.get("text", ""), node.attrib.get("content-desc", "")):
            continue
        box = bounds(node)
        if box is not None:
            results.append((node, box))
    if preferred:
        results = [(node, box) for node, box in results if preferred(box)]
    return results[0][1] if results else None


def tap(box):
    x, y = center(box)
    adb("shell", "input", "tap", str(x), str(y))
    time.sleep(0.6)


def current_size():
    output = adb("shell", "wm", "size")
    found = re.findall(r"(\d+)x(\d+)", output)
    if not found:
        raise RuntimeError(f"Cannot parse Android display size: {output}")
    w, h = map(int, found[-1])
    return w, h


def capture(path):
    data = adb("exec-out", "screencap", "-p", timeout=20, binary=True)
    if not data.startswith(b"\x89PNG\r\n\x1a\n") or len(data) < 4096:
        raise RuntimeError(f"Invalid or empty real capture: {path}")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)
    return len(data)


def dismiss_launcher_anr():
    """Recover only an unrelated Pixel Launcher ANR on disposable CI emulators.

    Never dismiss or ignore a Članarina crash/error dialog. If the launcher
    overlay keeps returning, the following navigation assertion still fails.
    """
    nodes = window_tree()
    labels = " ".join(
        (node.attrib.get("text", "") + " " + node.attrib.get("content-desc", ""))
        for node in nodes
    )
    is_launcher = "Pixel Launcher" in labels or "com.google.android.apps.nexuslauncher" in labels
    is_anr = "isn't responding" in labels or "not responding" in labels or "ne odgovara" in labels
    if not (is_launcher and is_anr):
        return False
    print("EMULATOR SYSTEM ANR: Pixel Launcher only; recovering and reopening app", flush=True)
    adb("shell", "am", "force-stop", "com.google.android.apps.nexuslauncher")
    time.sleep(1)
    adb("shell", "am", "start", "-n", "hr.brendigo.clanarina/.MainActivity")
    time.sleep(2)
    return True


def navigate(section, expected, width, height):
    # Some API 35 Google-APIs emulators raise a launcher-not-responding dialog
    # after wm size/density changes. This is not a failure of our app, but it
    # masks all ten accessible navigation entries; dismiss only that exact ANR.
    for _ in range(2):
        if not dismiss_launcher_anr():
            break
    nodes = window_tree()
    overflow = find_node(nodes, "Otvori sve module")
    if overflow is not None:
        tap(overflow)
    else:
        # Geometry fallback for the top-right native IconButton only.
        adb("shell", "input", "tap", str(width - 28), "55")
        time.sleep(0.6)
    candidates = window_tree()
    # Popup lies on the right of the viewport, above the bottom navigation.
    box = find_node(candidates, section, preferred=lambda b:
                    center(b)[0] >= width * 0.38 and center(b)[1] < height - 100)
    if box is None:
        raise RuntimeError(f"Navigation item absent or clipped: {section}")
    tap(box)
    time.sleep(0.4)
    nodes = window_tree()
    # The page heading is left aligned above the content, not in bottom tabs.
    heading = find_node(nodes, expected, preferred=lambda b:
                        center(b)[0] < width * 0.66 and center(b)[1] < height * 0.48)
    if heading is None:
        raise RuntimeError(f"Page heading not rendered after navigation: {section} => {expected}")


def capture_dialog(width, height, folder):
    navigate("Članovi", "Članovi", width, height)
    nodes = window_tree()
    open_button = find_node(nodes, "+ Novi član")
    if open_button is None:
        raise RuntimeError("New member action missing from members page")
    tap(open_button)
    nodes = window_tree()
    if find_node(nodes, "Novi član") is None:
        raise RuntimeError("Member editor did not open")
    capture(folder / "member-new-dialog.png")
    save = find_node(nodes, "Spremi")
    if save is None:
        raise RuntimeError("Member editor save control missing")
    tap(save)
    nodes = window_tree()
    error = find_node(nodes, "Ime mora imati 2–150 znakova.")
    if error is None:
        raise RuntimeError("Member validation feedback not visible")
    capture(folder / "member-validation-error.png")
    close = find_node(nodes, "Odustani")
    if close is None:
        raise RuntimeError("Member editor cancel control missing")
    tap(close)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", type=Path, default=Path("qa/runtime/android/screens"))
    parser.add_argument("--strict", action="store_true", help="Fail on missing module or dialog capture")
    args = parser.parse_args()
    width, height = current_size()
    folder = args.out / f"{width}x{height}"
    report = {"profile": f"{width}x{height}", "approvedPixelBaseline": False,
              "expected": len(SCREENS), "screens": [], "dialogs": {}}
    failures = []
    for name, expected, slug in SCREENS:
        try:
            navigate(name, expected, width, height)
            length = capture(folder / f"{slug}.png")
            report["screens"].append({"name": name, "id": slug, "status": "captured-unverified",
                                      "bytes": length})
            print(f"CAPTURED: {name} ({length} bytes)", flush=True)
        except Exception as exc:
            failures.append(f"{name}: {exc}")
            report["screens"].append({"name": name, "id": slug, "status": "failed",
                                      "error": str(exc)})
            print(f"NOT CAPTURED: {name}: {exc}", flush=True)
    try:
        capture_dialog(width, height, folder)
        report["dialogs"] = {"new-member": "captured-unverified",
                             "member-validation": "captured-unverified"}
    except Exception as exc:
        failures.append("Member dialogs: " + str(exc))
        report["dialogs"] = {"status": "failed", "error": str(exc)}
        print(f"NOT CAPTURED: member dialogs: {exc}", flush=True)
    folder.mkdir(parents=True, exist_ok=True)
    (folder / "coverage.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n",
                                           encoding="utf-8")
    print(f"Android page coverage: {len(SCREENS) - sum(x['status'] == 'failed' for x in report['screens'])}/{len(SCREENS)}; "
          f"failures: {len(failures)}", flush=True)
    if args.strict and failures:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
