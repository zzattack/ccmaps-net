# Human A/B evaluation — guide for follow-up sessions

## Session kickoff prompt

Paste this to start an evaluation session, filling in the target:

> We are evaluating ccmaps-net renderer changes against ground-truth gamemd captures. Read `tools\ab\HUMANEVAL.md` and follow its workflow. The corpus is at `~\Desktop\ComparisonRenders\YR` (`meta\summary.json` is the ranked overview, `verdicts.csv` holds my zone verdicts).
>
> Today's target: <the systemic difference, map, or zone to address>.
>
> Rules: captures are ground truth — never re-capture and never run humaneval `all`. After each renderer change, re-render + re-compare only the affected maps (delete their CCMAPS png / render.json / zones.json, then `humaneval.py render` + `compare`); I flip results in ComparisonViewer, or press R there myself. Before declaring the change done, snapshot `meta\summary.json` to `summary.before.json`, run the corpus regression from the guide, and report the mean plus EVERY map whose percent increased, with before/after values. Comparison artifacts before committing; atomic commits.

How to use the gamemd-vs-ccmaps comparison setup to evaluate renderer/engine changes.
Built 2026-08-29; plan at `~\.claude\plans\tidy-jingling-volcano.md`.

## Components

| Piece | Location | Role |
|---|---|---|
| Pipeline | `tools\ab\humaneval.py` (run with `C:\Python314\python.exe`, cwd = repo root) | `capture \| render \| compare \| all` subcommands; every step skips outputs that already exist |
| Ground truth | CncBufferSpyCli at `C:\Users\Frank\Desktop\workspace\cnc-buffer-spy\Debug\` | Stitched in-game screenshots of the real engine |
| Corpus | `%USERPROFILE%\Desktop\ComparisonRenders\YR\` | 449 aligned pairs of all loose MP maps in the Steam YR maps folder |
| Viewer | `ComparisonViewer\` (own git repo, excluded from ccmaps-net via `.git\info\exclude`; never push it anywhere — it consumes the private ZoomableCanvasNET.dll) | Human inspection: anchored A/B flicker, zone cycling |
| Diff library | `tools\ab\abdiff.py`, `abcompare.py` | Alignment/diff/cluster primitives the pipeline reuses; also the entry point for programmatic analysis |

## Corpus layout

```
#NNN_GAMEMD_<name>.png / #NNN_CCMAPS_<name>.png   the pair (sorts adjacently)
meta\manifest.json                                 index -> stem/path/name; source of truth
meta\#NNN_<name>.capture.json                      engine metadata (cellTransform, provenance.variantLattice)
meta\#NNN_<name>.render.json                       ccmaps --meta-json
meta\#NNN_<name>.zones.json                        alignment origins + stats + zone bboxes
meta\summary.json                                  all maps ranked by percent; suspect flag >20%
failures.txt / verdicts.csv                        capture failures / human verdicts (index,name,zone,verdict; zone 1-based)
```

Zone coordinates live in the aligned common space; `captureOrigin`/`renderOrigin` map back into each PNG. Alignment is arithmetic (both sides publish a cell transform), never a search.

## Evaluating an engine change

**Targeted loop (single map, seconds):** the user selects the map in the viewer and presses `R`. That deletes the map's CCMAPS png + render.json + zones.json, runs `humaneval.py render` then `compare` (render includes `dotnet build CNCMaps.slnx -c Release`, so the freshly changed renderer code is what runs), and reloads the pair in place. Agent-side equivalent: delete those three files yourself, then run the two subcommands.

**Corpus regression (all maps, ~2.5 min):** after an engine change that should hold corpus-wide:

```powershell
cd $env:USERPROFILE\Desktop\ComparisonRenders\YR
Copy-Item meta\summary.json meta\summary.before.json
Remove-Item "#*_CCMAPS_*.png"; Remove-Item meta\*.render.json; Remove-Item meta\*.zones.json
cd C:\Users\Frank\Desktop\workspace\ccmaps-net
C:\Python314\python.exe tools\ab\humaneval.py render
C:\Python314\python.exe tools\ab\humaneval.py compare
```

Both steps run 8-wide by default (`--jobs`), putting a full sweep at about two and a half
minutes: ~60s to render 449 maps and ~80s to compare them. Output is byte-identical to a
serial run -- the renderer is pinned by `--pin-random` and `--tile-lattice`, and renders and
zones.json alike were checked hash-for-hash against `--jobs 1` before this became the default.
A batch prints one line per finished map; `--jobs 1` restores the streamed per-map `--progress`
output that the viewer's `R` re-render reads, and a single-map run takes that path on its own.

Then diff `summary.before.json` against the new `summary.json` per map. Frank's standing rule for pixel changes: a no-regression gate — report the mean and every map whose percent went UP, not just the improved ones. Keep the before-file until the change is accepted.

**Interpreting numbers:** the compare puts the ccmaps side on the engine's color precision (`palette_quantize`: 6-bit palette floor + RGB565) with tolerance 8, and since 2026-09-01 `percent` counts only STRUCTURAL pixels: `abdiff.split_tonal` drops a differing pixel when its 7x7 window explains the difference as a constant colour shift or a gain of the same texture, and that shift stays under `--tint-max` (40) on every channel. A cell lit one intensity step apart or a lamp falling off differently is tint (reported as `tonalPercent`/`rawPercent` in stats and summary rows, never a zone); a sprite drawn a pixel off, a missing shadow or a wrong palette stays structural (synthetic checks: a 1-2 px shift keeps 83-88% of its pixels, a 0.9/1.15 gain keeps under 1%, a half-darkened block keeps 100%). `--tint-max 0` restores the raw diff. Corpus mean on the structural metric: 0.245% (raw 1.807%) on 2026-09-01; every summary before that date is on the raw metric (`meta\summary.raw.json` holds the last raw one). Sub-0.05% is near-parity now. Maps >20% are flagged `suspect` and get no zone list (they would be one giant blob). Known systemic residuals are being addressed in a dedicated session (see `verdicts.csv` and the human-ab-eval memory).

## Ground-truth captures: leave them alone

Captures are the stable reference — an engine change on our side never invalidates them. Re-run `capture` only for new maps or after CncBufferSpy changes. If you must:

- Needs the game dir free (launches gamemd hidden, patches spawn.ini/RA2MD.ini, kills every gamemd/Syringe process); strictly one at a time.
- `#108 4_limbo_of_the_lost` fails deterministically ("no frames from the game", twice) — skip it, don't debug it as a pipeline fault.
- Never use the `all` subcommand while #108 has no capture: it retries that map and stalls for minutes. Use `render` + `compare`.
- Re-rendering depends on the capture sidecars: `--tile-lattice` comes from `provenance.variantLattice`, which is what keeps random tile variants from polluting the zones.

