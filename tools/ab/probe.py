"""Ad-hoc probe: is a divergence structural (different sprite) or tonal (same sprite, shifted)?

A tonal shift collapses as the tolerance rises and leaves the two means equal; a structural
difference holds at any tolerance because the pixels are in different places.
"""

from __future__ import annotations

import json
import os
import sys
import zipfile

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abcompare import palette_quantize  # noqa: E402
from abdiff import align, capture_transform, diff_mask, load_png, render_transform  # noqa: E402

cap_zip = sys.argv[1]
cap_png = sys.argv[2]
ren_meta = sys.argv[3]
ren_png = sys.argv[4]

archive = zipfile.ZipFile(cap_zip)
cm = json.loads(archive.read("metadata.json"))
archive.close()
with open(ren_meta) as fh:
    rm = json.load(fh)

cap = load_png(cap_png)
ren_raw = load_png(ren_png)

for label, ren in (("quantized", palette_quantize(ren_raw)), ("raw 8-bit", ren_raw)):
    ca, ra, _ = align(cap, capture_transform(cm), ren, render_transform(rm))
    print(f"\n--- render {label} ---")
    for tol in (4, 8, 16, 32, 64):
        m = diff_mask(ca, ra, tol)
        print(f"  tolerance {tol:>3}: {100.0 * m.sum() / m.size:6.2f}% differ")

    m = diff_mask(ca, ra, 8)
    if m.sum():
        print(f"  on differing pixels: game mean {ca[m].mean(axis=0).round(1)}  "
              f"ours {ra[m].mean(axis=0).round(1)}")
    print(f"  whole image:        game mean {ca.reshape(-1,3).mean(axis=0).round(1)}  "
          f"ours {ra.reshape(-1,3).mean(axis=0).round(1)}")
