"""Profile map files so a corpus can be picked on feature coverage rather than on name.

Emits one TSV row per map: theater, size, object counts, tunnel count, and a histogram of the
overlay classes that matter for rendering (tiberium, veins, bridges, walls, rocks, tracks).
"""

from __future__ import annotations

import argparse
import glob
import os
import re
import sys
from collections import Counter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from overlays import cells  # noqa: E402

# TS/FS overlay id ranges, as sequential indices into [OverlayTypes] (see overlays.py).
TS_CLASSES = [
    ("tib", range(102, 122)),
    ("btib", range(27, 39)),
    ("tib2", range(127, 147)),
    ("tib3", range(147, 167)),
    ("veins", (126,)),
    ("veinhole", (167,)),
    ("bridge", (24, 25, 59, 60)),
    ("lobridge", range(74, 102)),
    ("wall", (1, 2, 3, 4, 22, 26)),
    ("track", range(39, 59)),
    ("crate", range(61, 74)),
]


def section(text, name):
    m = re.search(rf"(?ms)^\[{name}\]\r?\n(.*?)(?=^\[|\Z)", text)
    return [l for l in m.group(1).splitlines() if "=" in l] if m else []


def read(path):
    text = open(path, encoding="latin-1").read()
    basic = dict(l.split("=", 1) for l in section(text, "Basic"))
    mapsec = dict(l.split("=", 1) for l in section(text, "Map"))
    size = mapsec.get("Size", ",,0,0").split(",")

    counts = Counter(v for _, _, v, _ in cells(path))
    per_class = {}
    for name, ids in TS_CLASSES:
        n = sum(counts[i] for i in ids)
        if n:
            per_class[name] = n

    return {
        "file": os.path.splitext(os.path.basename(path))[0],
        "title": basic.get("Name", "").strip(),
        "theater": mapsec.get("Theater", "").strip(),
        "w": size[2] if len(size) > 2 else "",
        "h": size[3] if len(size) > 3 else "",
        "structures": len(section(text, "Structures")),
        "units": len(section(text, "Units")),
        "infantry": len(section(text, "Infantry")),
        "terrain": len(section(text, "Terrain")),
        "smudge": len(section(text, "Smudge")),
        "tubes": len(section(text, "Tubes")),
        "houses": "y" if section(text, "Houses") else "",
        "overlays": " ".join(f"{k}:{v}" for k, v in sorted(per_class.items(), key=lambda kv: -kv[1])),
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("paths", nargs="+", help="map files or directories")
    args = ap.parse_args()

    files = []
    for p in args.paths:
        files += sorted(glob.glob(os.path.join(p, "*.map"))) if os.path.isdir(p) else [p]

    cols = ["file", "title", "theater", "w", "h", "structures", "units", "infantry",
            "terrain", "smudge", "tubes", "houses", "overlays"]
    print("\t".join(cols))
    for f in files:
        row = read(f)
        print("\t".join(str(row[c]) for c in cols))


if __name__ == "__main__":
    main()
