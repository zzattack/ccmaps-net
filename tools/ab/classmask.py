"""Isolate the pixels one overlay class draws, by rendering the map with that class removed.

Deriving the mask geometrically would need each cell's height out of IsoMapPack5 and would still
guess at sprite extents. Rendering twice and taking the difference gives the exact set of pixels
the class is responsible for, including wherever it overlaps its neighbours.
"""

from __future__ import annotations

import argparse
import base64
import os
import re
import struct
import subprocess
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abdiff import load_png  # noqa: E402
from overlays import GRID, NONE, read_pack  # noqa: E402

BLOCK = 8192


def lcw_store(data: bytes) -> bytes:
    """Emit LCW that only uses literal runs. Valid input for any Format80 reader, and the packs
    are rewritten once for a diagnostic, so size does not matter."""
    out = bytearray()
    i = 0
    while i < len(data):
        n = min(63, len(data) - i)
        out.append(0x80 | n)
        out += data[i : i + n]
        i += n
    out.append(0x80)
    return bytes(out)


def write_pack(text: str, section: str, data: bytes) -> str:
    raw = bytearray()
    for start in range(0, len(data), BLOCK):
        chunk = bytes(data[start : start + BLOCK])
        comp = lcw_store(chunk)
        raw += struct.pack("<HH", len(comp), len(chunk)) + comp
    b64 = base64.b64encode(bytes(raw)).decode("ascii")
    lines = [f"{i + 1}={b64[p:p + 70]}" for i, p in enumerate(range(0, len(b64), 70))]
    body = "\n".join(lines) + "\n"
    return re.sub(rf"(?ms)^(\[{section}\]\r?\n)(.*?)(?=^\[|\Z)", lambda m: m.group(1) + body, text)


def strip_terrain(map_path: str, prefixes: list[str], out_path: str):
    """Remove [Terrain] entries whose type name starts with any of `prefixes`.

    Terrain objects are listed one per line as <cell>=<TYPE>, so isolating a class here is a text
    edit rather than a pack rewrite.
    """
    text = open(map_path, encoding="latin-1").read()
    m = re.search(r"(?ms)^\[Terrain\]\r?\n(.*?)(?=^\[|\Z)", text)
    if not m:
        return 0, text
    kept, removed = [], 0
    for line in m.group(1).splitlines():
        name = line.split("=", 1)[1].strip().upper() if "=" in line else ""
        if name and any(name.startswith(p) for p in prefixes):
            removed += 1
        else:
            kept.append(line)
    body = "\n".join(kept) + ("\n" if kept else "")
    text = text[: m.start(1)] + body + text[m.end(1):]
    with open(out_path, "w", encoding="latin-1", newline="") as fh:
        fh.write(text)
    return removed, text


def strip_objects(map_path: str, out_path: str):
    """Empty every object section and the overlay packs, leaving bare terrain.

    Diffing this against the full render marks every pixel an object touched, shadows included, so
    the complement is ground drawn through the tile drawer alone. A colour transfer measured over a
    mix of drawers reads as a curve that no single multiplier fits.
    """
    text = open(map_path, encoding="latin-1").read()
    removed = 0
    for section in ("Terrain", "Structures", "Units", "Infantry", "Aircraft", "Smudge"):
        m = re.search(r"(?ms)^\[" + section + r"\]\r?\n(.*?)(?=^\[|\Z)", text)
        if not m:
            continue
        removed += sum(1 for l in m.group(1).splitlines() if "=" in l)
        text = text[: m.start(1)] + "\n" + text[m.end(1):]
    ovl = read_pack(text, "OverlayPack")
    dat = read_pack(text, "OverlayDataPack")
    for i in range(GRID * GRID):
        if ovl[i] != NONE:
            ovl[i] = NONE
            dat[i] = 0
            removed += 1
    text = write_pack(text, "OverlayPack", bytes(ovl))
    text = write_pack(text, "OverlayDataPack", bytes(dat))
    with open(out_path, "w", encoding="latin-1", newline="") as fh:
        fh.write(text)
    return removed


