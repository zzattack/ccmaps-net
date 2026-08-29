"""Build a human-inspectable A/B corpus: engine captures vs ccmaps renders of the same maps.

  python humaneval.py all [--limit N]

Three steps, each resumable and each skipping work whose output is already on disk:

  capture   CncBufferSpyCli drives gamemd over every loose YR multiplayer map and stitches a
            full-map composite. Serial only -- the named pipe, spawn.ini and RA2MD.ini patching
            are all global state, so two runs at once corrupt each other.
  render    the same maps through CNCMaps.Renderer, with the engine's tile-variant lattice and
            every random draw pinned, so terrain variation is not reported as a difference.
  compare   arithmetic alignment (abdiff) plus connected-component zones, written per map for
            the WinForms comparison viewer to page through.

Both PNGs of a pair carry the same #NNN index and display name, so they sort next to each other
in a file listing. meta\\manifest.json is the source of truth for that mapping; the later steps
read it rather than re-deriving names.
"""

from __future__ import annotations

import argparse
import glob
import json
import os
import shutil
import subprocess
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abcompare import palette_quantize  # noqa: E402
from abdiff import (  # noqa: E402
    align,
    capture_transform,
    channel_delta,
    clusters,
    load_png,
    render_transform,
    summarize,
)

GAME_DIR = r"D:\SteamLibrary\steamapps\common\Command & Conquer Red Alert II"
MAP_DIR = os.path.join(GAME_DIR, "Maps", "Yuri's Revenge")
SPY_CLI = r"C:\Users\Frank\Desktop\workspace\cnc-buffer-spy\Debug\CncBufferSpyCli.exe"
PRESETS = r"C:\Users\Frank\Desktop\workspace\cnc-buffer-spy\presets"
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RENDERER = os.path.join(REPO, "CNCMaps.Renderer", "bin", "Release", "net10.0", "CNCMaps.Renderer.exe")
DEFAULT_OUT = os.path.join(os.path.expanduser("~"), "Desktop", "ComparisonRenders", "YR")

# The CLI reports these on stdout and still exits 0; a short composite is worth knowing about
# before someone blames the renderer for the missing strip.
STITCH_WARNINGS = ("stitch: WARNING canvas is", "stitch: canvas too large", "stitch: nothing composited")

# Above this the two images disagree about shroud rather than about art, and the zone list
# degenerates into one blob covering the map.
SUSPECT_PERCENT = 20.0
MAX_ZONES = 200


def meta_dir(out: str) -> str:
    return os.path.join(out, "meta")


def manifest_path(out: str) -> str:
    return os.path.join(meta_dir(out), "manifest.json")


def load_manifest(out: str) -> dict:
    try:
        with open(manifest_path(out)) as fh:
            return json.load(fh)
    except FileNotFoundError:
        return {}


def save_manifest(out: str, manifest: dict):
    os.makedirs(meta_dir(out), exist_ok=True)
    with open(manifest_path(out), "w") as fh:
        json.dump(manifest, fh, indent=2)


def log_failure(out: str, line: str):
    with open(os.path.join(out, "failures.txt"), "a") as fh:
        fh.write(line.rstrip() + "\n")


def enumerate_maps(map_dir: str) -> list[str]:
    """The loose maps at the root only; the subfolders hold campaign and mission files."""
    files = [f for f in glob.glob(os.path.join(map_dir, "*.map")) if os.path.isfile(f)]
    return sorted(files, key=lambda p: os.path.splitext(os.path.basename(p))[0].lower())


def index_maps(out: str, map_dir: str) -> dict:
    """Assign every map its permanent index. Indices come from the full listing, so a --limit run
    and a full run agree on which map is #001."""
    manifest = load_manifest(out)
    for i, path in enumerate(enumerate_maps(map_dir), start=1):
        key = f"{i:03d}"
        entry = manifest.setdefault(key, {})
        entry["index"] = i
        entry["stem"] = os.path.splitext(os.path.basename(path))[0]
        entry["map"] = path
    save_manifest(out, manifest)
    return manifest


