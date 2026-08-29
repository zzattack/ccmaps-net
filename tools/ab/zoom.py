"""Crop the same window from a capture and a render, side by side, magnified.

Reading a divergence usually needs individual pixels, not a whole-map view: at 1:1 a wrong sprite
and a wrong palette look alike.
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


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--capture", required=True)
    ap.add_argument("--capture-png")
    ap.add_argument("--render-meta", required=True)
    ap.add_argument("--render-png", required=True)
    ap.add_argument("--x", type=int, required=True)
    ap.add_argument("--y", type=int, required=True)
    ap.add_argument("--w", type=int, default=150)
    ap.add_argument("--h", type=int, default=90)
    ap.add_argument("--scale", type=int, default=5)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    archive = zipfile.ZipFile(args.capture)
    cap_meta = json.loads(archive.read("metadata.json"))
    archive.close()

    with open(args.render_meta) as fh:
        ren_meta = json.load(fh)

    cap_png = args.capture_png or os.path.splitext(args.capture)[0] + ".png"
    cap = load_png(cap_png)
    ren = palette_quantize(load_png(args.render_png))

    ca, ra, _ = align(cap, capture_transform(cap_meta), ren, render_transform(ren_meta))

    y, x, h, w = args.y, args.x, args.h, args.w
    a = ca[y : y + h, x : x + w]
    b = ra[y : y + h, x : x + w]
    strip = np.full((h, w * 2 + 6, 3), 40, np.uint8)
    strip[:, :w] = a
    strip[:, w + 6 :] = b
    img = Image.fromarray(strip)
    img = img.resize((img.width * args.scale, img.height * args.scale), Image.NEAREST)
    img.save(args.out)
    print(f"wrote {args.out} (game | ours) at {args.scale}x")


if __name__ == "__main__":
    main()
