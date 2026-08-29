"""Pixel comparison helpers for the engine-vs-renderer A/B campaign.

Two images only mean the same thing once they are in the same coordinate system, so everything
here works in *cell* space: both the capture and the render report a cell-to-pixel transform, and
alignment is arithmetic rather than a search.
"""

from __future__ import annotations

import json
import zipfile
from dataclasses import dataclass

import numpy as np
from PIL import Image


@dataclass(frozen=True)
class CellTransform:
    """Maps a map cell to a pixel in one image.

    The two producers use different but equivalent conventions, both affine with the same slope:
    the constant difference is absorbed here so callers work in one space.
    """

    off_x: int
    off_y: int
    tile_w: int
    tile_h: int

    def pixel(self, rx: int, ry: int, z: int = 0) -> tuple[int, int]:
        x = (rx - ry) * self.tile_w // 2 + self.off_x
        y = (rx + ry - z + 1) * self.tile_h // 2 + self.off_y
        return x, y


def capture_transform(meta: dict) -> CellTransform | None:
    ct = meta.get("cellTransform") or {}
    if not ct.get("hasTransform"):
        return None
    return CellTransform(ct["offsetX"], ct["offsetY"], ct["tileWidth"], ct["tileHeight"])


def render_transform(meta: dict) -> CellTransform:
    """The renderer reports a crop rectangle and the full map size instead of an offset.

    Both sides place a cell by its CENTRE. The renderer's surface formula is
    x = (Dx + 1) * tw/2 and y = (Dy - z + 1) * th/2, with Dx = rx - ry + width - 1 and
    Dy = rx + ry - width - 1; rewriting those in the capture's form leaves the constants below.
    Verified against the renderer's own startPositionPixels, which are authoritative.
    """
    tw = meta["tileWidth"]
    th = meta["tileHeight"]
    w = meta["fullSize"]["width"]
    sr = meta["saveRect"]
    return CellTransform(
        off_x=w * tw // 2 - sr["x"],
        off_y=-(w + 1) * th // 2 - sr["y"],
        tile_w=tw,
        tile_h=th,
    )


def read_capture(zip_path: str) -> tuple[dict, Image.Image | None]:
    """Metadata plus the stitched composite, read straight out of the capture zip."""
    with zipfile.ZipFile(zip_path) as z:
        meta = json.loads(z.read("metadata.json"))
    return meta, None


def load_png(path: str) -> np.ndarray:
    return np.asarray(Image.open(path).convert("RGB"), dtype=np.uint8)


def quantize_to_game(img: np.ndarray) -> np.ndarray:
    """Match the engine's output precision.

    The game's palettes are 6-bit scaled by 4, and its display surface is RGB565, so a render at
    full 8-bit precision reads a few percent brighter even when the lighting maths is identical.
    Comparing without this reports that systematic offset as a difference on every lit pixel.
    """
    out = img.astype(np.uint16)
    r = (out[..., 0] >> 3) << 3
    g = (out[..., 1] >> 2) << 2
    b = (out[..., 2] >> 3) << 3
    return np.dstack([r, g, b]).astype(np.uint8)


def align(a: np.ndarray, ta: CellTransform, b: np.ndarray, tb: CellTransform):
    """Crop both images to their common region in cell space.

    Returns (a_crop, b_crop, offset_in_a) where offset_in_a locates the crop inside `a`, so a
    finding can be reported back in the coordinates of the original image.
    """
    dx = ta.off_x - tb.off_x
    dy = ta.off_y - tb.off_y

    ax0 = max(0, dx)
    ay0 = max(0, dy)
    bx0 = max(0, -dx)
    by0 = max(0, -dy)

    w = min(a.shape[1] - ax0, b.shape[1] - bx0)
    h = min(a.shape[0] - ay0, b.shape[0] - by0)
    if w <= 0 or h <= 0:
        raise ValueError(f"images do not overlap (dx={dx}, dy={dy})")

    return (
        a[ay0 : ay0 + h, ax0 : ax0 + w],
        b[by0 : by0 + h, bx0 : bx0 + w],
        (ax0, ay0),
    )


def channel_delta(a: np.ndarray, b: np.ndarray) -> np.ndarray:
    """Per-pixel difference magnitude: the largest absolute channel difference."""
    return np.abs(a.astype(np.int16) - b.astype(np.int16)).max(axis=2)


def diff_mask(a: np.ndarray, b: np.ndarray, tolerance: int = 8) -> np.ndarray:
    """Pixels whose channels differ by more than `tolerance`.

    A tolerance rather than exact equality: the two pipelines round palette maths differently in
    the last bit or two, and counting that as a difference drowns real findings.
    """
    return channel_delta(a, b) > tolerance


def clusters(mask: np.ndarray, min_area: int = 20, values: np.ndarray | None = None):
    """Connected components of a boolean mask, largest first.

    Differences arrive as blobs -- one wrong sprite, one mis-lifted overlay -- so ranking blobs by
    area puts the biggest real defect on top instead of whichever row happened to be scanned first.

    `values` is an optional per-pixel delta magnitude of the same shape as the mask; when given,
    each cluster also reports mean/stdev/max of the deltas over its own pixels, which separates a
    blob that is barely off from one that is plain wrong.
    """
    from scipy import ndimage  # optional; only needed for clustering

    labels, count = ndimage.label(mask)
    if count == 0:
        return []
    flat = labels.ravel()
    areas = np.bincount(flat, minlength=count + 1)
    areas[0] = 0
    if values is not None:
        # Per-label sums in three passes over the image; a Python loop per cluster would rescan it
        # thousands of times on a multi-megapixel map. Label 0 is the background and is unused.
        v = values.ravel().astype(np.float64)
        sums = np.bincount(flat, weights=v, minlength=count + 1)
        sq = np.bincount(flat, weights=v * v, minlength=count + 1)
        maxima = np.zeros(count + 1)
        np.maximum.at(maxima, flat, v)
        with np.errstate(invalid="ignore", divide="ignore"):
            means = sums / areas
            variance = np.maximum(sq / areas - means * means, 0.0)
    # find_objects returns every label's bounding box in one pass. Scanning the whole image once
    # per label instead costs minutes on a multi-megapixel map with a thousand clusters.
    boxes = ndimage.find_objects(labels)
    out = []
    for lbl in np.argsort(areas)[::-1]:
        if areas[lbl] < min_area:
            break
        ys, xs = boxes[lbl - 1]
        zone = {
            "area": int(areas[lbl]),
            "x0": int(xs.start),
            "y0": int(ys.start),
            "x1": int(xs.stop) - 1,
            "y1": int(ys.stop) - 1,
        }
        if values is not None:
            zone["meanDelta"] = round(float(means[lbl]), 2)
            zone["stdevDelta"] = round(float(np.sqrt(variance[lbl])), 2)
            zone["maxDelta"] = round(float(maxima[lbl]), 2)
        out.append(zone)
    return out


def summarize(mask: np.ndarray, exclude: np.ndarray | None = None) -> dict:
    total = mask.size
    differing = int(mask.sum())
    result = {
        "pixels": int(total),
        "differing": differing,
        "percent": round(100.0 * differing / total, 4),
    }
    if exclude is not None:
        kept = mask & ~exclude
        result["excluded"] = int((mask & exclude).sum())
        result["differing_after_exclude"] = int(kept.sum())
        result["percent_after_exclude"] = round(100.0 * int(kept.sum()) / total, 4)
    return result
