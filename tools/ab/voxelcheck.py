"""Check voxel units for PLACEMENT and SCALE, ignoring shading.

Our voxel shading is deliberately better than the engine's, so colour differences on a unit body are
expected and uninteresting. Where it lands and how big it is are not: those still catch real depth,
offset and projection bugs.

Two measurements per unit, both derived from the renderer's voxel mask:

  offset   the (dx, dy) that best lines our unit up with the engine's, found by sliding the mask over
           the two images. A correct placement scores (0, 0).

  spill    the share of differing pixels that fall OUTSIDE the unit's own silhouette. If scale and
           position agree, the engine and our render disagree only on the body's interior, so spill
           is near zero. A unit drawn too large, too small or offset leaves a halo, and spill climbs.

Usage:
  python voxelcheck.py --capture <zip> --capture-png <png> --render-meta <json>
                       --render-png <png> --voxel-npy <npy> [--out <dir>]
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import zipfile

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abcompare import crop_to_saverect, palette_quantize  # noqa: E402
from abdiff import align, capture_transform, load_png, render_transform  # noqa: E402

SEARCH = 6  # px; wider than any plausible placement error, narrow enough to stay unambiguous


def best_offset(cap, ren, mask, search=SEARCH):
    """Find where our unit's pixels appear in the capture.

    The capture is what moves: for each candidate displacement the capture is sampled at
    mask + (dx, dy) and compared against our unit's own pixels. Holding the mask over the capture
    and sliding the render instead looks equivalent but is not -- with a large true offset it
    happily matches background against background and reports a small, wrong answer.

    Returns the displacement from where WE draw the unit to where the ENGINE draws it.
    """
    if not mask.any():
        return (0, 0), None
    ref = ren[mask].astype(np.int32)
    best, best_score = (0, 0), None
    for dy in range(-search, search + 1):
        for dx in range(-search, search + 1):
            sampled = np.roll(np.roll(cap, -dy, axis=0), -dx, axis=1)[mask].astype(np.int32)
            score = np.abs(sampled - ref).mean()
            if best_score is None or score < best_score:
                best, best_score = (dx, dy), score
    return best, best_score


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--capture", required=True)
    ap.add_argument("--capture-png", required=True)
    ap.add_argument("--render-meta", required=True)
    ap.add_argument("--render-png", required=True)
    ap.add_argument("--voxel-npy", required=True)
    ap.add_argument("--tolerance", type=int, default=8)
    ap.add_argument("--min-area", type=int, default=120)
    # Wider than a plausible placement error, narrow enough to stay unambiguous. Raise it when
    # chasing a gross misplacement: a search that cannot reach the true offset settles on a local
    # optimum and reports a small, believable, wrong number.
    ap.add_argument("--search", type=int, default=SEARCH)
    ap.add_argument("--out")
    args = ap.parse_args()

    archive = zipfile.ZipFile(args.capture)
    cap_meta = json.loads(archive.read("metadata.json"))
    archive.close()
    with open(args.render_meta) as fh:
        ren_meta = json.load(fh)

    cap = load_png(args.capture_png)
    ren = palette_quantize(load_png(args.render_png))
    vox = crop_to_saverect(np.load(args.voxel_npy).astype(bool), ren_meta)

    ct, rt = capture_transform(cap_meta), render_transform(ren_meta)
    cap_a, ren_a, _ = align(cap, ct, ren, rt)
    _, vox_a3, _ = align(cap, ct, np.dstack([vox.astype(np.uint8)] * 3), rt)
    vox_a = vox_a3[..., 0].astype(bool)

    labels, count = ndimage.label(vox_a)
    if count == 0:
        print("no voxel pixels in this render")
        return

    areas = np.bincount(labels.ravel())
    areas[0] = 0
    units = [i for i in range(1, count + 1) if areas[i] >= args.min_area]
    print(f"{len(units)} voxel units of at least {args.min_area}px "
          f"(of {count} components, {int(vox_a.sum())} voxel pixels)\n")

    rows = []
    for lbl in units:
        ys, xs = np.where(labels == lbl)
        y0, y1 = ys.min(), ys.max()
        x0, x1 = xs.min(), xs.max()
        pad = args.search + 6
        sy = slice(max(0, y0 - pad), min(cap_a.shape[0], y1 + pad + 1))
        sx = slice(max(0, x0 - pad), min(cap_a.shape[1], x1 + pad + 1))

        c, r, m = cap_a[sy, sx], ren_a[sy, sx], (labels[sy, sx] == lbl)
        (dx, dy), _ = best_offset(c, r, m, args.search)

        delta = np.abs(c.astype(np.int16) - r.astype(np.int16)).max(axis=2)
        differing = delta > args.tolerance
        halo = ndimage.binary_dilation(m, iterations=1)
        outside = int((differing & ~halo).sum())
        total = int(differing.sum())
        spill = 100.0 * outside / total if total else 0.0

        rows.append({
            "area": int(areas[lbl]),
            "box": [int(x0), int(y0), int(x1), int(y1)],
            "w": int(x1 - x0 + 1),
            "h": int(y1 - y0 + 1),
            "dx": dx, "dy": dy,
            "spill": round(spill, 1),
        })

    rows.sort(key=lambda r: -r["area"])
    print(f"{'area':>7}{'w x h':>10}{'dx':>5}{'dy':>5}{'spill%':>9}")
    for r in rows[:25]:
        print(f"{r['area']:>7}{f'{r[chr(119)]}x{r[chr(104)]}':>10}{r['dx']:>5}{r['dy']:>5}{r['spill']:>9.1f}")

    dxs = np.array([r["dx"] for r in rows])
    dys = np.array([r["dy"] for r in rows])
    sp = np.array([r["spill"] for r in rows])
    print()
    print(f"offset dx: median {np.median(dxs):+.1f}  mean {dxs.mean():+.2f}  "
          f"exact-zero {int((dxs == 0).sum())}/{len(dxs)}")
    print(f"offset dy: median {np.median(dys):+.1f}  mean {dys.mean():+.2f}  "
          f"exact-zero {int((dys == 0).sum())}/{len(dys)}")
    print(f"spill:     median {np.median(sp):.1f}%  mean {sp.mean():.1f}%")

    if args.out:
        os.makedirs(args.out, exist_ok=True)
        with open(os.path.join(args.out, "voxels.json"), "w") as fh:
            json.dump(rows, fh, indent=2)
        # a magnified strip of the worst-placed unit, for eyes
        worst = max(rows, key=lambda r: abs(r["dx"]) + abs(r["dy"]) + r["spill"] / 50)
        x0, y0, x1, y1 = worst["box"]
        pad = 10
        sy = slice(max(0, y0 - pad), min(cap_a.shape[0], y1 + pad + 1))
        sx = slice(max(0, x0 - pad), min(cap_a.shape[1], x1 + pad + 1))
        a, b = cap_a[sy, sx], ren_a[sy, sx]
        h, w = a.shape[:2]
        strip = np.full((h, w * 2 + 4, 3), 40, np.uint8)
        strip[:, :w] = a
        strip[:, w + 4:] = b
        img = Image.fromarray(strip)
        img.resize((img.width * 6, img.height * 6), Image.NEAREST).save(
            os.path.join(args.out, "worst_unit.png"))
        print(f"\nwrote {args.out}\\voxels.json and worst_unit.png "
              f"(dx={worst['dx']} dy={worst['dy']} spill={worst['spill']}%)")


if __name__ == "__main__":
    main()