def selected(manifest: dict, limit: int | None) -> list[tuple[str, dict]]:
    items = sorted(manifest.items(), key=lambda kv: kv[1]["index"])
    return items[:limit] if limit else items


# ---------------------------------------------------------------------------- capture


def capture_one(map_path: str, work: str, timeout: int) -> subprocess.CompletedProcess:
    shutil.rmtree(work, ignore_errors=True)
    os.makedirs(work, exist_ok=True)
    cmd = [
        SPY_CLI,
        "--engine", "YR",
        "--exe", os.path.join(GAME_DIR, "gamemd-spawn.exe"),
        "--settings", os.path.join(GAME_DIR, "RA2MD.ini"),
        "--spawn", os.path.join(GAME_DIR, "spawn.ini"),
        "--map", map_path,
        "--out", work,
        "--maxcells", "150",
        "--frames", "1",
        "--timeout", str(timeout),
        "--freezeframe", "1",
        "--nozip", "--stitch", "--hidden",
        "--mergeini", os.path.join(PRESETS, "hide_mcv_ra2yr.ini"),
        "--mergeini", os.path.join(PRESETS, "reveal_ra2yr.ini"),
        "--names", "mpmaps",
    ]
    return subprocess.run(cmd, capture_output=True, text=True, timeout=timeout + 300)


def cmd_capture(args):
    out = args.outdir
    os.makedirs(out, exist_ok=True)
    manifest = index_maps(out, args.mapdir)
    work = os.path.join(meta_dir(out), "_work")

    for key, entry in selected(manifest, args.limit):
        existing = glob.glob(os.path.join(out, f"#{key}_GAMEMD_*.png"))
        if existing:
            # Recovered from the files themselves, so a lost manifest does not cost hours of recapture.
            found = os.path.basename(existing[0])
            base = found[len(f"#{key}_GAMEMD_"):-len(".png")]
            entry.update(name=base, gamemd=found, capture=f"#{key}_{base}.capture.json")
            save_manifest(out, manifest)
            print(f"#{key} {entry['stem']}: capture present, skipped")
            continue

        print(f"#{key} {entry['stem']}: capturing...", flush=True)
        started = time.time()
        try:
            proc = capture_one(entry["map"], work, args.timeout)
        except subprocess.TimeoutExpired:
            log_failure(out, f"#{key} {entry['stem']}: CLI wall-clock timeout")
            print("  TIMEOUT")
            continue
        output = (proc.stdout or "") + (proc.stderr or "")
        pngs = glob.glob(os.path.join(work, "*.png"))

        if proc.returncode != 0 or len(pngs) != 1:
            log_failure(out, f"#{key} {entry['stem']}: exit {proc.returncode}, {len(pngs)} png(s) | "
                             f"{output.strip()[-400:]}")
            print(f"  FAILED (exit {proc.returncode}, {len(pngs)} png)")
            time.sleep(3)
            continue

        for line in output.splitlines():
            if any(w in line for w in STITCH_WARNINGS):
                log_failure(out, f"#{key} {entry['stem']}: WARNING {line.strip()}")

        base = os.path.splitext(os.path.basename(pngs[0]))[0]
        gamemd = f"#{key}_GAMEMD_{base}.png"
        capture_json = f"#{key}_{base}.capture.json"
        shutil.move(pngs[0], os.path.join(out, gamemd))
        src_json = os.path.join(work, base + ".json")
        if not os.path.exists(src_json):
            log_failure(out, f"#{key} {entry['stem']}: capture png without sidecar json")
            print("  FAILED (no sidecar json)")
            time.sleep(3)
            continue
        shutil.move(src_json, os.path.join(meta_dir(out), capture_json))

        entry.update(name=base, gamemd=gamemd, capture=capture_json)
        save_manifest(out, manifest)
        print(f"  ok {base} ({time.time() - started:.0f}s)")
        # The dying game has to release the pipe name before the next launch claims it.
        time.sleep(3)

    shutil.rmtree(work, ignore_errors=True)
    save_manifest(out, manifest)


