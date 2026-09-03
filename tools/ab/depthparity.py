"""Depth-buffer parity map: our z-buffer against the engine's DepthBuffer layer.

Where the two z models agree, ours + engine is a constant, at any height, so the image of
that sum minus its mode shows every per-object offset directly (a building one z too near
reads +1 over its whole sprite). Inputs: a capture taken WITHOUT --nozip (the zip holds
DepthBuffer_f<N>.npy, SurfacePrimary_f<N>.npy and SurfaceComposite_f<N>.npy of the frame the
logic was frozen on) and a render of the same map with --debug-zbuffer plus --meta-json.

The engine's depth buffer is a torus: horizontal scrolling rotates the columns and vertical
scrolling the rows, each wrapping on its own, and the buffer holds one screen only. The view
is located inside the stitched composite by matching the primary surface, the rotation by a
circular cross-correlation of row-detrended buffers (the flat-ring reading of an earlier
session is wrong: it puts a one-row seam through the picture where the column wrap falls).

usage: depthparity.py <capture.zip> <render.json> <zbuffer.npy> <out-prefix> [structures.map]
"""

from __future__ import annotations

import json
import os
import re
import sys
import zipfile

import numpy as np
from numpy.fft import fft, fft2, ifft, ifft2
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abdiff import capture_transform, render_transform  # noqa: E402


def rgb565_gray(a: np.ndarray) -> np.ndarray:
    a = a.astype(np.uint32)
    return ((a >> 11) & 31) * 2.0 + ((a >> 5) & 63) * 1.0 + (a & 31) * 0.5


def block_mean(a: np.ndarray, f: int) -> np.ndarray:
    h, w = a.shape[0] // f * f, a.shape[1] // f * f
    return a[:h, :w].reshape(h // f, f, w // f, f).mean(axis=(1, 3))


def locate_view(comp: np.ndarray, prim: np.ndarray, H: int, W: int):
    """Where the frozen frame's screen sits in the composite, and the tab-bar rows above it."""
    cg = rgb565_gray(comp)
    best = None
    for top in (0, prim.shape[0] - H):
        pg = rgb565_gray(prim[top:top + H, :W])
        A = block_mean(cg, 4); B = block_mean(pg, 4)
        A = A - A.mean(); B = B - B.mean()
        Bp = np.zeros(A.shape); Bp[:B.shape[0], :B.shape[1]] = B
        c = np.real(ifft2(fft2(A) * np.conj(fft2(Bp))))
        iy, ix = np.unravel_index(np.argmax(c), c.shape)
        ys, xs = slice(0, H, 7), slice(0, W, 7)
        for dy in range(iy * 4 - 4, iy * 4 + 5):
            for dx in range(ix * 4 - 4, ix * 4 + 5):
                if dy < 0 or dx < 0 or dy + H > cg.shape[0] or dx + W > cg.shape[1]:
                    continue
                s = np.abs(cg[dy:dy + H, dx:dx + W][ys, xs] - pg[ys, xs]).mean()
                if best is None or s < best[0]:
                    best = (s, dx, dy, top)
    return best[1], best[2], best[3]


def unwrap(eng: np.ndarray, ours: np.ndarray, valid: np.ndarray):
    """Rotate the engine torus onto our crop; returns (unwrapped, row shift, column shift)."""
    H, W = eng.shape
    a = np.where(valid, ours, 0).astype(np.float64)
    a -= np.median(a, axis=1, keepdims=True); a[~valid] = 0
    b = -eng.astype(np.float64); b -= np.median(b, axis=1, keepdims=True)
    c = np.real(ifft(np.conj(fft(a.ravel())) * fft(b.ravel())))
    k = int(np.argmax(c)); kr, kc = k // W, k % W
    best = None
    for dr in (kr, kr + 1):
        engu = np.roll(np.roll(eng, -dr, axis=0), -kc, axis=1)
        D = ours + engu
        vals, cnt = np.unique(D[valid], return_counts=True)
        if best is None or cnt.max() > best[0]:
            best = (cnt.max(), engu, dr, kc)
    return best[1], best[2], best[3]


def main():
    cap_zip, render_json, z_path, out = sys.argv[1:5]
    map_path = sys.argv[5] if len(sys.argv) > 5 else None
    with zipfile.ZipFile(cap_zip) as z:
        meta = json.loads(z.read("metadata.json"))
        layers = {l["name"]: l["file"] for l in meta["layers"]}
        eng = np.load(z.open(layers["DepthBuffer"])).astype(np.int32)
        prim = np.load(z.open(layers["SurfacePrimary"]))
        comp = np.load(z.open(layers["SurfaceComposite"]))
    H, W = eng.shape
    cx0, cy0, top = locate_view(comp, prim, H, W)
    rm = json.load(open(render_json)); sr = rm["saveRect"]
    ta, tb = capture_transform(meta), render_transform(rm)
    sx = cx0 - (ta.off_x - tb.off_x) + sr["x"]
    sy = cy0 - (ta.off_y - tb.off_y) + sr["y"]
    print(f"view at composite ({cx0},{cy0}), tab bar {top} rows, surface origin ({sx},{sy})")

    full = np.load(z_path).astype(np.int32)
    ours = np.full((H, W), -32768, np.int32)
    y0, x0 = max(0, sy), max(0, sx); y1, x1 = min(full.shape[0], sy + H), min(full.shape[1], sx + W)
    ours[y0 - sy:y1 - sy, x0 - sx:x1 - sx] = full[y0:y1, x0:x1]
    valid = ours != -32768
    engu, dr, kc = unwrap(eng, ours, valid)
    D = ours + engu
    vals, cnt = np.unique(D[valid], return_counts=True); mode = int(vals[np.argmax(cnt)])
    print(f"torus rotation rows {dr} cols {kc}; mode {mode}, {cnt.max() / valid.sum():.4f} of pixels exact")
    Dm = np.where(valid, D - mode, 9999)
    o = np.argsort(-cnt)
    print("histogram:", [(int(vals[i]) - mode, int(cnt[i])) for i in o[:10]])
    np.save(out + "_Dm.npy", Dm)

    im = np.zeros(Dm.shape + (3,), np.uint8)
    im[Dm == 0] = (128, 128, 128); im[Dm == -1] = (255, 80, 80); im[Dm == 1] = (80, 80, 255)
    im[Dm <= -2] = (140, 0, 0); im[Dm >= 2] = (0, 0, 140); im[np.abs(Dm) > 40] = (0, 0, 0)
    im[~valid] = (0, 60, 0)
    Image.fromarray(im).save(out + "_Dm.png")

    if map_path:
        text = open(map_path, encoding="latin-1").read()
        m = re.search(r"(?ms)^\[Structures\]\r?\n(.*?)(?=^\[|\Z)", text)
        for line in m.group(1).splitlines() if m else []:
            if "=" not in line:
                continue
            p = line.split("=", 1)[1].split(",")
            px, py = tb.pixel(int(p[3]), int(p[4]))
            c, r = px + sr["x"] - sx, py + sr["y"] - sy
            if not (60 <= c < W - 60 and 100 <= r < H - 20):
                continue
            win = Dm[r - 100:r + 20, c - 60:c + 60]
            v = win[np.abs(win) <= 40]
            vals, cnt = np.unique(v, return_counts=True); oo = np.argsort(-cnt)
            print("BLDG", p[1], p[3], p[4], [(int(vals[i]), int(cnt[i])) for i in oo[:4]])


if __name__ == "__main__":
    main()
