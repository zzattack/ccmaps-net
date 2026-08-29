"""Two-row before/after figure for a landed fix.

Row 1: engine, our old render, and where they disagreed. Row 2: the same three after the fix.
The difference panels carry the argument -- a placement error is legible there even when the two
colour panels look alike at a glance.
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

DIFF_RGB = (255, 64, 64)


def panel(cap, ct, meta_path, png_path, x, y, w, h):
    with open(meta_path) as fh:
        rm = json.load(fh)
    ren = palette_quantize(load_png(png_path))
    ca, ra, _ = align(cap, ct, ren, render_transform(rm))
    return ca[y : y + h, x : x + w], ra[y : y + h, x : x + w]


def diff_panel(a, b, tol=8):
    """The engine crop dimmed, with disagreeing pixels lit, so the shape of the error is visible
    against the terrain that produced it."""
    mask = np.abs(a.astype(np.int16) - b.astype(np.int16)).max(axis=2) > tol
    out = (a.astype(np.uint16) * 40 // 100).astype(np.uint8)
    out[mask] = DIFF_RGB
    return out, int(mask.sum())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--capture", required=True)
    ap.add_argument("--capture-png", required=True)
    ap.add_argument("--before", required=True, help="meta.json,render.png")
    ap.add_argument("--after", required=True, help="meta.json,render.png")
    ap.add_argument("--x", type=int, required=True)
    ap.add_argument("--y", type=int, required=True)
    ap.add_argument("--w", type=int, default=160)
    ap.add_argument("--h", type=int, default=120)
    ap.add_argument("--scale", type=int, default=4)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    with zipfile.ZipFile(args.capture) as z:
        cap_meta = json.loads(z.read("metadata.json"))
    cap = load_png(args.capture_png)
    ct = capture_transform(cap_meta)

    game, before = panel(cap, ct, *args.before.split(","), args.x, args.y, args.w, args.h)
    _, after = panel(cap, ct, *args.after.split(","), args.x, args.y, args.w, args.h)
    dbefore, nbefore = diff_panel(game, before)
    dafter, nafter = diff_panel(game, after)

    h, w = game.shape[:2]
    gap = 4
    sheet = np.full((h * 2 + gap, w * 3 + gap * 2, 3), 24, np.uint8)
    for row, panels in enumerate(((game, before, dbefore), (game, after, dafter))):
        for col, p in enumerate(panels):
            y0 = row * (h + gap)
            x0 = col * (w + gap)
            sheet[y0 : y0 + h, x0 : x0 + w] = p

    img = Image.fromarray(sheet)
    img.resize((img.width * args.scale, img.height * args.scale), Image.NEAREST).save(args.out)
    total = h * w
    print(f"wrote {args.out}: differing {nbefore} -> {nafter} px "
          f"({100 * nbefore / total:.1f}% -> {100 * nafter / total:.1f}% of the crop)")


if __name__ == "__main__":
    main()
