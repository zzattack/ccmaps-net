"""Classify every corpus pixel as fixed or broken between two renderer builds.

  python abregress.py --after <corpus> --before <corpus> --out <root>

A corpus-mean delta says a change cost 0.001%; it does not say whether that is a sprite in the
wrong layer or a one-pixel fringe that moved. Both builds are compared against the same gamemd
capture, and every pixel falls into one of:

  fixed    wrong before, right now
  broke    right before, wrong now
  (rest)   unchanged verdict

`broke` is clustered into zones and written as a normal zones.json, so ComparisonViewer pages
through the regressions themselves. Only maps that actually regressed get a file.

`shifted` splits the noise question: a broken pixel that touches an already-wrong pixel is the
same error displaced, while one with no wrong neighbour is a pixel the change newly got wrong.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
from concurrent.futures import ThreadPoolExecutor, as_completed

import numpy as np
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abcompare import palette_quantize  # noqa: E402
from abdiff import (  # noqa: E402
    align,
    capture_transform,
    channel_delta,
    clusters,
    load_png,
    render_transform,
)

MAX_ZONES = 200


def meta(root: str, name: str) -> str:
    return os.path.join(root, "meta", name)


def load_manifest(root: str) -> dict:
    with open(meta(root, "manifest.json")) as fh:
        return json.load(fh)


def side(root: str, entry: dict, cap: np.ndarray, ct):
    """Aligned crops plus the box they occupy in capture space and the render's own origin."""
    with open(meta(root, entry["render"])) as fh:
        rt = render_transform(json.load(fh))
    ren = palette_quantize(load_png(os.path.join(root, entry["ccmaps"])))
    cap_a, ren_a, (ax0, ay0) = align(cap, ct, ren, rt)
    dx = ct.off_x - rt.off_x
    dy = ct.off_y - rt.off_y
    h, w = cap_a.shape[:2]
    return cap_a, ren_a, (ax0, ay0, w, h), (max(0, -dx), max(0, -dy))


def intersect(box_a, box_b):
    ax, ay, aw, ah = box_a
    bx, by, bw, bh = box_b
    x0, y0 = max(ax, bx), max(ay, by)
    x1, y1 = min(ax + aw, bx + bw), min(ay + ah, by + bh)
    if x1 <= x0 or y1 <= y0:
        raise ValueError("the two builds' crops do not overlap")
    return x0, y0, x1 - x0, y1 - y0


def cut(img, box, common):
    x0, y0, w, h = common
    ox, oy = box[0], box[1]
    return img[y0 - oy: y0 - oy + h, x0 - ox: x0 - ox + w]


def one(out_root, after_root, before_root, ea, eb, tol, min_area):
    with open(meta(after_root, ea["capture"])) as fh:
        ct = capture_transform(json.load(fh))
    if ct is None:
        raise ValueError("capture has no cell transform")
    cap = load_png(os.path.join(after_root, ea["gamemd"]))

    ca, ra, box_a, ro_a = side(after_root, ea, cap, ct)
    cb, rb, box_b, _ = side(before_root, eb, cap, ct)

    common = intersect(box_a, box_b)
    ca, ra = cut(ca, box_a, common), cut(ra, box_a, common)
    cb, rb = cut(cb, box_b, common), cut(rb, box_b, common)

    delta_a = channel_delta(ca, ra)
    bad_a = delta_a > tol
    bad_b = channel_delta(cb, rb) > tol

    broke = bad_a & ~bad_b
    fixed = bad_b & ~bad_a
    shifted = broke & ndimage.binary_dilation(bad_b)

    total = bad_a.size
    x0, y0, w, h = common
    broke_px = int(broke.sum())
    shifted_px = int(shifted.sum())
    # The viewer crops the capture at captureOrigin and the render at renderOrigin; both move with
    # the common region when the two builds disagree about the crop rectangle.
    return {
        "index": ea["index"],
        "name": ea["name"],
        "stem": ea["stem"],
        "gamemd": os.path.relpath(os.path.join(after_root, ea["gamemd"]), out_root),
        "ccmaps": os.path.relpath(os.path.join(after_root, ea["ccmaps"]), out_root),
        "alignedSize": [w, h],
        "captureOrigin": [x0, y0],
        "renderOrigin": [ro_a[0] + x0 - box_a[0], ro_a[1] + y0 - box_a[1]],
        "cropChanged": box_a != box_b,
        "stats": {
            "pixels": total,
            "differing": broke_px,
            "percent": round(100.0 * broke_px / total, 4),
        },
        "beforePercent": round(100.0 * int(bad_b.sum()) / total, 4),
        "afterPercent": round(100.0 * int(bad_a.sum()) / total, 4),
        "brokePixels": broke_px,
        "fixedPixels": int(fixed.sum()),
        "shiftedPixels": shifted_px,
        "newPixels": broke_px - shifted_px,
        "zones": clusters(broke, min_area, delta_a)[:MAX_ZONES],
    }


