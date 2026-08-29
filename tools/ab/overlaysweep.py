"""Measure the placement of each overlay class against the engine, map by map.

For every (map, class) it builds the exact pixel mask by rendering with the class removed, then
reports the whole-pixel offset that best reconciles our render with the capture. A class that is
placed correctly shows almost no disputed pixels at all; a displaced one shows a clear winning
offset that resolves most of them.
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import zipfile

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from classmask import render, strip_map  # noqa: E402
from offsetscan import load_pair, scan  # noqa: E402
from abdiff import align  # noqa: E402

CLASSES = {
    "ore": "102",
    "ore-vinifera": "127-146",
    "gems": "27",
    "rocks": "168-177",
    "pavement": "74-101",
    "lowbridge": "83-98,237,238",
    "bridge": "24,25,26,241",
    "fence": "2,203,204",
}


def lattice_of(captures, name):
    with zipfile.ZipFile(os.path.join(captures, name + ".zip")) as z:
        meta = json.loads(z.read("metadata.json"))
    return (meta.get("provenance") or {}).get("variantLattice")


def parse_ids(spec):
    ids = set()
    for part in spec.split(","):
        if "-" in part:
            lo, hi = part.split("-")
            ids.update(range(int(lo), int(hi) + 1))
        else:
            ids.add(int(part))
    return ids


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--ab-root", required=True, help="per-map output of the current abrun")
    ap.add_argument("--captures", required=True)
    ap.add_argument("--corpus", required=True)
    ap.add_argument("--mixdir", required=True)
    ap.add_argument("--renderer", required=True)
    ap.add_argument("--work", required=True)
    ap.add_argument("--maps", nargs="+", required=True)
    ap.add_argument("--classes", nargs="+", default=sorted(CLASSES))
    ap.add_argument("--radius", type=int, default=4)
    ap.add_argument("--engine", default="-Y")
    ap.add_argument("--out")
    args = ap.parse_args()

    from scipy import ndimage

    rows = []
    for name in args.maps:
        cap, ren, cap_full, ct, rt = load_pair(args.ab_root, args.captures, name)
        lattice = lattice_of(args.captures, name)
        work = os.path.join(args.work, name)
        os.makedirs(work, exist_ok=True)
        full_png = render(args.renderer, os.path.join(args.corpus, name + ".map"),
                          args.mixdir, work, "full", lattice, args.engine)
        from abdiff import load_png
        full_img = load_png(full_png)

        for cls in args.classes:
            ids = parse_ids(CLASSES[cls])
            stripped = os.path.join(work, cls + ".map")
            removed = strip_map(os.path.join(args.corpus, name + ".map"), ids, stripped)
            if removed == 0:
                continue
            less_png = render(args.renderer, stripped, args.mixdir, work, cls, lattice, args.engine)
            less_img = load_png(less_png)
            mask = np.abs(full_img.astype(np.int16) - less_img.astype(np.int16)).max(axis=2) > 0
            drawn = int(mask.sum())
            if drawn < 500:
                continue

            wide = ndimage.binary_dilation(mask, iterations=args.radius)
            _, ma, _ = align(cap_full, ct, np.dstack([wide.astype(np.uint8)] * 3), rt)
            m = ma[..., 0].astype(bool)
            disputed = m & (np.abs(cap.astype(np.int16) - ren.astype(np.int16)).max(axis=2) > 8)
            n = int(disputed.sum())
            if n < 200:
                rows.append({"map": name, "class": cls, "cells": removed, "drawn": drawn,
                             "disputed": n, "best": None, "resolved": None})
                print(f"{name:<12}{cls:<14}{removed:>5} cells {drawn:>8} px  "
                      f"{n:>7} disputed  -- placement exact")
                continue

            results = scan(cap, ren, args.radius, 8, disputed)
            best = min(results, key=results.get)
            rows.append({"map": name, "class": cls, "cells": removed, "drawn": drawn,
                         "disputed": n, "best": list(best),
                         "resolved": round(100 * (1 - results[best]), 1)})
            print(f"{name:<12}{cls:<14}{removed:>5} cells {drawn:>8} px  {n:>7} disputed  "
                  f"best dx={best[0]:+d} dy={best[1]:+d} resolves {100 * (1 - results[best]):.0f}%")

    if args.out:
        with open(args.out, "w") as fh:
            json.dump(rows, fh, indent=2)


if __name__ == "__main__":
    main()
