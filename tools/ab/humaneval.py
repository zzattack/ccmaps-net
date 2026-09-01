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

import numpy as np
from concurrent.futures import ThreadPoolExecutor, as_completed

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from abcompare import palette_quantize  # noqa: E402
from abdiff import (
    split_tonal,  # noqa: E402
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
    return subprocess.run(cmd, capture_output=True, text=True, timeout=timeout + 60)


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


def render_cmd(out: str, key: str, entry: dict, base: str, render_json: str,
               extra: list[str] | None = None) -> list[str]:
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
    # The captures come from the CnCNet spawner, whose InitBootstrapMixFiles_CustomMixes hook jumps
    # over gamemd's expandmd##.mix loop and registers cncnet.mix (its own rulesmd/artmd and tree
    # art) in that slot. A Terrain Expansion pack installed as expandmd06.mix never reaches the
    # game, and neither does the 1.001 patch's expandmd01.mix.
    cmd += ["--no-expand-mixes", "-m", os.path.join(GAME_DIR, "cncnet.mix")]
    with open(os.path.join(meta_dir(out), entry["capture"])) as fh:
        cap = json.load(fh)
    lattice = (cap.get("provenance") or {}).get("variantLattice")
    if lattice:
        cmd += ["--tile-lattice", ",".join(str(v) for v in lattice)]
    cmd += ["--anim-frame", str(cap.get("frame", 6))]
    return cmd + (extra or [])


def cmd_render(args):
    out = args.outdir
    manifest = load_manifest(out)
    if not manifest:
        raise SystemExit("no manifest; run capture first")
    build_renderer()

    todo = []
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
        todo.append((key, entry, name, base, png, render_json))

    if not todo:
        save_manifest(out, manifest)
        return

    def finish(key, entry, name, base, png, render_json, code, output):
        if code != 0 or not os.path.exists(png):
            log_failure(out, f"#{key} {name}: renderer exit {code} | {output.strip()[-400:]}")
            print(f"#{key} {name}: RENDER FAILED (exit {code})", flush=True)
            return False
        entry.update(ccmaps=base + ".png", render=render_json)
        return True

    # One map, or --jobs 1: keep streaming the renderer's own --progress output, which the viewer's
    # re-render reads to drive its progress bar. Interleaving eight of those is noise, so a batch
    # run reports one line per finished map instead.
    if args.jobs <= 1 or len(todo) == 1:
        for key, entry, name, base, png, render_json in todo:
            print(f"#{key} {name}: rendering...", flush=True)
            code, output = run_streaming(render_cmd(out, key, entry, base, render_json, args.render_arg))
            if finish(key, entry, name, base, png, render_json, code, output):
                save_manifest(out, manifest)
                print(f"#{key} {name}: rendered", flush=True)
        save_manifest(out, manifest)
        return

    jobs = min(args.jobs, len(todo))
    print(f"rendering {len(todo)} maps on {jobs} processes", flush=True)
    started = time.time()
    done = 0
    with ThreadPoolExecutor(max_workers=jobs) as pool:
        futures = {}
        for item in todo:
            key, entry, name, base, png, render_json = item
            cmd = render_cmd(out, key, entry, base, render_json, args.render_arg)
            futures[pool.submit(subprocess.run, cmd, capture_output=True, text=True)] = item
        for fut in as_completed(futures):
            key, entry, name, base, png, render_json = futures[fut]
            done += 1
            try:
                proc = fut.result()
                code, output = proc.returncode, (proc.stdout or "") + (proc.stderr or "")
            except Exception as exc:  # noqa: BLE001 - a crashed child must not sink the batch
                code, output = -1, repr(exc)
            if finish(key, entry, name, base, png, render_json, code, output):
                print(f"[{done}/{len(todo)}] #{key} {name}: rendered", flush=True)
            # the manifest is the only shared state; written from this thread only
            if done % 25 == 0:
                save_manifest(out, manifest)
    print(f"rendered {len(todo)} maps in {time.time() - started:.0f}s", flush=True)

    save_manifest(out, manifest)


# ---------------------------------------------------------------------------- compare


def compare_one(out: str, entry: dict, tolerance: int, min_area: int, tint_radius: int,
                tint_max: int) -> dict:
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
    # percent and zones are the structural difference; a tint on the same art is reported
    # separately and never makes a zone (tint_max 0 restores the raw pixel diff)
    if tint_max > 0:
        mask, tonal = split_tonal(cap_a, ren_a, tolerance, tint_radius, tint_max)
    else:
        mask = delta > tolerance
        tonal = np.zeros_like(mask)
    stats = summarize(mask)
    stats["tonal"] = int(tonal.sum())
    stats["tonalPercent"] = round(100.0 * stats["tonal"] / stats["pixels"], 4)
    stats["rawPercent"] = round(stats["percent"] + stats["tonalPercent"], 4)
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

    work = []
    for key, entry in selected(manifest, args.limit):
        if not all(entry.get(k) for k in ("gamemd", "ccmaps", "capture", "render")):
            continue
        zones_name = f"#{key}_{entry['name']}.zones.json"
        zones_path = os.path.join(meta_dir(out), zones_name)
        work.append((key, entry, zones_name, zones_path))

    # Diffing two full-map PNGs is numpy-bound and releases the GIL, so threads get most of the win
    # without pickling the images across processes.
    fresh = [w for w in work if not os.path.exists(w[3])]
    reports = {}
    if fresh:
        jobs = max(1, min(args.jobs, len(fresh)))
        print(f"comparing {len(fresh)} maps on {jobs} threads", flush=True)
        started = time.time()
        done = 0
        with ThreadPoolExecutor(max_workers=jobs) as pool:
            futures = {pool.submit(compare_one, out, entry, args.tolerance, args.min_area,
                                   args.tint_radius, args.tint_max): w
                       for w in fresh for key, entry, _, _ in [w]}
            for fut in as_completed(futures):
                key, entry, zones_name, zones_path = futures[fut]
                done += 1
                try:
                    report = fut.result()
                except Exception as exc:  # noqa: BLE001
                    log_failure(out, f"#{key} {entry.get('name', entry['stem'])}: compare failed: {exc}")
                    print(f"#{key}: COMPARE FAILED {exc}", flush=True)
                    continue
                with open(zones_path, "w") as fh:
                    json.dump(report, fh, indent=2)
                entry["zones"] = zones_name
                reports[key] = report
                print(f"[{done}/{len(fresh)}] #{key} {entry['name']}: {report['stats']['percent']:.4f}%", flush=True)
        save_manifest(out, manifest)
        print(f"compared {len(fresh)} maps in {time.time() - started:.0f}s", flush=True)

    for key, entry, zones_name, zones_path in work:
        report = reports.get(key)
        if report is None:
            if not os.path.exists(zones_path):
                continue
            with open(zones_path) as fh:
                report = json.load(fh)

        percent = report["stats"]["percent"]
        rows.append({
            "index": report["index"],
            "name": report["name"],
            "stem": report["stem"],
            "gamemd": report["gamemd"],
            "ccmaps": report["ccmaps"],
            "zones": zones_name,
            "percent": percent,
            "tonalPercent": report["stats"].get("tonalPercent", 0.0),
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
    ap.add_argument("--jobs", type=int, default=8,
                    help="parallel renders; 1 keeps the streaming progress output")
    ap.add_argument("--render-arg", action="append", default=[], metavar="ARG",
                    help="extra argument passed to every renderer invocation; repeat for more. Lets "
                         "one capture set be rendered twice under different options and compared.")
    ap.add_argument("--tolerance", type=int, default=8)
    ap.add_argument("--min-area", type=int, default=30)
    ap.add_argument("--tint-radius", type=int, default=3,
                    help="window radius of the tonal/structural split (abdiff.split_tonal)")
    ap.add_argument("--tint-max", type=int, default=40,
                    help="a local colour shift above this on any channel is structural, not tint; "
                         "0 disables the split and counts every differing pixel")
    ap.add_argument("--timeout", type=int, default=60,
                    help="per-map CLI budget covering launch, first frame and the stitch sweep. The "
                         "slowest of 440 corpus maps takes 13s; a map that misses this is one the "
                         "game refuses to load, and every second above it is spent waiting on that.")
    args = ap.parse_args()

    os.makedirs(meta_dir(args.outdir), exist_ok=True)
    for step in (["capture", "render", "compare"] if args.step == "all" else [args.step]):
        print(f"=== {step} ===")
        {"capture": cmd_capture, "render": cmd_render, "compare": cmd_compare}[step](args)


if __name__ == "__main__":
    main()