# ---------------------------------------------------------------------------- render


def run_streaming(cmd: list[str]) -> tuple[int, str]:
    """Run `cmd` and forward its stdout line by line, so a caller watching our own pipe (the
    comparison viewer) sees progress while the child is still running."""
    lines = []
    proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                            text=True, bufsize=1)
    for line in proc.stdout:
        lines.append(line)
        sys.stdout.write(line)
        sys.stdout.flush()
    proc.wait()
    return proc.returncode, "".join(lines)


def build_renderer():
    print("building renderer...", flush=True)
    proc = subprocess.run(["dotnet", "build", os.path.join(REPO, "CNCMaps.slnx"), "-c", "Release"],
                          capture_output=True, text=True)
    if proc.returncode != 0:
        sys.stderr.write(proc.stdout + proc.stderr)
        raise SystemExit("dotnet build failed")


def mix_dirs() -> list[str]:
    dirs = [GAME_DIR]
    for sub in ("MIX", "INI"):
        path = os.path.join(GAME_DIR, sub)
        if os.path.isdir(path):
            dirs.append(path)
    return dirs


def cmd_render(args):
    out = args.outdir
    manifest = load_manifest(out)
    if not manifest:
        raise SystemExit("no manifest; run capture first")
    build_renderer()

    for key, entry in selected(manifest, args.limit):
        name = entry.get("name")
        if not name:
            continue
        base = f"#{key}_CCMAPS_{name}"
        png = os.path.join(out, base + ".png")
        if os.path.exists(png):
            entry.setdefault("ccmaps", base + ".png")
            entry.setdefault("render", f"#{key}_{name}.render.json")
            print(f"#{key} {name}: render present, skipped")
            continue

        render_json = f"#{key}_{name}.render.json"
        cmd = [
            RENDERER,
            "-i", entry["map"],
            "-d", out,
            "-o", base,
            "-p", "-f", "-Y",
            "--pin-random",
            "--progress",
            "--meta-json", os.path.join(meta_dir(out), render_json),
        ]
        for d in mix_dirs():
            cmd += ["-m", d]
        with open(os.path.join(meta_dir(out), entry["capture"])) as fh:
            cap = json.load(fh)
        lattice = (cap.get("provenance") or {}).get("variantLattice")
        if lattice:
            cmd += ["--tile-lattice", ",".join(str(v) for v in lattice)]
        cmd += ["--anim-frame", str(cap.get("frame", 6))]

        print(f"#{key} {name}: rendering...", flush=True)
        code, output = run_streaming(cmd)
        if code != 0 or not os.path.exists(png):
            log_failure(out, f"#{key} {name}: renderer exit {code} | {output.strip()[-400:]}")
            print(f"#{key} {name}: RENDER FAILED (exit {code})", flush=True)
            continue

        entry.update(ccmaps=base + ".png", render=render_json)
        save_manifest(out, manifest)
        print(f"#{key} {name}: rendered", flush=True)

    save_manifest(out, manifest)


# ---------------------------------------------------------------------------- compare


