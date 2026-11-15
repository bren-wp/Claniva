#!/usr/bin/env python3
"""Regression tests for exact screenshot comparator, no reference images fabricated."""
from pathlib import Path
from tempfile import TemporaryDirectory
from PIL import Image
from visual_audit import compare, manifest

def main():
    assert len(manifest()["screens"]) >= 40
    with TemporaryDirectory() as tmp:
        root=Path(tmp)
        a=root/"reference.png"
        b=root/"actual.png"
        Image.new("RGBA",(12,8),(15,31,58,255)).save(a)
        Image.new("RGBA",(12,8),(15,31,58,255)).save(b)
        assert compare(a,b)==(0,0), "Identical images rejected"
        different=Image.open(b).copy()
        different.putpixel((6,5),(15,31,59,255))
        different.save(b)
        changed,maximum=compare(a,b,root/"diff.png")
        assert changed==1 and maximum==1, "One-channel error was missed"
        assert (root/"diff.png").is_file()
        Image.new("RGBA",(12,9),(15,31,58,255)).save(b)
        try:
            compare(a,b)
        except ValueError:
            pass
        else:
            raise AssertionError("Different dimensions incorrectly accepted")
        b.unlink()
        try:
            compare(a,b)
        except FileNotFoundError:
            pass
        else:
            raise AssertionError("Missing capture incorrectly accepted")
    print("PASS: exact visual comparator detects 1 channel, dimensions and missing screenshots")

if __name__=="__main__":
    main()
