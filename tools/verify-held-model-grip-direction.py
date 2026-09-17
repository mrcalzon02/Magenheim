#!/usr/bin/env python3
"""Reject long-held crystal assets whose working end points into the player's grip.

Orientation is two separate contracts. `verify-held-model-orientation.py` establishes that the
long axis is Y. This gate establishes direction along that axis. Magenheim's canonical weapon
sources place the grip/pommel at negative Y and the blade/head/working end toward positive Y.
A sideways-axis repair can satisfy the first contract while still leaving an asset reversed.

The check is deliberately source-independent and runs over exported runtime JSON. It estimates
cross-sectional mass in equal end slices: a normal weapon/staff has the compact grip at -Y and
more authored geometry at +Y. Assets whose negative end is materially heavier are rejected.
Ambiguous near-symmetric assets are reported separately and require visual acceptance rather
than an automatic flip.
"""
from pathlib import Path
import json
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "assets" / "models" / "runtime"
SLICE = 0.22
REVERSE_RATIO = 1.30
NAMES = (
    "crystal-weapon-sword", "crystal-weapon-greatsword", "crystal-weapon-axe",
    "crystal-weapon-battleaxe", "crystal-weapon-mace", "crystal-weapon-spear",
    "crystal-weapon-knife", "crystal-weapon-atgeir",
)


def vertices(payload):
    for part in payload.get("parts", []):
        raw = part.get("vertices", [])
        if raw and isinstance(raw[0], list):
            yield from raw
        else:
            for i in range(0, len(raw), 3):
                if i + 2 < len(raw):
                    yield raw[i:i + 3]


def inspect(path):
    payload = json.loads(path.read_text(encoding="utf-8"))
    points = list(vertices(payload))
    if not points:
        return "FAIL", "no vertices"
    ys = [float(v[1]) for v in points]
    lo, hi = min(ys), max(ys)
    span = hi - lo
    if span <= 1e-6:
        return "FAIL", "zero Y span"
    cut = span * SLICE
    negative = sum(1 for y in ys if y <= lo + cut)
    positive = sum(1 for y in ys if y >= hi - cut)
    if negative > positive * REVERSE_RATIO:
        return "FAIL", "negative-Y end heavier (%d vs %d vertices)" % (negative, positive)
    if positive <= negative * REVERSE_RATIO:
        return "AMBIGUOUS", "end mass near-symmetric (%d vs %d vertices)" % (negative, positive)
    return "PASS", "grip/working-end mass %d/%d" % (negative, positive)


def main():
    paths = [RUNTIME / (name + ".model.json") for name in NAMES]
    paths += sorted(p for p in RUNTIME.glob("*staff*.model.json") if p not in paths)
    missing = [p for p in paths if not p.exists()]
    if missing:
        print("FAIL missing runtime held assets: " + ", ".join(str(p.relative_to(ROOT)) for p in missing))
        return 1
    failed = False
    ambiguous = False
    for path in paths:
        state, detail = inspect(path)
        print("%-9s %-42s %s" % (state, path.stem, detail))
        failed |= state == "FAIL"
        ambiguous |= state == "AMBIGUOUS"
    if failed:
        print("HELD GRIP-DIRECTION GATE: FAIL")
        return 1
    if ambiguous:
        print("HELD GRIP-DIRECTION GATE: PASS WITH VISUAL-ACCEPTANCE REQUIRED")
    else:
        print("HELD GRIP-DIRECTION GATE: PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
