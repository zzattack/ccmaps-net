"""Measure the placement of every object of one class, one object at a time.

The whole-image offset scan reports the single shift that best reconciles a class. That hides the
case the eye actually catches: most objects sitting right and a minority sitting somewhere else.
This walks each connected blob of a class mask, template-matches it on its own, and reports the
distribution of per-object offsets.
"""

from __future__ import annotations

import argparse
import collections
import json
import os
import sys
import zipfile

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abcompare import palette_quantize  # noqa: E402
from abdiff import align, capture_transform, load_png, render_transform  # noqa: E402


def best_shift(cap, ren, box, radius, tol):
    """Shift the engine crop over ours and return the offset that agrees best.

    Scoring runs over our sprite's own pixels rather than the whole window, so a tree does not get
    scored mostly on the ground it stands in.
    """
    ys, xs = box
    pad = radius + 4
    y0, y1 = max(0, ys.start - pad), min(cap.shape[0], ys.stop + pad)
    x0, x1 = max(0, xs.start - pad), min(cap.shape[1], xs.stop + pad)
    if y1 - y0 < 2 * pad or x1 - x0 < 2 * pad:
        return None
    r = radius
    core_ren = ren[y0 + r : y1 - r, x0 + r : x1 - r].astype(np.int16)
    best, best_score = None, None
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            sub = cap[y0 + r + dy : y1 - r + dy, x0 + r + dx : x1 - r + dx].astype(np.int16)
            score = int((np.abs(core_ren - sub).max(axis=2) > tol).sum())
            if best_score is None or score < best_score:
                best, best_score = (dx, dy), score
    return best, best_score, core_ren[..., 0].size


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--ab-root", required=True)
    ap.add_argument("--captures", required=True)
    ap.add_argument("--map", required=True)
    ap.add_argument("--mask", required=True, help="class mask .npy in render coordinates")
    ap.add_argument("--radius", type=int, default=20)
    ap.add_argument("--tolerance", type=int, default=8)
    ap.add_argument("--min-area", type=int, default=150)
    ap.add_argument("--max-objects", type=int, default=400)
    args = ap.parse_args()

    from scipy import ndimage

    with zipfile.ZipFile(os.path.join(args.captures, args.map + ".zip")) as z:
        cm = json.loads(z.read("metadata.json"))
    with open(os.path.join(args.ab_root, args.map, "render.meta.json")) as fh:
        rm = json.load(fh)
    ct, rt = capture_transform(cm), render_transform(rm)
    cap_full = load_png(os.path.join(args.captures, args.map + ".png"))
    cap, ren, _ = align(cap_full, ct,
                        palette_quantize(load_png(os.path.join(args.ab_root, args.map, "render.png"))), rt)

    full = np.load(args.mask)
    _, ma, _ = align(cap_full, ct, np.dstack([full.astype(np.uint8)] * 3), rt)
    mask = ma[..., 0].astype(bool)

    lab, n = ndimage.label(mask)
    areas = np.bincount(lab.ravel())
    areas[0] = 0
    boxes = ndimage.find_objects(lab)

    hist = collections.Counter()
    rows = []
    for lbl in np.argsort(areas)[::-1][: args.max_objects]:
        if areas[lbl] < args.min_area:
            break
        got = best_shift(cap, ren, boxes[lbl - 1], args.radius, args.tolerance)
        if not got:
            continue
        (dx, dy), score, size = got
        hist[(dx, dy)] += 1
        rows.append((int(areas[lbl]), dx, dy, score, size, boxes[lbl - 1]))

    print(f"{len(rows)} objects measured (mask blobs >= {args.min_area}px)")
    print("offset (dx,dy)  count   share")
    for (dx, dy), c in hist.most_common(12):
        print(f"   {dx:+3d},{dy:+3d}      {c:>4}   {100*c/len(rows):5.1f}%")
    off = [r for r in rows if (r[1], r[2]) != (0, 0)]
    print(f"\n{len(off)} of {len(rows)} sit somewhere other than (0,0)")
    for area, dx, dy, score, size, box in sorted(off, key=lambda r: -r[0])[:8]:
        ys, xs = box
        print(f"   area {area:>6}  offset {dx:+d},{dy:+d}  at x{xs.start} y{ys.start}")


if __name__ == "__main__":
    main()
