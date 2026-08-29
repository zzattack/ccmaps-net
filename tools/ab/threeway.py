"""Engine | before | after, cropped to the same window and magnified.

The shape a fix should be reviewed in: what the engine does, what we did, what we do now.
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


def panel(cap, ct, meta_path, png_path, x, y, w, h):
    with open(meta_path) as fh:
        rm = json.load(fh)
    ren = palette_quantize(load_png(png_path))
    ca, ra, _ = align(cap, ct, ren, render_transform(rm))
    return ca[y:y + h, x:x + w], ra[y:y + h, x:x + w]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--capture", required=True)
    ap.add_argument("--capture-png", required=True)
    ap.add_argument("--before", required=True, help="meta.json,render.png")
    ap.add_argument("--after", required=True, help="meta.json,render.png")
    ap.add_argument("--x", type=int, required=True)
    ap.add_argument("--y", type=int, required=True)
    ap.add_argument("--w", type=int, default=140)
    ap.add_argument("--h", type=int, default=110)
    ap.add_argument("--scale", type=int, default=4)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    archive = zipfile.ZipFile(args.capture)
    cap_meta = json.loads(archive.read("metadata.json"))
    archive.close()
    cap = load_png(args.capture_png)
    ct = capture_transform(cap_meta)

    game, before = panel(cap, ct, *args.before.split(","), args.x, args.y, args.w, args.h)
    _, after = panel(cap, ct, *args.after.split(","), args.x, args.y, args.w, args.h)

    h, w = game.shape[:2]
    gap = 6
    strip = np.full((h, w * 3 + gap * 2, 3), 32, np.uint8)
    strip[:, :w] = game
    strip[:, w + gap:w * 2 + gap] = before
    strip[:, w * 2 + gap * 2:] = after
    img = Image.fromarray(strip)
    img.resize((img.width * args.scale, img.height * args.scale), Image.NEAREST).save(args.out)
    print(f"wrote {args.out} (engine | before | after) at {args.scale}x")


if __name__ == "__main__":
    main()
