"""Embed PNGs losslessly as base64 data URIs.

The ore comparison is about individual pixels, so JPEG is not an option: its ringing would show up
as exactly the kind of per-pixel difference the page exists to inspect.
"""

from __future__ import annotations

import argparse
import base64
import glob
import io
import json
import os

from PIL import Image


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dir", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--quantize", type=int, default=0,
                    help="palette size; 0 keeps truecolour. The sources are palettised game art, so a "
                         "generous palette is lossless in practice and much smaller.")
    args = ap.parse_args()

    out = {}
    total = 0
    for path in sorted(glob.glob(os.path.join(args.dir, "*.png"))):
        key = os.path.splitext(os.path.basename(path))[0]
        img = Image.open(path).convert("RGB")
        if args.quantize:
            img = img.quantize(colors=args.quantize, method=Image.MEDIANCUT, dither=Image.NONE)
        buf = io.BytesIO()
        img.save(buf, "PNG", optimize=True)
        raw = buf.getvalue()
        out[key] = "data:image/png;base64," + base64.b64encode(raw).decode("ascii")
        total += len(raw)
        print(f"{key:<24}{len(raw)/1024:>8.0f} KB")

    with open(args.out, "w") as fh:
        json.dump(out, fh)
    print(f"\n{len(out)} images, {total/1024:.0f} KB raw -> {total*4/3/1024/1024:.2f} MB base64")


if __name__ == "__main__":
    main()