def compare_one(out: str, entry: dict, tolerance: int, min_area: int) -> dict:
    with open(os.path.join(meta_dir(out), entry["capture"])) as fh:
        cap_meta = json.load(fh)
    with open(os.path.join(meta_dir(out), entry["render"])) as fh:
        ren_meta = json.load(fh)

    ct = capture_transform(cap_meta)
    if ct is None:
        raise ValueError("capture has no cell transform")
    rt = render_transform(ren_meta)

    cap = load_png(os.path.join(out, entry["gamemd"]))
    ren = palette_quantize(load_png(os.path.join(out, entry["ccmaps"])))
    cap_a, ren_a, _ = align(cap, ct, ren, rt)

    # align() only reports the a-side origin; the viewer needs both crops, and they come out of the
    # same dx/dy split it uses.
    dx = ct.off_x - rt.off_x
    dy = ct.off_y - rt.off_y

    # The same magnitudes diff_mask thresholds on, kept so each zone can report how far off it is
    # as well as how large.
    delta = channel_delta(cap_a, ren_a)
    mask = delta > tolerance
    stats = summarize(mask)
    zones = [] if stats["percent"] > SUSPECT_PERCENT else clusters(mask, min_area, delta)[:MAX_ZONES]

    return {
        "index": entry["index"],
        "name": entry["name"],
        "stem": entry["stem"],
        "gamemd": entry["gamemd"],
        "ccmaps": entry["ccmaps"],
        "alignedSize": [int(cap_a.shape[1]), int(cap_a.shape[0])],
        "captureOrigin": [max(0, dx), max(0, dy)],
        "renderOrigin": [max(0, -dx), max(0, -dy)],
        "stats": stats,
        "zones": zones,
    }


def cmd_compare(args):
    out = args.outdir
    manifest = load_manifest(out)
    if not manifest:
        raise SystemExit("no manifest; run capture first")
    rows = []

    for key, entry in selected(manifest, args.limit):
        if not all(entry.get(k) for k in ("gamemd", "ccmaps", "capture", "render")):
            continue
        zones_name = f"#{key}_{entry['name']}.zones.json"
        zones_path = os.path.join(meta_dir(out), zones_name)
        if os.path.exists(zones_path):
            with open(zones_path) as fh:
                report = json.load(fh)
        else:
            print(f"#{key} {entry['name']}: comparing...", flush=True)
            try:
                report = compare_one(out, entry, args.tolerance, args.min_area)
            except Exception as exc:
                log_failure(out, f"#{key} {entry.get('name', entry['stem'])}: compare failed: {exc}")
                print(f"#{key}: COMPARE FAILED {exc}")
                continue
            with open(zones_path, "w") as fh:
                json.dump(report, fh, indent=2)
            entry["zones"] = zones_name
            save_manifest(out, manifest)

        percent = report["stats"]["percent"]
        rows.append({
            "index": report["index"],
            "name": report["name"],
            "stem": report["stem"],
            "gamemd": report["gamemd"],
            "ccmaps": report["ccmaps"],
            "zones": zones_name,
            "percent": percent,
            "zoneCount": len(report["zones"]),
            "suspect": percent > SUSPECT_PERCENT,
        })
        print(f"#{key} {report['name']}: {percent}% ({len(report['zones'])} zones)", flush=True)

    rows.sort(key=lambda r: r["percent"], reverse=True)
    with open(os.path.join(meta_dir(out), "summary.json"), "w") as fh:
        json.dump(rows, fh, indent=2)
    if rows:
        mean = sum(r["percent"] for r in rows) / len(rows)
        print(f"\n{len(rows)} maps, mean {mean:.3f}%, {sum(r['suspect'] for r in rows)} suspect")


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("step", choices=["capture", "render", "compare", "all"])
    ap.add_argument("--outdir", default=DEFAULT_OUT)
    ap.add_argument("--mapdir", default=MAP_DIR)
    ap.add_argument("--limit", type=int, help="only the first N maps, for smoke tests")
    ap.add_argument("--tolerance", type=int, default=8)
    ap.add_argument("--min-area", type=int, default=30)
    ap.add_argument("--timeout", type=int, default=600,
                    help="per-map CLI timeout; its own frame wait floor is max(120, timeout/2)s, so "
                         "anything short aborts a stitch sweep mid-way")
    args = ap.parse_args()

    os.makedirs(meta_dir(args.outdir), exist_ok=True)
    for step in (["capture", "render", "compare"] if args.step == "all" else [args.step]):
        print(f"=== {step} ===")
        {"capture": cmd_capture, "render": cmd_render, "compare": cmd_compare}[step](args)


if __name__ == "__main__":
    main()
