"""Mismatch of one or more renders against a single capture, for before/after comparisons.

  python measure.py <capture.zip> <capture.png> <label>=<meta.json>,<render.png> ...
"""

from __future__ import annotations

import json
import os
import sys
import zipfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abcompare import palette_quantize  # noqa: E402
from abdiff import align, capture_transform, diff_mask, load_png, render_transform  # noqa: E402

cap_zip, cap_png = sys.argv[1:3]
archive = zipfile.ZipFile(cap_zip)
cap_meta = json.loads(archive.read("metadata.json"))
archive.close()
cap = load_png(cap_png)
ct = capture_transform(cap_meta)

box = None
specs = []
for arg in sys.argv[3:]:
    if arg.startswith("box="):
        box = [int(v) for v in arg[4:].split(",")]  # x0,y0,x1,y1 in aligned coordinates
    else:
        specs.append(arg)

for spec in specs:
    label, _, paths = spec.partition("=")
    meta_path, png_path = paths.split(",")
    with open(meta_path) as fh:
        rm = json.load(fh)
    ren = palette_quantize(load_png(png_path))
    ca, ra, _ = align(cap, ct, ren, render_transform(rm))
    if box:
        x0, y0, x1, y1 = box
        ca, ra = ca[y0:y1, x0:x1], ra[y0:y1, x0:x1]
    m = diff_mask(ca, ra, 8)
    print(f"{label:<12} {100.0 * m.sum() / m.size:7.3f}% differ   ({int(m.sum())} px of {m.size})")
