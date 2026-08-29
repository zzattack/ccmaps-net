"""Measure where every terrain object landed, keyed by its cell's slope and height.

Each object in [Terrain] is projected to its pixel through the capture's own cell transform, and
the engine crop is then slid over ours to find the offset that agrees best. The result is a table
of per-object offsets against ramp type and height, which is what separates "trees are misplaced"
from "trees on ramps are misplaced".
"""

from __future__ import annotations

import argparse
import collections
import csv
import json
import os
import re
import sys
import zipfile

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abcompare import palette_quantize  # noqa: E402
from abdiff import align, capture_transform, load_png, render_transform  # noqa: E402


def terrain_objects(map_path):
    text = open(map_path, encoding="latin-1").read()
    m = re.search(r"(?ms)^\[Terrain\]\r?\n(.*?)(?=^\[|\Z)", text)
    if not m:
        return []
    out = []
    for line in m.group(1).splitlines():
        if "=" not in line:
            continue
        key, name = line.split("=", 1)
        key = key.strip()
        if not key.isdigit():
            continue
        pos = int(key)
        out.append((pos % 1000, pos // 1000, name.strip().upper()))
    return out


def best_shift(cap, ren, cx, cy, half, radius, tol):
    r = radius
    y0, y1 = cy - half, cy + half
    x0, x1 = cx - half, cx + half
    if y0 - r < 0 or x0 - r < 0 or y1 + r >= cap.shape[0] or x1 + r >= cap.shape[1]:
        return None
    core = ren[y0:y1, x0:x1].astype(np.int16)
    # only score where our sprite differs from the ground it stands on, so the match is driven by
    # the object rather than by the terrain filling most of the window
    interest = core.std(axis=2) + np.abs(core.astype(np.int32) - core.mean(axis=(0, 1))).sum(axis=2)
    sel = interest > np.percentile(interest, 60)
    if sel.sum() < 80:
        return None
    best, best_score = None, None
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            sub = cap[y0 + dy : y1 + dy, x0 + dx : x1 + dx].astype(np.int16)
            score = int((np.abs(core - sub).max(axis=2) > tol)[sel].sum())
            if best_score is None or score < best_score:
                best, best_score = (dx, dy), score
    return best, best_score, int(sel.sum())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--ab-root", required=True)
    ap.add_argument("--captures", required=True)
    ap.add_argument("--map", required=True)
    ap.add_argument("--map-file", required=True)
    ap.add_argument("--tiles", required=True, help="CSV from the renderer's --debug-tiles")
    ap.add_argument("--radius", type=int, default=20)
    ap.add_argument("--half", type=int, default=26, help="half-size of the match window")
    ap.add_argument("--tolerance", type=int, default=8)
    ap.add_argument("--out")
    args = ap.parse_args()

    with zipfile.ZipFile(os.path.join(args.captures, args.map + ".zip")) as z:
        cm = json.loads(z.read("metadata.json"))
    with open(os.path.join(args.ab_root, args.map, "render.meta.json")) as fh:
        rm = json.load(fh)
    ct, rt = capture_transform(cm), render_transform(rm)
    cap_full = load_png(os.path.join(args.captures, args.map + ".png"))
    ren_full = palette_quantize(load_png(os.path.join(args.ab_root, args.map, "render.png")))
    cap, ren, origin = align(cap_full, ct, ren_full, rt)

    tiles = {}
    with open(args.tiles) as fh:
        for row in csv.DictReader(fh):
            tiles[(int(row["rx"]), int(row["ry"]))] = (int(row["z"]), int(row["ramp"]))

    rows = []
    for rx, ry, name in terrain_objects(args.map_file):
        z, ramp = tiles.get((rx, ry), (0, 0))
        px, py = ct.pixel(rx, ry, z)
        got = best_shift(cap, ren, px - origin[0], py - origin[1], args.half, args.radius, args.tolerance)
        if not got:
            continue
        (dx, dy), score, n = got
        rows.append({"rx": rx, "ry": ry, "type": name, "z": z, "ramp": ramp,
                     "dx": dx, "dy": dy, "residual": round(100 * score / n, 1)})

    print(f"{len(rows)} terrain objects measured\n")
    print("offset (dx,dy)   count   ramp types seen")
    hist = collections.Counter((r["dx"], r["dy"]) for r in rows)
    for (dx, dy), c in hist.most_common(10):
        ramps = collections.Counter(r["ramp"] for r in rows if (r["dx"], r["dy"]) == (dx, dy))
        print(f"   {dx:+3d},{dy:+3d}      {c:>4}   {dict(sorted(ramps.items()))}")

    print("\nby ramp type:")
    for ramp in sorted({r["ramp"] for r in rows}):
        sub = [r for r in rows if r["ramp"] == ramp]
        offs = collections.Counter((r["dx"], r["dy"]) for r in sub)
        print(f"   ramp {ramp:>2}: {len(sub):>4} objects   {dict(offs.most_common(4))}")

    if args.out:
        with open(args.out, "w", newline="") as fh:
            w = csv.DictWriter(fh, fieldnames=list(rows[0]))
            w.writeheader()
            w.writerows(rows)
        print(f"\nwrote {args.out}")


if __name__ == "__main__":
    main()