def write_report(out, summary):
    worse = [r for r in summary if r["delta"] > 0]
    better = [r for r in summary if r["delta"] < 0]
    same = [r for r in summary if r["delta"] == 0]
    broke = sum(r["brokePixels"] for r in summary)
    fixed = sum(r["fixedPixels"] for r in summary)
    shift = sum(r["shiftedPixels"] for r in summary)
    lines = [
        f"{len(summary)} maps",
        f"mean before {sum(r['beforePercent'] for r in summary) / len(summary):.4f}%  "
        f"after {sum(r['afterPercent'] for r in summary) / len(summary):.4f}%",
        f"{len(worse)} worse, {len(better)} better, {len(same)} unchanged",
        f"broke {broke} px, fixed {fixed} px, net {fixed - broke:+d}",
        f"of the broken px: {shift} shifted ({100.0 * shift / max(1, broke):.1f}%), {broke - shift} new",
        "",
        f"{'idx':>5} {'before%':>9} {'after%':>9} {'delta':>9} {'broke':>9} {'fixed':>9} {'new':>9}  name",
    ]
    for r in sorted(summary, key=lambda r: r["delta"], reverse=True):
        lines.append(f"{r['index']:>5} {r['beforePercent']:>9.4f} {r['afterPercent']:>9.4f} "
                     f"{r['delta']:>+9.4f} {r['brokePixels']:>9} {r['fixedPixels']:>9} "
                     f"{r['newPixels']:>9}  {r['name']}")
    with open(os.path.join(out, "meta", "regression-report.txt"), "w") as fh:
        fh.write("\n".join(lines) + "\n")
    print("\n" + "\n".join(lines[:5]))


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--after", required=True)
    ap.add_argument("--before", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--tolerance", type=int, default=8)
    ap.add_argument("--min-area", type=int, default=30)
    ap.add_argument("--jobs", type=int, default=6)
    ap.add_argument("--limit", type=int, help="only the first N maps, for smoke tests")
    args = ap.parse_args()

    out = os.path.abspath(args.out)
    os.makedirs(os.path.join(out, "meta"), exist_ok=True)

    ma, mb = load_manifest(args.after), load_manifest(args.before)
    work = []
    for key, ea in sorted(ma.items()):
        eb = mb.get(key)
        if not eb or not all(ea.get(k) for k in ("gamemd", "ccmaps", "capture", "render")):
            continue
        if not all(eb.get(k) for k in ("ccmaps", "render")):
            continue
        work.append((key, ea, eb))
        if args.limit and len(work) >= args.limit:
            break

    rows = []
    print(f"classifying {len(work)} maps on {args.jobs} threads", flush=True)
    with ThreadPoolExecutor(max_workers=args.jobs) as pool:
        futures = {pool.submit(one, out, args.after, args.before, ea, eb, args.tolerance,
                               args.min_area): key for key, ea, eb in work}
        done = 0
        for fut in as_completed(futures):
            key = futures[fut]
            done += 1
            try:
                report = fut.result()
            except Exception as exc:  # noqa: BLE001
                print(f"[{done}/{len(work)}] #{key}: FAILED {exc}", flush=True)
                continue
            if report["zones"]:
                with open(meta(out, f"#{key}_{report['name']}.zones.json"), "w") as fh:
                    json.dump(report, fh, indent=2)
            rows.append(report)
            print(f"[{done}/{len(work)}] #{key} {report['name']}: "
                  f"{report['beforePercent']:.4f}% -> {report['afterPercent']:.4f}%, "
                  f"broke {report['brokePixels']} fixed {report['fixedPixels']}", flush=True)

    keep = ("index", "name", "stem", "gamemd", "ccmaps", "beforePercent", "afterPercent",
            "brokePixels", "fixedPixels", "shiftedPixels", "newPixels", "cropChanged")
    summary = [{k: r[k] for k in keep} | {"percent": r["stats"]["percent"],
                                          "zoneCount": len(r["zones"]),
                                          "delta": round(r["afterPercent"] - r["beforePercent"], 4)}
               for r in sorted(rows, key=lambda r: r["stats"]["percent"], reverse=True)]
    with open(meta(out, "summary.json"), "w") as fh:
        json.dump(summary, fh, indent=2)
    write_report(out, summary)


if __name__ == "__main__":
    main()
