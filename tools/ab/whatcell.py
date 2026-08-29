"""Turn a pixel in an aligned comparison back into a map cell, and name what stands there.

Going from "this blob is wrong" to "this building is wrong" otherwise means eyeballing coordinates.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import zipfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abdiff import capture_transform, load_png, render_transform  # noqa: E402


def objects_at(map_path, rx, ry, radius=2):
    text = open(map_path, encoding="latin-1").read()
    found = []
    for sec in ("Structures", "Units", "Infantry", "Aircraft", "Terrain"):
        m = re.search(rf"(?ms)^\[{sec}\]\r?\n(.*?)(?=^\[|\Z)", text)
        if not m:
            continue
        for line in m.group(1).splitlines():
            if "=" not in line:
                continue
            parts = line.split("=", 1)[1].split(",")
            try:
                if sec == "Terrain":
                    cell = int(line.split("=")[0])
                    x, y, name = cell % 1000, cell // 1000, parts[0]
                else:
                    name, x, y = parts[1], int(parts[3]), int(parts[4])
            except (ValueError, IndexError):
                continue
            if abs(x - rx) <= radius and abs(y - ry) <= radius:
                found.append((sec, name.strip(), x, y))
    return found


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--capture", required=True)
    ap.add_argument("--render-meta", required=True)
    ap.add_argument("--map", required=True)
    ap.add_argument("--x", type=int, required=True, help="x in the aligned image")
    ap.add_argument("--y", type=int, required=True)
    ap.add_argument("--radius", type=int, default=2)
    # Cluster boxes are reported in aligned coordinates, which sit at an offset inside the capture.
    # Without this the lookup lands somewhere else and reports "nothing here".
    ap.add_argument("--report", help="report.json from abcompare, for alignOriginInCapture")
    ap.add_argument("--z", type=int, default=0, help="assumed cell height; raise it on hilly maps")
    args = ap.parse_args()

    archive = zipfile.ZipFile(args.capture)
    cap_meta = json.loads(archive.read("metadata.json"))
    archive.close()
    with open(args.render_meta) as fh:
        ren_meta = json.load(fh)

    ct = capture_transform(cap_meta)
    tw, th = ct.tile_w, ct.tile_h

    ox = oy = 0
    if args.report:
        with open(args.report) as fh:
            ox, oy = json.load(fh)["alignOriginInCapture"]
    cx, cy = args.x + ox, args.y + oy

    # invert x = (rx-ry)*tw/2 + offx ; y = (rx+ry-z+1)*th/2 + offy
    u = (cx - ct.off_x) / (tw / 2.0)                    # rx - ry
    v = (cy - ct.off_y) / (th / 2.0) - 1 + args.z       # rx + ry
    rx, ry = (v + u) / 2.0, (v - u) / 2.0
    print(f"aligned ({args.x},{args.y}) -> capture ({cx},{cy}) -> cell approx "
          f"({rx:.1f}, {ry:.1f}) at z={args.z}")

    hits = objects_at(args.map, round(rx), round(ry), args.radius)
    if not hits:
        print("nothing within radius; try a larger --radius (height shifts a cell upward)")
    for sec, name, x, y in hits:
        print(f"  {sec:<11}{name:<12} at ({x},{y})")


if __name__ == "__main__":
    main()
