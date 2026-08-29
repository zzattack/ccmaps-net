"""Run the capture-vs-render comparison across a whole corpus and rank what diverges.

  python abrun.py --captures <dir> --corpus <dir> --mixdir <game dir> --renderer <exe> --out <dir>

Per map it renders, aligns, diffs and writes crops; then it aggregates, so the campaign works from
one ranked list rather than twenty separate reports.
"""

from __future__ import annotations

import argparse
import glob
import json
import os
import subprocess
import sys


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--captures", required=True)
    ap.add_argument("--corpus", required=True)
    ap.add_argument("--mixdir", required=True)
    ap.add_argument("--renderer", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--engine", default="-Y")
    ap.add_argument("--engine-alt", help="engine flag for the maps named in --engine-alt-maps; the "
                                         "TS corpus mixes Tiberian Sun and Firestorm maps, and each "
                                         "was captured against the rules of its own expansion")
    ap.add_argument("--engine-alt-maps", default="", help="comma-separated map names")
    ap.add_argument("--top", type=int, default=8)
    args = ap.parse_args()

    alt = {m.strip() for m in args.engine_alt_maps.split(",") if m.strip()}

    here = os.path.dirname(os.path.abspath(__file__))
    os.makedirs(args.out, exist_ok=True)
    rows = []

    for zip_path in sorted(glob.glob(os.path.join(args.captures, "*.zip"))):
        name = os.path.splitext(os.path.basename(zip_path))[0]
        map_path = os.path.join(args.corpus, name + ".map")
        if not os.path.exists(map_path):
            print(f"{name}: no matching map in the corpus, skipped")
            continue

        out_dir = os.path.join(args.out, name)
        cmd = [
            sys.executable, os.path.join(here, "abcompare.py"),
            "--capture", zip_path,
            "--map", map_path,
            "--mixdir", args.mixdir,
            "--renderer", args.renderer,
            "--out", out_dir,
            # "=" form: the engine flags are themselves "-Y", "-t" and friends, which argparse
            # would otherwise read as options rather than as this option's value.
            f"--engine={args.engine_alt if name in alt and args.engine_alt else args.engine}",
            "--top", str(args.top),
        ]
        proc = subprocess.run(cmd, capture_output=True, text=True)
        if proc.returncode != 0:
            print(f"{name}: FAILED\n{proc.stdout}{proc.stderr}")
            rows.append({"map": name, "error": (proc.stderr or proc.stdout).strip()[-400:]})
            continue

        with open(os.path.join(out_dir, "report.json")) as fh:
            report = json.load(fh)
        stats = report["stats"]
        pct = stats.get("percent_after_exclude", stats["percent"])
        rows.append(
            {
                "map": name,
                "percent": stats["percent"],
                "percent_excl_voxel": pct,
                "differing": stats["differing"],
                "voxel_excluded": stats.get("excluded", 0),
                "clusters": len(report["clusters"]),
                "largest": report["clusters"][0]["area"] if report["clusters"] else 0,
            }
        )
        print(f"{name}: {stats['percent']}% ({len(report['clusters'])} clusters)")

    rows.sort(key=lambda r: r.get("percent_excl_voxel", 0), reverse=True)
    with open(os.path.join(args.out, "summary.json"), "w") as fh:
        json.dump(rows, fh, indent=2)

    print()
    print(f"{'map':<16}{'colour%':>9}{'excl voxel':>12}{'clusters':>10}{'largest':>10}")
    for r in rows:
        if "error" in r:
            print(f"{r['map']:<16}  ERROR {r['error'][:60]}")
            continue
        print(
            f"{r['map']:<16}{r['percent']:>9.3f}{r['percent_excl_voxel']:>12.3f}"
            f"{r['clusters']:>10}{r['largest']:>10}"
        )
    ok = [r for r in rows if "error" not in r]
    if ok:
        avg = sum(r["percent_excl_voxel"] for r in ok) / len(ok)
        print(f"\n{len(ok)} maps, mean colour mismatch {avg:.3f}%")


if __name__ == "__main__":
    main()
