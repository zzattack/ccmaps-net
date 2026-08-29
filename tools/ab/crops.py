"""Cut aligned engine / before / after crops for a writeup.

Every panel comes out at 1:1 in the same coordinate frame, so a viewer can blink between them
without doing any geometry of its own.
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


def cut(captures, roots, map_name, x, y, w, h):
    with zipfile.ZipFile(os.path.join(captures, map_name + ".zip")) as z:
        cm = json.loads(z.read("metadata.json"))
    ct = capture_transform(cm)
    cap_full = load_png(os.path.join(captures, map_name + ".png"))

    out = []
    engine = None
    for root in roots:
        with open(os.path.join(root, map_name, "render.meta.json")) as fh:
            rm = json.load(fh)
        a, b = align(cap_full, ct, palette_quantize(load_png(os.path.join(root, map_name, "render.png"))),
                     render_transform(rm))[:2]
        if engine is None:
            engine = a[y:y + h, x:x + w]
        out.append(b[y:y + h, x:x + w])
    return engine, out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--spec", required=True, help="JSON list of crop definitions")
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    os.makedirs(args.out, exist_ok=True)
    manifest = []
    for item in json.load(open(args.spec)):
        engine, panels = cut(item["captures"], item["roots"], item["map"],
                             item["x"], item["y"], item["w"], item["h"])
        name = item["name"]
        Image.fromarray(engine).save(os.path.join(args.out, f"{name}-engine.png"))
        for label, p in zip(item["labels"], panels):
            Image.fromarray(p).save(os.path.join(args.out, f"{name}-{label}.png"))
        stats = {}
        for label, p in zip(item["labels"], panels):
            d = np.abs(engine.astype(np.int32) - p.astype(np.int32)).max(axis=2)
            stats[label] = round(100.0 * (d > 8).mean(), 2)
        manifest.append({"name": name, "map": item["map"], "title": item["title"],
                         "labels": item["labels"], "roles": ["engine"] + item["labels"],
                         "differ": stats, "w": item["w"], "h": item["h"]})
        print(f"{name:<22}{item['map']:<13}{item['w']}x{item['h']}  " +
              "  ".join(f"{k} {v}%" for k, v in stats.items()))

    with open(os.path.join(args.out, "manifest.json"), "w") as fh:
        json.dump(manifest, fh, indent=2)


if __name__ == "__main__":
    main()