## Viewer reference

Launch `ComparisonViewer` (opens the corpus folder by default; folder path as arg 1 works). Map list left with sort selector (index / diff% / zones-after-filter); zone list + log-scale min-area slider right, plus an Area/Severity toggle that ranks zones by mean color delta instead of area; the slider filters list, cycling, rectangles and counts everywhere consistently. Left-clicking inside a zone box selects it (smallest containing zone wins) without moving the view. The stamp above the active zone shows rank, size and delta stats (`Δ mean ±stdev`; per-zone `meanDelta`/`stdevDelta`/`maxDelta` are written by `compare`). Re-render progress streams into the status bar (the renderer's `--progress` phases drive a real progress bar).

Keys: `Space`/`Tab` flip GAMEMD/CCMAPS (view stays pixel-anchored — the core feature), `Left`/`Right` or `N`/`P` cycle zones with auto-zoom, `Q`/`E` or `PgUp`/`PgDn` cycle maps, `F` fit, `Z` toggle zone rects, `R` re-render selected map, `G`/`B` log verdict + advance, `+`/`-` zoom, hold `F1` for the hotkey overlay; right-drag pans, wheel zooms. The active zone box carries a GAMEMD/CCMAPS stamp in the side's color.

Caveats: the map list is read once at startup — restart to see corpus entries added outside the viewer (`R` refreshes in place). While the viewer runs it locks its Debug exe; build with `-c Release` to verify code changes without killing it.

## Knobs

- `humaneval.py compare --min-area <N>` (default 30) and `--tolerance <N>` (default 8): re-running `compare` after deleting zones.json files only rewrites analysis, no rendering. Many maps cap at 200 stored zones; the viewer's slider filters client-side, so prefer the slider over re-comparing.
- `render` passes `--anim-frame <capture.json frame>` so animations draw the exact frame the
  engine showed when its logic was frozen (flags, fountains, oil-derrick flares, waterfalls).
  The tick simulation lives in `CNCMaps.Engine\Game\FrameDeciders.SimulateAnimStage`; power-gated
  anim slots (`<slot>Powered`, default yes) on `NeedsEngineer` or `Powered=true` buildings hold at
  their start frame like the game's unpowered-anim pause (a neutral oil derrick's pump stands
  still while its `Powered=no` flare keeps burning). A `NeedsEngineer` building that a game-start
  trigger hands to a player (`MapFile.ApplyPreCapturedOwners`) runs freely from tick 0 instead,
  which is what the engine's change-house capture path does.
- `render` mirrors the CnCNet spawner's mix set: `--no-expand-mixes` (the spawner's
  `InitBootstrapMixFiles_CustomMixes` hook skips gamemd's expandmd##.mix loop, so a Terrain
  Expansion installed as expandmd06.mix and the 1.001 patch's expandmd01.mix never load in a
  CnCNet game) plus `-m <game>\cncnet.mix` in that slot (CnCNet's own rulesmd/artmd, `$Include`
  files and tree art). Rendering with the plain game dir instead draws Terrain Expansion tile
  overrides (grey slope17-20 wedges in NewUrban) and 1.001 rules the captures never saw.
- The captured game runs with Ares and Phobos injected (Syringe), so their engine hooks are part of
  the ground truth. Ares' `MediansFix` (hook 0x545904) gives snow `Medians = 71` when snowmd.ini
  omits the key; the renderer mirrors it in `TileCollection`. Check `Ares.dll` strings for a hook name
  before concluding that a gamemd decompile alone explains a capture.
- A map section for a lamp type that omits a light key turns that key into whole units in the game
  (`BuildingTypeClass::Read_INI` re-reads the type with the per-mille field / 1000 as default, integer
  division), so an override without `LightIntensity` switches the lamp off. `LightSource.TruncateKeysOmittedBy`
  mirrors it; xeb2 Sinkhole is the reference case. Proven with scratch captures, not a hook.
- TS/FS comparisons: planned, not built. The pipeline defaults (map dir, game dir, presets, `-Y`) are YR-specific.
