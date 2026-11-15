#!/usr/bin/env bash
set -euo pipefail
APK="android/app/build/outputs/apk/debug/app-debug.apk"
if [[ $# -gt 0 ]]; then APK="$1"; fi
PKG="hr.brendigo.clanarina"
OUT="$PWD/qa/runtime/android"
mkdir -p "$OUT"
test -s "$APK"
adb wait-for-device
adb install -r "$APK" | tee "$OUT/install.txt"
adb logcat -c
for pass in first second; do
  if [[ "$pass" == second ]]; then adb shell am force-stop "$PKG"; fi
  result="$(adb shell am start -W -n "$PKG/.MainActivity" 2>&1)"
  printf '%s\n' "$result" | tee "$OUT/launch-$pass.txt"
  if ! grep -q "Status: ok" <<< "$result"; then
    echo "::error::Android Activity Manager did not confirm launch"
    exit 1
  fi
  sleep 8
  if ! adb shell pidof "$PKG" > "$OUT/pid-$pass.txt"; then
    adb logcat -d -v time > "$OUT/logcat.txt" || true
    echo "::error::Android process exited unexpectedly"
    exit 1
  fi
  adb shell screencap -p "/sdcard/Clanarina-$pass.png"
  adb pull "/sdcard/Clanarina-$pass.png" "$OUT/$pass.png" >/dev/null
  test -s "$OUT/$pass.png"
done
adb logcat -d -v time > "$OUT/logcat.txt" || true
if grep -E -A 4 'FATAL EXCEPTION|Process: hr\.brendigo\.clanarina' "$OUT/logcat.txt" > "$OUT/fatal.txt"; then
  echo "::error::Fatal Android error recorded by logcat"
  cat "$OUT/fatal.txt"
  exit 1
fi
# Smoke-test real rendering on three viewport sizes; golden-screen approval is separate.
restore_display() {
  adb shell wm size reset >/dev/null 2>&1 || true
  adb shell wm density reset >/dev/null 2>&1 || true
}
trap restore_display EXIT
for spec in "small:360x640:320" "normal:412x915:360" "tablet:800x1280:320"; do
  IFS=: read -r label size dpi <<< "$spec"
  adb shell wm size "$size"
  adb shell wm density "$dpi"
  sleep 3
  if ! adb shell pidof "$PKG" >/dev/null; then
    echo "::error::Android app stopped in responsive viewport $label ($size @ $dpi)"
    exit 1
  fi
  adb shell screencap -p "/sdcard/Clanarina-$label.png"
  adb pull "/sdcard/Clanarina-$label.png" "$OUT/$label.png" >/dev/null
  test -s "$OUT/$label.png"
done

echo "PASS: actual emulator Activity started twice and remained alive; screenshots captured."
