"""Substitute {{figure}} placeholders in an HTML template with base64 data URIs.

Artifacts load nothing from an external host, so every image has to be inlined; keeping the template
readable means keeping the ~700 KB of base64 out of it until the last step.
"""

from __future__ import annotations

import json
import re
import sys

template_path, figures_path, out_path = sys.argv[1:4]

with open(figures_path) as fh:
    figures = json.load(fh)
with open(template_path, encoding="utf-8") as fh:
    html = fh.read()

missing = set(re.findall(r"\{\{(\w+)\}\}", html)) - set(figures)
if missing:
    raise SystemExit(f"template needs figures that were not built: {sorted(missing)}")

for name, uri in figures.items():
    html = html.replace("{{" + name + "}}", uri)

with open(out_path, "w", encoding="utf-8") as fh:
    fh.write(html)

print(f"wrote {out_path}  ({len(html) / 1024 / 1024:.2f} MB)")
