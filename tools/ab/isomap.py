"""Read a map's IsoMapPack5 so a finding can be tied to a cell's height and tile.

Same LCW container as the overlay packs, but the payload is a list of 11-byte cell records rather
than a grid, and only the cells the map actually defines appear.
"""

from __future__ import annotations

import base64
import re
import struct

from overlays import lcw_decompress

BLOCK = 8192


def cells(map_path: str):
    """Yield {x, y, tile, subtile, level} for every defined cell."""
    text = open(map_path, encoding="latin-1").read()
    m = re.search(r"(?ms)^\[IsoMapPack5\]\r?\n(.*?)(?=^\[|\Z)", text)
    if not m:
        return {}
    b64 = "".join(l.split("=", 1)[1].strip() for l in m.group(1).splitlines() if "=" in l)
    raw = base64.b64decode(b64)

    out = bytearray()
    i = 0
    while i + 4 <= len(raw):
        csize, usize = struct.unpack_from("<HH", raw, i)
        i += 4
        lcw_decompress(raw[i : i + csize], usize, relative=True, out=out)
        i += csize

    result = {}
    for off in range(0, len(out) - 10, 11):
        x, y, tile, subtile, level = struct.unpack_from("<hhIBB", out, off)
        if x or y:
            result[(x, y)] = {"tile": tile, "subtile": subtile, "level": level}
    return result


if __name__ == "__main__":
    import argparse
    import collections

    ap = argparse.ArgumentParser()
    ap.add_argument("map")
    ap.add_argument("--at", nargs=2, type=int, metavar=("X", "Y"), help="print one cell")
    args = ap.parse_args()

    c = cells(args.map)
    if args.at:
        print(tuple(args.at), c.get(tuple(args.at)))
    else:
        print(f"{len(c)} cells")
        print("levels:", dict(sorted(collections.Counter(v["level"] for v in c.values()).items())))
