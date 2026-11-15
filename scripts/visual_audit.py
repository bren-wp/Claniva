#!/usr/bin/env python3
"""Strict zero-tolerance screenshot comparison; requires real screenshots."""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageChops
ROOT=Path(__file__).resolve().parents[1]
def manifest():
    data=json.loads((ROOT/"qa/visual-manifest.json").read_text(encoding="utf-8"))
    assert data["product"]=="Članarina"
    assert data["thresholds"]=={"maxDifferentPixels":0,"maxAbsoluteChannelError":0}
    assert len(data["screens"])>=40
    assert len({s["id"] for s in data["screens"]})==len(data["screens"])
    for s in data["screens"]:
        assert s["platforms"]==["windows","android","windows-demo"]
        assert all(k in s["states"] for k in ("default","empty","populated","validation-error","dialog"))
    return data

def compare(reference, actual, out=None):
    if not reference.is_file() or not actual.is_file():
        raise FileNotFoundError("Missing approved baseline or real capture")
    with Image.open(reference) as a, Image.open(actual) as b:
        if a.size!=b.size: raise ValueError("Image dimensions differ")
        delta=ImageChops.difference(a.convert("RGBA"),b.convert("RGBA"))
        changed=sum(1 for p in delta.getdata() if any(p))
        max_channel=max((max(p) for p in delta.getdata()),default=0)
        if out:
            out.parent.mkdir(parents=True,exist_ok=True)
            delta.save(out)
        return changed,max_channel

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--validate-manifest",action="store_true")
    parser.add_argument("--audit-all",action="store_true")
    parser.add_argument("--reference",type=Path)
    parser.add_argument("--actual",type=Path)
    parser.add_argument("--diff",type=Path)
    args=parser.parse_args()
    doc=manifest()
    if args.validate_manifest:
        print("PASS: 40+ screens, two platforms, mandatory visual states")
        return
    if args.audit_all:
        unverified=0
        checks=0
        for s in doc["screens"]:
            for platform in s["platforms"]:
                for state in s["states"]:
                    rel=Path(platform)/s["id"]/(state+".png")
                    checks+=1
                    try:
                        changed, max_channel=compare(ROOT/"qa/baselines"/rel,ROOT/"qa/captures"/rel)
                        if changed or max_channel:
                            unverified+=1
                            print(f"NOT 1:1: {rel}, {changed} pixels differ")
                    except (OSError,ValueError) as exc:
                        unverified+=1
                        if unverified<=20: print(f"UNVERIFIED {rel}: {exc}")
        print(f"Checked {checks} states; failed or missing {unverified}")
        if unverified: raise SystemExit(1)
        return
    if args.reference is None or args.actual is None:
        parser.error("Provide --audit-all or --reference and --actual")
    try:
        changed,max_channel=compare(args.reference,args.actual,args.diff)
    except (OSError,ValueError) as exc:
        parser.exit(1,str(exc)+"\n")
    print(f"Changed pixels: {changed}; max channel delta: {max_channel}")
    if changed or max_channel: raise SystemExit(1)
    print("PASS: exactly equal pixels")

if __name__=="__main__": main()
