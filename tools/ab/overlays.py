"""Read a map's OverlayPack / OverlayDataPack so a comparison can be restricted to one overlay class.

Both packs are a 512x512 byte grid indexed [y*512+x] in map (not display) coordinates, stored as
base64 over a chain of LCW blocks. 0xFF in the overlay pack means "no overlay".
"""

from __future__ import annotations

import base64
import re
import struct

GRID = 512
NONE = 0xFF


def lcw_decompress(src: bytes, out_size: int, relative: bool = False,
                   out: bytearray | None = None) -> bytearray:
    """Westwood Format80. The relative-copy commands may read output the same command is writing,
    so the byte-at-a-time loops below are load-bearing, not naive.

    Two variants exist and they are not distinguishable from the data: the long copy commands take
    an offset from the start of the block, or one counted back from the write head. The overlay
    packs use the first, IsoMapPack5 the second.
    """
    out = bytearray() if out is None else out
    limit = len(out) + out_size
    i = 0
    n = len(src)
    while i < n and len(out) < limit:
        cmd = src[i]
        i += 1
        if cmd == 0x80:
            break
        if cmd & 0x80 == 0:
            # 0cccpppp p: copy count+3 from a relative back-reference
            count = (cmd >> 4) + 3
            pos = ((cmd & 0x0F) << 8) | src[i]
            i += 1
            start = len(out) - pos
            for k in range(count):
                out.append(out[start + k])
        elif cmd & 0x40 == 0:
            # 10cccccc: copy count bytes straight from the source
            count = cmd & 0x3F
            out += src[i : i + count]
            i += count
        elif cmd == 0xFE:
            # run of one colour
            count = struct.unpack_from("<H", src, i)[0]
            i += 2
            out += bytes([src[i]]) * count
            i += 1
        elif cmd == 0xFF:
            # long copy
            count = struct.unpack_from("<H", src, i)[0]
            i += 2
            pos = struct.unpack_from("<H", src, i)[0]
            i += 2
            start = len(out) - pos if relative else pos
            for k in range(count):
                out.append(out[start + k])
        else:
            # 11cccccc + offset
            count = (cmd & 0x3F) + 3
            pos = struct.unpack_from("<H", src, i)[0]
            i += 2
            start = len(out) - pos if relative else pos
            for k in range(count):
                out.append(out[start + k])
    return out


def read_pack(text: str, section: str) -> bytearray:
    m = re.search(rf"(?ms)^\[{section}\]\r?\n(.*?)(?=^\[|\Z)", text)
    if not m:
        return bytearray(GRID * GRID)
    b64 = "".join(line.split("=", 1)[1].strip() for line in m.group(1).splitlines() if "=" in line)
    raw = base64.b64decode(b64)

    out = bytearray()
    i = 0
    while i + 4 <= len(raw) and len(out) < GRID * GRID:
        csize, usize = struct.unpack_from("<HH", raw, i)
        i += 4
        out += lcw_decompress(raw[i : i + csize], usize)
        i += csize
    out += bytearray(GRID * GRID - len(out))
    return out


def load(map_path: str):
    text = open(map_path, encoding="latin-1").read()
    return read_pack(text, "OverlayPack"), read_pack(text, "OverlayDataPack")


def cells(map_path: str, ids=None):
    """Yield (rx, ry, overlay_id, overlay_data) for every cell carrying an overlay."""
    ovl, dat = load(map_path)
    for y in range(GRID):
        row = y * GRID
        for x in range(GRID):
            v = ovl[row + x]
            if v == NONE:
                continue
            if ids is not None and v not in ids:
                continue
            yield x, y, v, dat[row + x]


if __name__ == "__main__":
    import argparse
    import collections

    ap = argparse.ArgumentParser()
    ap.add_argument("map")
    ap.add_argument("--names", help="rulesmd.ini, to name the overlay ids")
    args = ap.parse_args()

    names = {}
    if args.names:
        txt = open(args.names, encoding="latin-1").read()
        sec = re.search(r"(?ms)^\[OverlayTypes\]\r?\n(.*?)(?=^\[|\Z)", txt)
        for line in sec.group(1).splitlines():
            if "=" in line:
                k, v = line.split("=", 1)
                if k.strip().isdigit():
                    names[int(k)] = v.split(";")[0].strip()

    counts = collections.Counter(v for _, _, v, _ in cells(args.map))
    for oid, n in sorted(counts.items(), key=lambda kv: -kv[1]):
        print(f"{oid:>4}  {names.get(oid, ''):<16}{n:>7}")
