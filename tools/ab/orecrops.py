"""Cut matched engine/render crops for the interactive ore viewer.

Writes <name>-engine.png and <name>-ours.png at 1:1, already aligned, so a viewer can overlay them
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


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--ab-root", required=True, help="directory holding per-map abcompare output")
    ap.add_argument("--captures", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--samples", nargs="+", required=True,
                    help="name:map:cluster  e.g. ore-bridgegap:bridgegap:0")
    ap.add_argument("--size", type=int, default=480)
    args = ap.parse_args()

    os.makedirs(args.out, exist_ok=True)
    manifest = []

    for spec in args.samples:
        name, map_name, cluster_idx = spec.split(":")
        cluster_idx = int(cluster_idx)

        zip_path = os.path.join(args.captures, map_name + ".zip")
        png_path = os.path.join(args.captures, map_name + ".png")
        meta_path = os.path.join(args.ab_root, map_name, "render.meta.json")
        ren_path = os.path.join(args.ab_root, map_name, "render.png")
        rep_path = os.path.join(args.ab_root, map_name, "report.json")

        archive = zipfile.ZipFile(zip_path)
        cap_meta = json.loads(archive.read("metadata.json"))
        archive.close()
        with open(meta_path) as fh:
            ren_meta = json.load(fh)
        with open(rep_path) as fh:
            report = json.load(fh)

        cap = load_png(png_path)
        ren = palette_quantize(load_png(ren_path))
        ca, ra, _ = align(cap, capture_transform(cap_meta), ren, render_transform(ren_meta))

        box = report["clusters"][cluster_idx]
        cx = (box["x0"] + box["x1"]) // 2
        cy = (box["y0"] + box["y1"]) // 2
        half = args.size // 2
        x0 = max(0, min(ca.shape[1] - args.size, cx - half))
        y0 = max(0, min(ca.shape[0] - args.size, cy - half))

        eng = ca[y0:y0 + args.size, x0:x0 + args.size]
        our = ra[y0:y0 + args.size, x0:x0 + args.size]
        Image.fromarray(eng).save(os.path.join(args.out, f"{name}-engine.png"))
        Image.fromarray(our).save(os.path.join(args.out, f"{name}-ours.png"))

        delta = np.abs(eng.astype(np.int16) - our.astype(np.int16)).max(axis=2)
        pct = 100.0 * (delta > 8).sum() / delta.size
        manifest.append({"name": name, "map": map_name, "differ": round(pct, 2),
                         "size": args.size})
        print(f"{name:<20} {map_name:<12} {args.size}x{args.size}  {pct:.2f}% differ")

    with open(os.path.join(args.out, "manifest.json"), "w") as fh:
        json.dump(manifest, fh, indent=2)


if __name__ == "__main__":
    main()
