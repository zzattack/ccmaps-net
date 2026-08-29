"""Find the colour transform that best maps our 8-bit render onto the engine's output.

The engine displays through 6-bit palettes scaled by 4 and an RGB565 surface. Guessing which of
those steps our renderer already applies is guesswork; measuring which transform minimises the
mismatch is not. Whatever wins here becomes the harness's quantiser, so that the remaining
difference is structure rather than tone.
"""

from __future__ import annotations

import json
import os
import sys
import zipfile

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abdiff import align, capture_transform, diff_mask, load_png, render_transform  # noqa: E402


def rgb565(v: np.ndarray) -> np.ndarray:
    r = (v[..., 0] >> 3) << 3
    g = (v[..., 1] >> 2) << 2
    b = (v[..., 2] >> 3) << 3
    return np.dstack([r, g, b]).astype(np.uint8)


def candidates():
    yield "none", lambda a: a
    yield "565 only", lambda a: rgb565(a)
    yield "pal6x4 floor + 565", lambda a: rgb565((a >> 2) << 2)
    yield "scale 252/255 + 565", lambda a: rgb565((a.astype(np.uint16) * 252 // 255).astype(np.uint8))
    yield "pal6 round + 565", lambda a: rgb565(np.minimum(255, ((a.astype(np.uint16) + 2) >> 2) << 2).astype(np.uint8))
    yield "6bit*4 exact + 565", lambda a: rgb565(((a.astype(np.uint16) * 63 // 255) * 4).astype(np.uint8))


def main():
    cap_zip, cap_png, ren_meta, ren_png = sys.argv[1:5]
    archive = zipfile.ZipFile(cap_zip)
    cm = json.loads(archive.read("metadata.json"))
    archive.close()
    with open(ren_meta) as fh:
        rm = json.load(fh)

    cap = load_png(cap_png)
    ren = load_png(ren_png)
    ct, rt = capture_transform(cm), render_transform(rm)

    print(f"{'transform':<24}{'tol 4':>9}{'tol 8':>9}{'tol 16':>9}{'mean delta':>26}")
    for name, fn in candidates():
        ca, ra, _ = align(cap, ct, fn(ren), rt)
        cols = []
        for tol in (4, 8, 16):
            m = diff_mask(ca, ra, tol)
            cols.append(100.0 * m.sum() / m.size)
        delta = ra.reshape(-1, 3).mean(axis=0) - ca.reshape(-1, 3).mean(axis=0)
        print(f"{name:<24}{cols[0]:>8.2f}%{cols[1]:>8.2f}%{cols[2]:>8.2f}%"
              f"{str(delta.round(2)):>26}")


if __name__ == "__main__":
    main()