def strip_map(map_path: str, ids: set[int], out_path: str, pool: int | None = None):
    text = open(map_path, encoding="latin-1").read()
    ovl = read_pack(text, "OverlayPack")
    dat = read_pack(text, "OverlayDataPack")

    removed = 0
    for i in range(GRID * GRID):
        if ovl[i] not in ids:
            continue
        # with a pool index, keep only the tiberium cells the engine draws from that pool slot,
        # so each of the 12 flat images can be measured on its own
        if pool is not None and (i % GRID) * (i // GRID) % 12 != pool:
            continue
        ovl[i] = NONE
        dat[i] = 0
        removed += 1

    text = write_pack(text, "OverlayPack", bytes(ovl))
    text = write_pack(text, "OverlayDataPack", bytes(dat))
    with open(out_path, "w", encoding="latin-1", newline="") as fh:
        fh.write(text)
    return removed


def render(renderer, map_path, mixdir, out_dir, name, lattice, engine_flag):
    """`mixdir` may be a ';'-separated list, for game dirs that split mixes from inis."""
    base = os.path.join(out_dir, name)
    cmd = [
        renderer, "-i", map_path, "-d", out_dir, "-o", name, "-p", "-F", engine_flag,
        "--pin-random", "--meta-json", base + ".meta.json",
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
    return base + ".png"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--map", required=True)
    ap.add_argument("--mixdir", required=True)
    ap.add_argument("--renderer", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--ids", help="overlay ids to isolate, e.g. 102 or 168-177")
    ap.add_argument("--strip-objects", action="store_true",
                    help="isolate bare terrain: remove every object section and overlay")
    ap.add_argument("--terrain", help="comma-separated [Terrain] type name prefixes to isolate "
                                      "instead of overlay ids, e.g. TIBTRE or FONA")
    ap.add_argument("--engine", default="-Y")
    ap.add_argument("--lattice", help="comma-separated lattice from the capture")
    ap.add_argument("--name", default="class")
    ap.add_argument("--pool", type=int, help="restrict to tiberium cells with this (x*y) %% 12 slot")
    args = ap.parse_args()

    ids = set()
    if args.ids:
        for part in args.ids.split(","):
            if "-" in part:
                lo, hi = part.split("-")
                ids.update(range(int(lo), int(hi) + 1))
            else:
                ids.add(int(part))
    elif not args.terrain and not args.strip_objects:
        raise SystemExit("pass --ids, --terrain or --strip-objects")

    os.makedirs(args.out, exist_ok=True)
    lattice = [int(v) for v in args.lattice.split(",")] if args.lattice else None

    stripped_map = os.path.join(args.out, args.name + ".map")
    if args.strip_objects:
        removed = strip_objects(args.map, stripped_map)
        print(f"removed {removed} objects and overlay cells")
    elif args.terrain:
        prefixes = [p.strip().upper() for p in args.terrain.split(",") if p.strip()]
        removed, _ = strip_terrain(args.map, prefixes, stripped_map)
        print(f"removed {removed} terrain objects matching {','.join(prefixes)}")
    else:
        removed = strip_map(args.map, ids, stripped_map, args.pool)
        print(f"removed {removed} cells carrying {sorted(ids)[0]}..{sorted(ids)[-1]}")

    full = render(args.renderer, args.map, args.mixdir, args.out, "full", lattice, args.engine)
    less = render(args.renderer, stripped_map, args.mixdir, args.out, args.name, lattice, args.engine)

    a = load_png(full)
    b = load_png(less)
    if a.shape != b.shape:
        raise SystemExit(f"renders differ in size: {a.shape} vs {b.shape}")
    mask = np.abs(a.astype(np.int16) - b.astype(np.int16)).max(axis=2) > 0
    np.save(os.path.join(args.out, args.name + ".mask.npy"), mask)
    print(f"{args.name}: {int(mask.sum())} pixels drawn by this class")


if __name__ == "__main__":
    main()
