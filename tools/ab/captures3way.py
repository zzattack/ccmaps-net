"""Two captures and a render, cropped to the same window.

For showing a capture-side bug: what the capture produced before a fix, after it, and what our
renderer draws for the same place.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import zipfile

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abcompare import palette_quantize  # noqa: E402
from abdiff import align, capture_transform, load_png, render_transform  # noqa: E402


def cap_and_ren(cap_zip, cap_png, meta_path, ren_png, x, y, w, h):
    archive = zipfile.ZipFile(cap_zip)
    cm = json.loads(archive.read("metadata.json"))
    archive.close()
    with open(meta_path) as fh:
        rm = json.load(fh)
    cap = load_png(cap_png)
    ren = palette_quantize(load_png(ren_png))
    ca, ra, _ = align(cap, capture_transform(cm), ren, render_transform(rm))
    return ca[y:y + h, x:x + w], ra[y:y + h, x:x + w]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--before", required=True, help="zip,png")
    ap.add_argument("--after", required=True, help="zip,png")
    ap.add_argument("--render", required=True, help="meta.json,png")
    ap.add_argument("--x", type=int, required=True)
    ap.add_argument("--y", type=int, required=True)
    ap.add_argument("--w", type=int, default=300)
    ap.add_argument("--h", type=int, default=180)
    ap.add_argument("--scale", type=int, default=3)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    meta_path, ren_png = args.render.split(",")
    before, _ = cap_and_ren(*args.before.split(","), meta_path, ren_png, args.x, args.y, args.w, args.h)
    after, ours = cap_and_ren(*args.after.split(","), meta_path, ren_png, args.x, args.y, args.w, args.h)

    h, w = before.shape[:2]
    gap = 6
    strip = np.full((h, w * 3 + gap * 2, 3), 32, np.uint8)
    strip[:, :w] = before
    strip[:, w + gap:w * 2 + gap] = after
    strip[:, w * 2 + gap * 2:] = ours
    img = Image.fromarray(strip)
    img.resize((img.width * args.scale, img.height * args.scale), Image.NEAREST).save(args.out)
    print(f"wrote {args.out} (capture before | capture after | our render) at {args.scale}x")


if __name__ == "__main__":
    main()
