"""Crop a render around a map cell, using the transform in its --meta-json.

Saves hunting for a building by eye in a several-thousand-pixel image.
"""

from __future__ import annotations

import argparse
import json
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abdiff import render_transform  # noqa: E402


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--render-meta", required=True)
    ap.add_argument("--render-png", required=True)
    ap.add_argument("--rx", type=int, required=True)
    ap.add_argument("--ry", type=int, required=True)
    ap.add_argument("--w", type=int, default=420)
    ap.add_argument("--h", type=int, default=260)
    ap.add_argument("--scale", type=int, default=2)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    with open(args.render_meta) as fh:
        meta = json.load(fh)
    t = render_transform(meta)
    x, y = t.pixel(args.rx, args.ry)

    img = Image.open(args.render_png).convert("RGB")
    left = max(0, x - args.w // 2)
    top = max(0, y - args.h // 2)
    right = min(img.width, left + args.w)
    bottom = min(img.height, top + args.h)
    crop = img.crop((left, top, right, bottom))
    if args.scale != 1:
        crop = crop.resize((crop.width * args.scale, crop.height * args.scale), Image.NEAREST)
    crop.save(args.out)
    print(f"cell ({args.rx},{args.ry}) -> pixel ({x},{y}); wrote {args.out}")


if __name__ == "__main__":
    main()
