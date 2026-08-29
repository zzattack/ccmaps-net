"""Assemble the parity writeup: ledger cards, per-map table and embedded crops."""

from __future__ import annotations

import html
import json
import sys

template_path, images_path, manifest_path, table_path, out_path = sys.argv[1:6]

with open(template_path, encoding="utf-8") as fh:
    page = fh.read()
with open(images_path, encoding="utf-8") as fh:
    images = fh.read()
with open(manifest_path, encoding="utf-8") as fh:
    manifest = json.load(fh)
with open(table_path, encoding="utf-8") as fh:
    table = json.load(fh)

# name -> (kind, heading, where, prose)
ENTRIES = {
    "ore": ("defect", "Tiberium drawn three pixels too high", "bridgegap &middot; Red Alert 2 and Yuri's Revenge",
        "<code>Drawable.LoadFromRules</code> lifts overlays half a tile when they declare "
        "<code>Land=Rock</code> or <code>Land=Road</code>. <span class='mono'>TIB01</span> and "
        "<span class='mono'>GEM01</span> declare no <code>Land=</code> at all, because tiberium inherits the "
        "land type of the tile beneath it, so it fell through to an offset of zero. The correct value turned "
        "out to be three pixels rather than fifteen: the engine draws tiberium in a pass of its own with an "
        "anchor of its own. Measured at <span class='mono'>dx=0, dy=-3</span> on four theatres, three tiberium "
        "types and each of the twelve per-cell pool images."),
    "gems": ("defect", "Gems carried the same offset", "hillbtwn &middot; Cruentus",
        "Stated separately because it was a real question rather than an assumption: gems are a different "
        "tiberium type, a different pool of twelve images, a different art file and a different palette. They "
        "came out at exactly the same three pixels."),
    "ramp": ("defect", "Objects on sloped cells stood on the wrong height", "fourcorners &middot; both engines",
        "A ramp cell slopes across its own width, so its stored height describes only one corner. "
        "<code>CellClass::Get_Height</code> samples the surface at the object's own position; evaluated at a "
        "cell centre the game's ramp table gives half a level for ramps 1-4, a full level for 9-16 and nothing "
        "for 0 and 5-8. Measuring every terrain object individually, keyed by its cell's ramp and height: 12 of "
        "1719 sat 7 or 15 pixels low before, and <strong>1719 of 1719 are exact now</strong>."),
    "tsfudge": ("defect", "YDrawFudge was never read", "tiers &middot; Tiberian Sun only",
        "<code>TerrainClass::Draw_It</code> adds this rules value straight to the draw point. Only the Tiberian "
        "Sun blossom-tree types set it &mdash; <span class='mono'>FONA01..15</span>, at "
        "<span class='mono'>-12</span>, half a TS tile &mdash; and Red Alert 2 defines it nowhere, which is why "
        "it had gone unnoticed."),
    "tibtre": ("defect", "Blossom trees were drawn with no lighting at all", "xmp22s8 &middot; TIBTRE",
        "<code>TerrainClass::Draw_It</code> sends tiberium-spawning terrain through "
        "<span class='mono'>TiberiumDrawer</span>, which the engine aliases to "
        "<span class='mono'>VoxelDrawer</span> &mdash; the unit palette, which we already had right &mdash; "
        "tinted with the cell's <span class='mono'>Brightness</span>. We had "
        "<code>LightingType.None</code>, so these trees ignored ambient entirely and read far too bright on "
        "any dim map. The old code carried a <span class='mono'>// todo: verify it's not NONE</span>."),
    "quant": ("choice", "Lighting quantized to the engine's 63 steps", "fourcorners &middot; both engines",
        "The engine does not scale colours by the light level; it picks one of 63 steps in a "
        "<span class='mono'>LightConvertClass</span> table. Found on Tiberian Sun by sweeping a map's ambient "
        "from 0.30 to 1.00 and reading the recovered scale &mdash; it landed on exact integers over 31. Red "
        "Alert 2 inherits the same machinery, which showed up because <span class='mono'>xhailmary</span>, the "
        "one corpus map whose lighting works out to exactly 1.0, was already perfect while every map below 1.0 "
        "read 1.4 to 3.2 percent bright. <strong>This is decision 1 below.</strong>"),
    "shadow": ("defect", "Anything on a slope sat behind its own ground", "fourcorners &middot; both engines",
        "The ramp fix above moved a shape up the slope but carried its depth up with it, so a shape "
        "standing on a ramp measured a lift further away than the ground it stands on and lost the "
        "z test against it &mdash; its shadow first, and its own upper rows wherever the ground won. "
        "The game's <code>ZAdjust</code> is <span class='mono'>-Z_Lepton_To_Pixel(Position.Z)</span>, "
        "an absolute height that already contains the ramp surface, so the lift is a screen offset "
        "and never a depth one. This tree stands on a ramp-12 cell, half a level above its stored "
        "corner. Counting the darkened pixels of one tree's shadow elsewhere on the map: the engine "
        "casts 226, we cast 124 of them before and <strong>all 226 now</strong>."),
    "lamp": ("defect", "Light posts were never drawn", "springs &middot; Tiberian Sun",
        "A hardcoded list of 25 lamp names forced every one of them invisible. Both games actually "
        "split their lamps in two, and say so in their own rules: the <span class='mono'>IN*</span> "
        "and theatre-named ones declare <code>InvisibleInGame</code> and exist only to light their "
        "surroundings, while <span class='mono'>REDLAMP</span>, <span class='mono'>GALITE</span>, "
        "<span class='mono'>TSTLAMP</span> and four others are real light posts the game draws from "
        "<span class='mono'>GALITE</span> art. Seven visible types were being hidden; reading the "
        "rules key alone is both simpler and right."),
    "alphaorder": ("defect", "AlphaImage glow lit only what came before it", "springs &middot; the truck under the lamp",
        "The engine keeps its alpha lighting in a buffer that every blitter reads, so a glow lights "
        "whatever ends up visible under it. We applied ours as a single multiply at the lamp's own "
        "draw time, which left every object drawn later &mdash; here the truck and the tank beside "
        "it &mdash; sitting unlit inside the glow. The glows now run as a pass after everything "
        "else. The truck's mean colour went from far too dark to "
        "<span class='mono'>127/143/151</span> against the engine's "
        "<span class='mono'>127/142/150</span>; what is left on it is the voxel shading below."),
    "bridge": ("defect", "Every span of a high bridge drew the same image", "bridgegap &middot; both engines",
        "A map stores one frame for a full bridge span &mdash; <span class='mono'>0</span> east-west, "
        "<span class='mono'>9</span> north-south &mdash; and <code>CellClass::Draw_Overlay</code> nudges it "
        "by 0 to 3 from a 4&times;4 table on the cell's own coordinates, so a long bridge does not repeat one "
        "image down its whole length. We drew the stored frame everywhere. All 66 spans on this map carry the "
        "same stored value, which is why the deck reads as the right bridge with the wrong texture. Measured "
        "over the pixels the bridge draws: <strong>21.3% differed before and 0.11% after</strong>."),
    "oreshadow": ("defect", "Ore cast the wrong shadow", "dunepatr &middot; both engines",
        "The crystals were already right &mdash; the pooled per-cell image agrees with the engine on "
        "98.4% of cells and the growth frame on 96.5% &mdash; but splitting the field by our own shadow "
        "mask put 91% of its mismatch on the shadow pixels alone. Rendering the field twelve times, once "
        "per pool image, and matching each cell's engine shadow against all twelve settled it: "
        "<strong>372 of 372 cells matched image one</strong>, at a mean overlap of 0.73 against 0.14 for "
        "the runner-up. <code>CellClass::Draw_Overlay_Shadow</code> reads "
        "<span class='mono'>OverlayTypes[Overlay]</span> straight back, so tiberium casts the shadow of "
        "the id the map stored while its body comes from the pooled art. Ours cast the pooled one.</p>"
        "<p>What kept this hidden was <code>RecalculateOreSpread</code>, and it deserves its due: it had "
        "the right insight years ago &mdash; that the game redistributes a field across twelve images by "
        "cell position rather than drawing what the map stored &mdash; and without it an ore field draws "
        "one image throughout. It worked that out in <em>display</em> coordinates, though, which carry the "
        "map's width, so the same cell got a different image on a differently sized map: widths 84 and 85 "
        "agree on 7.8% of cells. It also rewrote the cell's overlay id to say so, which is what erased the "
        "id the shadow needed and sent the first attempt at this fix the wrong way. The engine never "
        "rewrites a cell; it picks the art at draw time. So does "
        "<code>Operations.ApplyTiberiumArt</code> now, late enough to see the cell's ramp and leaving the "
        "map's own data alone."),
    "quant2": ("choice", "The same change on a dim map", "all05s &middot; ambient 0.68",
        "The darker the map, the further the continuous value drifts from the step the engine would have "
        "picked. This map moved from 9.7% to 3.6% on the change alone."),
    "tsamb": ("choice", "Tiberian Sun, where it was found", "springs &middot; ambient 0.62",
        "The symptom that started it: our snow read too bright, worst at low ambient. The same map at ambient "
        "1.0 scored +0.9/+0.6/+0.5 and at 0.62 scored +5.4/+6.4/+6.7, which ruled out the palette and the "
        "theatre and left only the ambient response. Worst map in the TS corpus at 26.6%, now 4.7%."),
}


