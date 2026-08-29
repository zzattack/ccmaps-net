"""Measure how far our render sits from the engine's, in whole pixels, over a region.

Slides our render under the capture and reports the mismatch at every offset. A displacement bug
shows up as a clear minimum away from (0,0); a sprite-selection bug leaves the minimum at (0,0)
with a high floor. Sign convention: the score at dy compares capture row y against render row
y+dy, so a win at dy=-3 means our content sits 3 pixels too HIGH.
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


def load_pair(ab_root: str, captures: str, map_name: str):
    with zipfile.ZipFile(os.path.join(captures, map_name + ".zip")) as z:
        cap_meta = json.loads(z.read("metadata.json"))
    with open(os.path.join(ab_root, map_name, "render.meta.json")) as fh:
        ren_meta = json.load(fh)
    cap = load_png(os.path.join(captures, map_name + ".png"))
    ren = palette_quantize(load_png(os.path.join(ab_root, map_name, "render.png")))
    ct = capture_transform(cap_meta)
    rt = render_transform(ren_meta)
    ca, ra, _ = align(cap, ct, ren, rt)
    return ca, ra, cap, ct, rt


def scan(cap: np.ndarray, ren: np.ndarray, radius: int, tol: int, mask: np.ndarray | None = None):
    """Mismatch fraction for every whole-pixel offset within `radius`.

    The comparison window is inset by `radius` on all sides so every offset sees the same pixels;
    otherwise offsets that pull in fresh border content score unfairly.
    """
    r = radius
    h, w = cap.shape[:2]
    if h <= 2 * r or w <= 2 * r:
        raise ValueError("region too small for this radius")
    core = cap[r : h - r, r : w - r].astype(np.int16)
    core_mask = mask[r : h - r, r : w - r] if mask is not None else None

    results = {}
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            shifted = ren[r + dy : h - r + dy, r + dx : w - r + dx].astype(np.int16)
            differ = np.abs(core - shifted).max(axis=2) > tol
            if core_mask is not None:
                n = int(core_mask.sum())
                if n == 0:
                    continue
                frac = float(differ[core_mask].sum()) / n
            else:
                frac = float(differ.sum()) / differ.size
            results[(dx, dy)] = frac
    return results


def ore_mask(img: np.ndarray) -> np.ndarray:
    """Pixels whose hue reads as ore or gems rather than ground.

    Ore is a saturated yellow-orange and gems a saturated blue-violet; theatre ground in both
    cases is far less saturated, so a saturation floor separates them without hand-tuned hues.
    """
    f = img.astype(np.int16)
    mx = f.max(axis=2)
    mn = f.min(axis=2)
    return (mx - mn) > 40


def report(results, label, radius):
    best = min(results, key=results.get)
    print(f"\n{label}")
    print(f"  best offset dx={best[0]:+d} dy={best[1]:+d}   mismatch {results[best]*100:.2f}%")
    print(f"  at (0,0)                          mismatch {results[(0,0)]*100:.2f}%")
    lim = min(radius, 4)
    hdr = "      " + "".join(f"{dx:>8d}" for dx in range(-lim, lim + 1))
    print("      dx ->")
    print(hdr)
    for dy in range(-lim, lim + 1):
        row = "".join(f"{results[(dx, dy)]*100:>8.2f}" for dx in range(-lim, lim + 1))
        print(f"dy{dy:+3d}{row}")
    return best


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--ab-root", required=True)
    ap.add_argument("--captures", required=True)
    ap.add_argument("--map", required=True)
    ap.add_argument("--cluster", type=int, help="centre the window on this cluster from report.json")
    ap.add_argument("--region", nargs=4, type=int, metavar=("X", "Y", "W", "H"))
    ap.add_argument("--size", type=int, default=400)
    ap.add_argument("--radius", type=int, default=6)
    ap.add_argument("--tolerance", type=int, default=8)
    ap.add_argument("--saturated-only", action="store_true",
                    help="score only saturated pixels (ore, gems) instead of the whole window")
    ap.add_argument("--mask", help="a .npy class mask from classmask.py, in render coordinates")
    ap.add_argument("--whole-image", action="store_true",
                    help="score the whole aligned image instead of a window")
    ap.add_argument("--disputed-only", action="store_true",
                    help="score only the pixels that already disagree at (0,0), which asks how much "
                         "of the disagreement a pure shift explains")
    ap.add_argument("--dump", help="write engine/ours crops here for inspection")
    args = ap.parse_args()

    cap, ren, cap_full, ct, rt = load_pair(args.ab_root, args.captures, args.map)

    if args.whole_image:
        x0, y0 = 0, 0
        h, w = cap.shape[:2]
    elif args.region:
        x0, y0, w, h = args.region
    else:
        with open(os.path.join(args.ab_root, args.map, "report.json")) as fh:
            box = json.load(fh)["clusters"][args.cluster or 0]
        cx = (box["x0"] + box["x1"]) // 2
        cy = (box["y0"] + box["y1"]) // 2
        w = h = args.size
        x0 = max(0, min(cap.shape[1] - w, cx - w // 2))
        y0 = max(0, min(cap.shape[0] - h, cy - h // 2))

    c = cap[y0 : y0 + h, x0 : x0 + w]
    r = ren[y0 : y0 + h, x0 : x0 + w]
    m = None
    if args.mask:
        from scipy import ndimage

        full = np.load(args.mask)
        # The mask marks where our render drew the class; the engine's copy may sit a few pixels
        # away, which is what the scan measures, so widen it by the search radius.
        full = ndimage.binary_dilation(full, iterations=args.radius)
        _, ma, _ = align(cap_full, ct, np.dstack([full.astype(np.uint8)] * 3), rt)
        m = ma[y0 : y0 + h, x0 : x0 + w, 0].astype(bool)
    if args.saturated_only:
        m = ore_mask(c) if m is None else m & ore_mask(c)
    if args.disputed_only:
        d = np.abs(c.astype(np.int16) - r.astype(np.int16)).max(axis=2) > args.tolerance
        m = d if m is None else m & d

    if args.dump:
        os.makedirs(args.dump, exist_ok=True)
        Image.fromarray(c).save(os.path.join(args.dump, f"{args.map}-engine.png"))
        Image.fromarray(r).save(os.path.join(args.dump, f"{args.map}-ours.png"))

    label = f"{args.map}  window ({x0},{y0}) {w}x{h}"
    if m is not None:
        kind = "class mask" if args.mask else "saturated" if args.saturated_only else "disputed"
        label += f"  {kind} pixels only ({int(m.sum())})"
    results = scan(c, r, args.radius, args.tolerance, m)
    report(results, label, args.radius)


if __name__ == "__main__":
    main()
