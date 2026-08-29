"""Inject the image data URIs and the sample manifest into the ore viewer template."""

from __future__ import annotations

import json
import sys

template_path, images_path, manifest_path, out_path = sys.argv[1:5]

with open(template_path, encoding="utf-8") as fh:
    html = fh.read()
with open(images_path, encoding="utf-8") as fh:
    images = fh.read()
with open(manifest_path, encoding="utf-8") as fh:
    manifest = json.load(fh)

# The JSON goes inside a <script> block, so a literal "</script>" in the data would end it early.
# Data URIs cannot contain one, but escaping the slash costs nothing.
html = html.replace("{{IMAGES}}", images.replace("</", "<\\/"))
html = html.replace("{{MANIFEST}}", json.dumps(manifest).replace("</", "<\\/"))

with open(out_path, "w", encoding="utf-8") as fh:
    fh.write(html)

print(f"wrote {out_path}  ({len(html) / 1024 / 1024:.2f} MB, {len(manifest)} samples)")
