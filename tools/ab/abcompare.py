"""Compare a cnc-buffer-spy engine capture against a ccmaps-net render of the same map.

  python abcompare.py --capture <capture.zip> --map <map file> --mixdir <game dir> --out <dir>

The capture zip carries everything needed to reproduce the render: the tile-variant lattice the
engine actually used, and the cell-to-pixel transform of the stitched image. Alignment is therefore
arithmetic -- both sides report where a cell landed -- rather than a search over offsets.
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import zipfile

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abdiff import (  # noqa: E402
    CellTransform,
    align,
    capture_transform,
    clusters,
    diff_mask,
    load_png,
    render_transform,
    summarize,
)


def read_npy(path: str) -> np.ndarray:
    return np.load(path)


def render(renderer: str, map_path: str, mixdir: str, out_dir: str, lattice, engine_flag: str):
    """Render the same map the capture was taken from, with its randomness pinned.

    `mixdir` may be a ';'-separated list: the CnCNet TS client keeps its mixes in MIX\\ and its inis
    in INI\\, and the renderer needs all three roots on its search path.
    """
    base = os.path.join(out_dir, "render")
    cmd = [
        renderer,
        "-i", map_path,
        "-d", out_dir,
        "-o", "render",
        "-p",
        "-F",                    # never Auto: its 15% heuristic can flip crop mode between builds
        engine_flag,
        "--pin-random",
        "--meta-json", base + ".meta.json",
        "--debug-voxelmask", base + ".voxel.npy",
    ]
    for d in mixdir.split(";"):
        if d.strip():
            cmd += ["-m", d.strip()]
    if lattice:
        cmd += ["--tile-lattice", ",".join(str(v) for v in lattice)]
    proc = subprocess.run(cmd, capture_output=True, text=True)
    if proc.returncode != 0:
        sys.stderr.write(proc.stdout + proc.stderr)
        raise SystemExit(f"renderer failed ({proc.returncode})")
    return base + ".png", base + ".meta.json", base + ".voxel.npy"


def crop_to_saverect(mask: np.ndarray, meta: dict) -> np.ndarray:
    """The z-buffer and voxel-mask dumps cover the whole drawing surface; the PNG is the crop."""
    sr = meta["saveRect"]
    return mask[sr["y"] : sr["y"] + sr["height"], sr["x"] : sr["x"] + sr["width"]]


def palette_quantize(img: np.ndarray) -> np.ndarray:
    """Put our render on the engine's colour precision before comparing.

    The engine's palettes hold 6-bit channels scaled by 4, so its brightest value is 252, and it
    displays through an RGB565 surface. Our renderer scales the same palettes across the full 0-255
    range, which leaves every lit pixel a few units brighter -- on dunepatr the whole-image mean sat
    +4.1/+3.2/+3.7 above the capture. Going back through 6 bits (measured against the alternatives
    in calibrate.py) drops that to +1.7/+1.0/+1.2 and takes 2.3 points of false mismatch with it.

    The floor here is deliberate, and rounding instead is wrong even though it looks more correct:
    on an idealised full-light round trip flooring lands on a different value from the engine for 60
    of the 64 palette entries, but the engine truncates at every step of its own chain
    (`((pal * interp) >> 16) & 0xFC`), so the floor reproduces that cascade. Measured: switching to
    rint costs fourcorners 0.78 -> 1.93%, bridgegap 2.06 -> 3.88%, all05s 3.69 -> 6.60%.
    """
    v = ((img.astype(np.uint16) * 63 // 255) * 4).astype(np.uint8)
    r = (v[..., 0] >> 3) << 3
    g = (v[..., 1] >> 2) << 2
    b = (v[..., 2] >> 3) << 3
    return np.dstack([r, g, b]).astype(np.uint8)


def write_strip(cap, ren, mask, box, path, pad=24, scale=2):
    y0 = max(0, box["y0"] - pad)
    y1 = min(cap.shape[0], box["y1"] + pad)
    x0 = max(0, box["x0"] - pad)
    x1 = min(cap.shape[1], box["x1"] + pad)
    a, b = cap[y0:y1, x0:x1], ren[y0:y1, x0:x1]
    m = (mask[y0:y1, x0:x1] * 255).astype(np.uint8)
    h, w = a.shape[:2]
    strip = np.full((h, w * 3 + 16, 3), 32, np.uint8)
    strip[:, :w] = a
    strip[:, w + 8 : w * 2 + 8] = b
    strip[:, w * 2 + 16 :] = np.dstack([m] * 3)
    img = Image.fromarray(strip)
    if scale != 1:
        img = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
    img.save(path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--capture", required=True, help="capture .zip from CncBufferSpy")
    ap.add_argument("--capture-png", help="stitched PNG (defaults to the zip's name with .png)")
    ap.add_argument("--map", required=True)
    ap.add_argument("--mixdir", required=True, help="the game dir the capture ran against")
    ap.add_argument("--renderer", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--engine", default="-Y", help="renderer engine flag (-Y, -y, -t, -T)")
    ap.add_argument("--tolerance", type=int, default=8)
    ap.add_argument("--top", type=int, default=12, help="how many clusters to write crops for")
    args = ap.parse_args()

    os.makedirs(args.out, exist_ok=True)

    with zipfile.ZipFile(args.capture) as z:
        cap_meta = json.loads(z.read("metadata.json"))
    cap_png = args.capture_png or os.path.splitext(args.capture)[0] + ".png"

    ct = capture_transform(cap_meta)
    if ct is None:
        raise SystemExit("capture has no cell transform; re-capture with --stitch")
    lattice = (cap_meta.get("provenance") or {}).get("variantLattice")

    ren_png, ren_meta_path, ren_voxel_path = render(
        args.renderer, args.map, args.mixdir, args.out, lattice, args.engine
    )
    with open(ren_meta_path) as f:
        ren_meta = json.load(f)
    rt = render_transform(ren_meta)

    cap = load_png(cap_png)
    ren = load_png(ren_png)
    ren_q = palette_quantize(ren)

    cap_a, ren_a, origin = align(cap, ct, ren_q, rt)
    mask = diff_mask(cap_a, ren_a, args.tolerance)

    voxel = None
    if os.path.exists(ren_voxel_path):
        vx = crop_to_saverect(read_npy(ren_voxel_path).astype(bool), ren_meta)
        _, vx_a, _ = align(cap, ct, np.dstack([vx.astype(np.uint8)] * 3), rt)
        voxel = vx_a[..., 0].astype(bool)

    stats = summarize(mask, exclude=voxel)
    ranked = clusters(mask & ~voxel if voxel is not None else mask, min_area=30)

    for i, box in enumerate(ranked[: args.top]):
        write_strip(cap_a, ren_a, mask, box, os.path.join(args.out, f"cluster{i:02d}.png"))

    report = {
        "map": cap_meta.get("map"),
        "capture": os.path.basename(args.capture),
        "logicFrozen": (cap_meta.get("provenance") or {}).get("logicFrozen"),
        "alignedSize": [int(cap_a.shape[1]), int(cap_a.shape[0])],
        "alignOriginInCapture": list(origin),
        "stats": stats,
        "clusters": ranked[:50],
    }
    with open(os.path.join(args.out, "report.json"), "w") as f:
        json.dump(report, f, indent=2)

    print(f"aligned {cap_a.shape[1]}x{cap_a.shape[0]}")
    print(f"colour mismatch {stats['percent']}%", end="")
    if voxel is not None:
        print(f"  (excluding voxel pixels: {stats['percent_after_exclude']}%)")
    else:
        print()
    print(f"{len(ranked)} clusters >= 30px; wrote crops for the top {min(args.top, len(ranked))}")


if __name__ == "__main__":
    main()
