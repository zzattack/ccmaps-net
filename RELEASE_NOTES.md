# CNCMaps 3.1.0 (unreleased)

Every change below was checked against frames captured from the running game (gamemd with logic frozen at a known tick) over the 450 loose multiplayer maps that ship with Yuri's Revenge. Where the game's behaviour was not obvious it was read out of the engine itself, and the numbers quoted are the share of pixels that still differ from the capture, averaged over that corpus.

## Depth and occlusion

- Per-pixel depth now follows the engine's model: standing shapes recede 1 z per 3 rows from their own drawn bottom row, flat shapes lie on the ground ramp, and the test is strict (a tie keeps the earlier drawing) with the engine's rounding of the profile start to a multiple of 3.
- Buildings are depth-shaped with BUILDNGZ.SHA, the 396x477 pyramid the game blits with every building body, placed and biased the way the engine does. Wide sprites no longer stand in front of cliffs and trees that hide their edges in game.
- Units, infantry and aircraft are depth-tested but never write depth; voxel bodies get the standing test they never had and anchor at the bottom of their projected volume, so a turret no longer draws behind the post it is mounted on.
- Overlays draw in the terrain pass with the engine's lifts (flat overlays one in front of the ground, walls and rocks likewise, standing overlays 17); bridge deck pieces take their depth from their own cell, so cars on the last piece of a high bridge are visible again.
- Building animations draw after every object and write no depth, as AnimClass does; each anchors at its own bottom row, so a mast-mounted flag disappears into the roof where it should.
- Smudges are tested against depth but never write it, and a multi-cell crater is drawn once per footprint cell at that cell's level.
- Tiles without a z-data section draw untested, like Blit_Iso_Tile.
- Cells, overlays and terrain objects are walked bottom row up, left to right, which decides which of two equal-depth bridge pieces or neighbouring trees shows.

## Shadows

- Shadows darken only where the caster's ground plane wins the depth test, so they no longer fall across building facades or objects standing in front of them, and they clip against up-slopes.
- A building's shadow and a tree's shadow stack on the same pixel like the game's; two shadows of one kind never do. Building shadows draw after their body at the engine's lift.
- Voxel shadows are the ShadowIndex section's bottom face, one dot per column, shifted 3 px right as the engine's light angle does.

## Lighting

- Light intensity quantizes to the game's 63 steps for RA2 and YR as well as TS; previously only maps whose lighting worked out to exactly 1.0 were exact.
- Lamp light is clamped and normalized like CellClass::ComputeLighting, so stacked or negative lamps saturate instead of discolouring.
- A lamp lights from its building's centre coordinate. GALITE-style posts declare a 0x0 foundation and light from their cell's top corner, half a cell up-left of where we had them.
- A map section that overrides a lamp type without restating its light keys switches the lamp off, as the game's integer re-read does; a map override of InvisibleInGame=no does not bring a hidden building back.
- Infantry, vehicles and aircraft add ExtraUnitLight / ExtraInfantryLight / ExtraAircraftLight to their brightness.
- AlphaImage glows of invisible lamp buildings (TSTLAMP) are drawn again, and every glow is applied after the map is complete, over whatever ends up beneath it.

## Terrain

- Tile variants are picked the way the game picks them: a fixed Latin square for small sets and an 8x8 lattice for larger ones, so LAT transitions match the engine cell for cell.
- Ramp smoothing pieces are substituted like the game's LAT recalculation; temperate slopes were never smoothed before.
- Objects on a ramp stand on the slope surface, and the lift rounds like Z_Lepton_To_Pixel (7 or 8 px depending on the cell's height).
- A cell that autolat downgrades to plain draws the plain tile instead of the map's transition art.
- Snow pavement joins the "paved road bits" set seamlessly, matching Ares' MediansFix that the game runs with.
- Ore and gems draw the type's pooled per-cell image (pool[(x*y) % 12]) instead of the stored overlay id; high bridge spans vary their frame per cell.
- Plain overlays (crates, drums, pallets) use the cell's ISO palette; rocks on a slope stay at their cell's level.

## Objects

- Infantry stand at the sub-cell spot the map gives them and draw the art of their own section (a camel is no longer a monkey).
- Voxels render one pixel per voxel, stepped in 8.8 fixed point from the far corner like the game's voxel library; vehicles were a half voxel too fat on every side.
- Buildings burn: damage fires are drawn, picked round-robin from DamageFireTypes like Start_Damage_Fires, gated on ConditionRed for occupiable buildings, and placed through the game's lepton round trip.
- House colours use the engine's remap ramp (hue kept, saturation up a sine, value down a cosine to black) and its integer HSV conversion. Neutral and Special objects remap in LightGrey under RA2/YR rules.
- Tech buildings handed to a starting player by a map trigger take that player's colour and run their pumps and flares from the first tick, while a neutral derrick stands still.
- Animations are drawn at the frame the game shows at a given tick (--anim-frame), with the load-time phase the game applies.
- Object types listed without a rules section (CALOND02, CALA02) are dropped instead of drawn as a placeholder slab; country-owned objects on multiplayer-only maps are dropped, as a skirmish never creates them.
- Only a rules-declared InvisibleInGame hides a building; the old hardcoded lamp list also swallowed real light posts.

## Robustness

- Facings outside 0-255, negative frame indices, one-past-the-end IsoMapPack5 entries, malformed base64 tails and a missing buildngz no longer crash a render.
- The preview plugin loads again (the two-argument GetObjectsAt it binds to is back).

## Command line and tools

- --anim-frame, --pin-random, --tile-lattice, --no-expand-mixes, --precapture, --thumb-markers, --debug-zbuffer, --debug-tiles, --debug-voxelmask; --meta-json now carries terrain, resource and object statistics.
- tools/ab: the capture-render-compare pipeline against the running game, with a structural difference metric that ignores pure tint shifts.
- CNCMaps.MixTool inspects and extracts .mix archives; CNCMaps.TileScan reports each map's theater and tile usage.
- A TS golden render covers the alpha light posts; 70 golden tests in all.

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
