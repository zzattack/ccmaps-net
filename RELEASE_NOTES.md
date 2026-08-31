# CNCMaps 3.0.0

First release since 2.4.0 (June 2020). The project now targets .NET 10, no longer relies on a graphics context for voxel rendering, and only the GUI retains a dependency on Windows.

## Platform

- Ported from .NET Framework 4 to .NET 10. Releases are self-contained.
- The console renderer now also runs on Linux. Resulting renders are deterministically byte-identical to those made with Windows versions.
- The OpenGL voxel renderer is replaced with a built-in software rasterizer using an orthographic projection that matches the game, so rendering works fully in headless environments.
- Voxels are lit through `voxels.vpl` like the game (ZivDero), and voxel shadows are projected from the full voxel volume.
- GDI+ is replaced with ImageSharp for image encoding and overlays; PNG encoding is parallelized.
- Drawing and file lookups are noticeably faster (~25% on typical maps).

## Rendering fixes

- Objects at equal height no longer cast shadows onto each other.
- Jumpjet units with BalloonHover are drawn at their cruise altitude.
- Veinhole monsters are rendered connected to their surrounding vein field.
- Plain swamp tiles no longer get LAT transitions. *(E1Elite)*
- Ice-growth markers are clamped to the drawing surface. *(E1Elite)*
- Ground lighting is applied correctly in map lighting. *(Starkku)*
- Tiled start markers are sized per engine, and start-position waypoints outside the map are skipped.

## Engine autodetection

- TS maps are no longer mistaken for RA2 when only TS game data is present.
- For of RA2/YR maps, file extension settle ties (`.yrm`/`.yro` map prefers Yuri's Revenge, `.mpr` prefers RA2).
- Detection is much faster: probes run lazily and their game data is cached across renders.

## Mod support

- Phobos `$Include` and `$Inherits` are supported in rules and art. *(MortonPL)*
- Ares `#include` files that cannot be found are skipped. *(handama)*
- CnCNet client `[INISystem]BasedOn` map inheritance is resolved.
- `BaseSection` inheritance is resolved in ini lookups.
- Mod configs with linear `FrameDeciderCode` expressions work again.
- Relative paths in a mod config resolve against the config file itself, so a `modconfig.xml` can ship inside the mod folder it describes.

## Command line

- New `--meta-json` option exports the resolved map metadata (name, engine, size, players).
- New `--progress` option prints machine-readable render progress for tools that wrap the renderer.
- `-z` accepts multiple thumbnail specs, each with its own name, dimensions and JPEG quality.
- `--mixdir` can be given more than once.
- `-x` and `--tunnelpos` now do what their help text documents.
- Map backups default to off; `--bkp` enables them. *(Starkku)*
- The map's proper name is resolved even when `-o` is given.

## GUI

- New interactive preview window after rendering: zoom/pan, heightmap and shadow views, tile inspection. Closed source component distributed only as binary.
- Update checks and bug reports now go to spysat.cc.
- The GUI is DPI-aware. *(Starkku)*

## Robustness

- Malformed or truncated map data can no longer crash the renderer (LZO bounds, missing sections, short art names, unopenable mix files, objects without a collection).
- Missing game data fails with a clear error instead of a crash.
- Added a regression test suite with some golden renders to hopefully catch regressions for future changes.

## Downloads

- The installer (Windows, console + GUI) is joined by a trimmed console-only zip (~12 MB) for mods that bundle the renderer.

Thanks to handama, Starkku, E1Elite, MortonPL and zjumelody for their contributions, and to ZivDero for the voxel lighting model.
