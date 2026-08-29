"""List placed objects whose cell projects near a pixel, using the capture's own transform.

Going forward (cell -> pixel) avoids inverting the projection, which needs the cell's height and so
cannot be done reliably from a pixel alone on a map with terrain relief.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import zipfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abdiff import capture_transform  # noqa: E402


def placed(map_path):
    text = open(map_path, encoding="latin-1").read()
    for sec in ("Structures", "Units", "Infantry", "Aircraft", "Terrain", "Smudge"):
        m = re.search(rf"(?ms)^\[{sec}\]\r?\n(.*?)(?=^\[|\Z)", text)
        if not m:
            continue
        for line in m.group(1).splitlines():
            if "=" not in line:
                continue
            key, _, value = line.partition("=")
            parts = value.split(",")
            try:
                if sec in ("Terrain", "Smudge"):
                    cell = int(key)
                    yield sec, parts[0].strip(), cell % 1000, cell // 1000
                else:
                    yield sec, parts[1].strip(), int(parts[3]), int(parts[4])
            except (ValueError, IndexError):
                continue


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--capture", required=True)
    ap.add_argument("--map", required=True)
    ap.add_argument("--x", type=int, required=True)
    ap.add_argument("--y", type=int, required=True)
    ap.add_argument("--radius", type=int, default=90)
    args = ap.parse_args()

    archive = zipfile.ZipFile(args.capture)
    meta = json.loads(archive.read("metadata.json"))
    archive.close()
    t = capture_transform(meta)

    hits = []
    for sec, name, rx, ry in placed(args.map):
        px, py = t.pixel(rx, ry)
        d = abs(px - args.x) + abs(py - args.y)
        if d <= args.radius:
            hits.append((d, sec, name, rx, ry, px, py))

    hits.sort()
    if not hits:
        print(f"nothing placed within {args.radius}px of ({args.x},{args.y})")
    for d, sec, name, rx, ry, px, py in hits[:20]:
        print(f"  d={d:<5} {sec:<11}{name:<12} cell=({rx},{ry}) pixel=({px},{py})")


if __name__ == "__main__":
    main()
