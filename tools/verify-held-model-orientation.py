#!/usr/bin/env python3
"""Validate canonical local-space orientation for held Magenheim weapon/staff models.

Valheim parents item visuals directly under the prefab `attach` transform.  The checked-in
Blender payload therefore has to carry a stable local convention; ModelAssets deliberately
applies no corrective rotation.  This gate catches sideways/upside-down exports before they
become hand-model and model-derived-icon regressions.

Convention for long held implements:
  * principal/long axis is local +Y;
  * the grip/pommel occupies the low-Y end and the working end the high-Y end;
  * X/Z remain the minor axes (bows/crossbows are exempt from the long-axis test).

This is intentionally a geometry gate, not a runtime rotation shim.  Fix failures in the
source .blend/export so inventory, dropped and held presentations share one authority.
"""
from __future__ import annotations

import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "assets" / "models" / "runtime"

LONG_HELD = (
    "crystal-weapon-sword",
    "crystal-weapon-greatsword",
    "crystal-weapon-axe",
    "crystal-weapon-battleaxe",
    "crystal-weapon-mace",
    "crystal-weapon-spear",
    "crystal-weapon-knife",
    "crystal-weapon-atgeir",
)

# Elemental staffs are deliberately discovered rather than duplicated here; every staff
# exported into the runtime library must obey the same attach-space convention.
STAFF_GLOBS = ("*staff*.model.json",)
MIN_Y_DOMINANCE = 1.15


def bounds(document: dict) -> tuple[list[float], list[float]]:
    points = []
    for part in document.get("parts", []):
        verts = part.get("vertices", [])
        if len(verts) % 3 == 0 and verts and isinstance(verts[0], (int, float)):
            points.extend(tuple(map(float, verts[i:i + 3])) for i in range(0, len(verts), 3))
        else:
            points.extend(tuple(map(float, v)) for v in verts)
    if not points:
        raise ValueError("model has no vertices")
    lo = [min(p[i] for p in points) for i in range(3)]
    hi = [max(p[i] for p in points) for i in range(3)]
    return lo, hi


def validate(path: Path) -> str | None:
    doc = json.loads(path.read_text(encoding="utf-8"))
    lo, hi = bounds(doc)
    span = [hi[i] - lo[i] for i in range(3)]
    minor = max(span[0], span[2], 1e-6)
    if span[1] < minor * MIN_Y_DOMINANCE:
        return "%s: local +Y is not dominant (span x/y/z %.3f/%.3f/%.3f)" % (
            path.stem.replace(".model", ""), span[0], span[1], span[2])
    if not all(math.isfinite(v) for v in lo + hi):
        return "%s: non-finite bounds" % path.name
    return None


def main() -> int:
    paths = {RUNTIME / (model + ".model.json") for model in LONG_HELD}
    for pattern in STAFF_GLOBS:
        paths.update(RUNTIME.glob(pattern))
    missing = sorted(str(p.relative_to(ROOT)) for p in paths if not p.exists())
    if missing:
        print("HELD MODEL ORIENTATION FAIL: missing runtime assets: " + ", ".join(missing))
        return 1
    failures = [failure for p in sorted(paths) if (failure := validate(p))]
    if failures:
        print("HELD MODEL ORIENTATION FAIL")
        for failure in failures:
            print("  " + failure)
        return 1
    print("HELD MODEL ORIENTATION PASS: %d long weapon/staff assets use canonical +Y attach-space" % len(paths))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