TAGS = {"defect": "defect", "choice": "model choice", "open": "still open"}


def card(name):
    kind, heading, where, prose = ENTRIES[name]
    meta = next(c for c in manifest if c["name"] == name)
    roles = meta.get("roles", ["engine", "before", "after"])[1:]
    buttons = "\n".join(
        f'    <button class="btn" data-mode="{r}" aria-pressed="false">Blink engine &harr; {r}</button>'
        for r in roles)
    if len(roles) == 1:
        hint = f"{meta['differ'][roles[0]]}% of this crop still differs"
    else:
        hint = f"{meta['differ']['before']}% &rarr; {meta['differ']['after']}% of this crop"
    return f"""<div class="card" data-crop="{name}">
  <div class="card-head">
    <h3>{heading}</h3>
    <span class="tag {kind}">{TAGS[kind]}</span>
    <span class="where">{where}</span>
  </div>
  <p>{prose}</p>
  <div class="panels"></div>
  <div class="ctl">
    <button class="btn" data-mode="side" aria-pressed="true">Side by side</button>
{buttons}
    <button class="btn" data-step hidden>Step</button>
    <span class="hint">{hint}</span>
  </div>
</div>"""


geometry = "\n".join(card(n) for n in ("ore", "gems", "ramp", "shadow", "bridge", "tsfudge", "lamp"))
lighting = "\n".join(card(n) for n in ("tibtre", "alphaorder", "tsamb", "quant", "quant2"))
still_open = card("oreshadow")

