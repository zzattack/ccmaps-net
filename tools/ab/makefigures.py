"""Produce web-sized comparison figures from an abcompare run, as base64 data URIs.

Artifacts are self-contained: nothing loads from an external host, so every figure has to be
embedded. These are downscaled and JPEG-encoded to keep the page small enough to open quickly.
"""

from __future__ import annotations

import argparse
import base64
import io
import json
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))


def encode(img: Image.Image, max_w: int, quality: int) -> tuple[str, int]:
    if img.width > max_w:
        h = round(img.height * max_w / img.width)
        img = img.resize((max_w, h), Image.LANCZOS)
    buf = io.BytesIO()
    img.convert("RGB").save(buf, "JPEG", quality=quality, optimize=True)
    raw = buf.getvalue()
    return "data:image/jpeg;base64," + base64.b64encode(raw).decode("ascii"), len(raw)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--figures", nargs="+", required=True,
                    help="label=path.png pairs")
    ap.add_argument("--max-width", type=int, default=1600)
    ap.add_argument("--quality", type=int, default=82)
    ap.add_argument("--out", required=True, help="JSON file of label -> data URI")
    args = ap.parse_args()

    out = {}
    total = 0
    for spec in args.figures:
        label, _, path = spec.partition("=")
        if not os.path.exists(path):
            print(f"missing: {path}")
            continue
        uri, size = encode(Image.open(path), args.max_width, args.quality)
        out[label] = uri
        total += size
        print(f"{label:<24}{size/1024:>8.0f} KB")

    with open(args.out, "w") as fh:
        json.dump(out, fh)
    print(f"\n{len(out)} figures, {total/1024:.0f} KB before base64 -> {args.out}")


if __name__ == "__main__":
    main()