rows = []
for r in table:
    gained = r["v300"] - r["now"]
    rows.append(f'<tr><td class="name">{html.escape(r["map"])}</td>'
                f'<td class="n">{r["v300"]:.2f}%</td><td class="n">{r["now"]:.2f}%</td>'
                f'<td class="n good">&minus;{gained:.2f}</td></tr>')
mean_a = sum(r["v300"] for r in table) / len(table)
mean_b = sum(r["now"] for r in table) / len(table)
rows.append(f'<tr class="total"><td class="name">mean</td><td class="n">{mean_a:.2f}%</td>'
            f'<td class="n">{mean_b:.2f}%</td><td class="n good">&minus;{mean_a-mean_b:.2f}</td></tr>')

page = page.replace("{{LEDGER_GEOMETRY}}", geometry)
page = page.replace("{{LEDGER_LIGHTING}}", lighting)
page = page.replace("{{LEDGER_OPEN}}", still_open)
page = page.replace("{{TABLE}}", "\n".join(rows))
page = page.replace("{{IMAGES}}", images.replace("</", "<\\/"))
page = page.replace("{{MANIFEST}}", json.dumps(manifest).replace("</", "<\\/"))

with open(out_path, "w", encoding="utf-8") as fh:
    fh.write(page)
print(f"wrote {out_path}  ({len(page)/1024/1024:.2f} MB, {len(manifest)} comparisons)")
