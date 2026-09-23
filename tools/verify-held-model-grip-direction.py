#!/usr/bin/env python3
"""Reject long-held crystal assets whose working end points into the player's grip.

Orientation is two separate contracts. `verify-held-model-orientation.py` establishes that the
long axis is Y. This gate establishes direction along that axis. Magenheim's canonical weapon
sources place the grip/pommel at negative Y and the blade/head/working end toward positive Y.
A sideways-axis repair can satisfy the first contract while still leaving an asset reversed.

The check is deliberately source-independent and runs over exported runtime JSON. It weighs each
side of the hand by authored TRIANGLE AREA times distance from the attach origin (see the
2026-09-22 note below for why this replaced plain area in end slices), not by vertex count and not
by bounding volume: a normal weapon/staff has the compact grip at -Y and more material, further
out, at +Y. Assets whose negative side is materially heavier are rejected. Ambiguous near-symmetric assets are reported separately and require visual acceptance
rather than an automatic flip.

Two prior metrics were tried and measurably failed before this one. Vertex count called the
crystal-weapon-greatsword reversed: its pommel decoration ("tip-core") is small but modelled with
648 vertices, more than the blade's 576+732, though a render confirmed 2026-09-19 that the blade is
plainly the dominant end. Bounding-box volume then called the greatsword's blade "light" for the
opposite reason -- the user had just made it flatter, and volume penalises thinness -- while ALSO
flipping four previously-correct staff tiers (Fire/Storm/Radiance/Venom Crystal) from pass to fail,
because a small chunky crystal ornament can out-volume a long thin shaft the same way it
out-vertexed one. Surface area avoids both failure modes: a thin blade's two broad faces still
carry real area, and a small ornament's facets stay small regardless of how finely they are
tessellated. It also matches the visual quantity a player actually reads as "size" better than a
count of internal geometry or a solid-body measurement no rendered surface represents.

That same corrected metric then caught a second, real, unrelated defect: Magenheim_Staff_Fire_Crystal,
Magenheim_Staff_Storm_Crystal, staff-radiance-crystal and staff-venom-crystal each had their entire
ornate head authored at the POMMEL end and the plain pommel cap authored at the WORKING end -- built
back-to-front, confirmed independently by their rendered icons (staff-*-crystal.icon.png showed the
head pointing toward the grip) and by each source's own Blender Z coordinates (pommel at positive Z,
head parts at negative Z, the exact mirror of every correctly-built staff). Fixed 2026-09-19 by
rotating each of the four sources 180 degrees about their shared world origin and re-exporting; no
longer excluded below.

2026-09-22: end-slice area was replaced by the FIRST MOMENT of surface area about the attach
origin -- each triangle's area times its distance from the hand, summed on each side. The slice
metric failed the re-authored sword and greatsword, both confirmed blade-at-+Y by render and by
their own coordinates: a properly tapered blade puts little area in its last 22%, while a wrapped
grip and pommel put a lot in the first. It had also never been decisive on the defect it exists
for -- the reversed sword it was introduced beside measured 0.110 vs 0.088 m2, under the 1.30 fail
ratio. Measured over every held asset (tools/ModelAssetTests replays alignment separately):

    metric                   40 correct assets (min pos/neg)   5 known-reversed (max pos/neg)
    end-slice area           0.70 (fails the new sword)        0.80 (reversed sword not failed)
    area moment about hand   1.47 (staff-spirit-simple)        0.28 (all five fail)

The five known-reversed payloads are the four Crystal staves at ee4e765^ and the sword at bc29d5f^.
Reach alone (which end extends further) was also measured and rejected: the balanced battleaxe and
the spirit staves reach slightly further toward the grip while being correct.
"""
from pathlib import Path
import json
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "assets" / "models" / "runtime"
REVERSE_RATIO = 1.30
NAMES = (
    "crystal-weapon-sword", "crystal-weapon-greatsword", "crystal-weapon-axe",
    "crystal-weapon-battleaxe", "crystal-weapon-mace", "crystal-weapon-spear",
    "crystal-weapon-knife", "crystal-weapon-atgeir",
)

# All four assets this gate once excluded (Fire/Storm/Radiance/Venom Crystal) were fixed
# 2026-09-19 -- see the module docstring -- and confirmed by render. Nothing is excluded now, but
# the mechanism stays: a future genuine defect this gate finds gets tracked here by name, in
# PROJECT_STATE.md, and removed only after a visual confirmation, not by loosening the metric.
KNOWN_REVERSED_PENDING_REVIEW: frozenset[str] = frozenset()


def part_points(part):
    """A part's own vertices as (x, y, z) tuples, regardless of the flat-or-nested JSON shape."""
    raw = part.get("vertices", [])
    if raw and isinstance(raw[0], list):
        return [tuple(v) for v in raw]
    return [tuple(raw[i:i + 3]) for i in range(0, len(raw), 3) if i + 2 < len(raw)]


def part_triangles(part):
    """A part's authored triangles as ((x,y,z), (x,y,z), (x,y,z)) point triples."""
    points = part_points(part)
    indices = part.get("triangles", [])
    for i in range(0, len(indices) - 2, 3):
        a, b, c = indices[i], indices[i + 1], indices[i + 2]
        if a < len(points) and b < len(points) and c < len(points):
            yield points[a], points[b], points[c]


def triangle_area(a, b, c):
    ux, uy, uz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    vx, vy, vz = c[0] - a[0], c[1] - a[1], c[2] - a[2]
    cx, cy, cz = uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx
    return 0.5 * (cx * cx + cy * cy + cz * cz) ** 0.5


def vertices(payload):
    for part in payload.get("parts", []):
        yield from part_points(part)


def triangles(payload):
    for part in payload.get("parts", []):
        yield from part_triangles(part)


def inspect(path):
    payload = json.loads(path.read_text(encoding="utf-8"))
    tris = list(triangles(payload))
    if not tris:
        return "FAIL", "no triangles"
    ys = [p[1] for tri in tris for p in tri]
    if max(ys) - min(ys) <= 1e-6:
        return "FAIL", "zero Y span"
    # First moment of surface area about the hand: each triangle weighted by how far it sits from
    # the attach origin, summed separately on each side of it.
    negative = positive = 0.0
    for a, b, c in tris:
        centroid_y = (a[1] + b[1] + c[1]) / 3.0
        moment = triangle_area(a, b, c) * abs(centroid_y)
        if centroid_y < 0.0:
            negative += moment
        else:
            positive += moment
    if negative > positive * REVERSE_RATIO:
        return "FAIL", "negative-Y end heavier (%.4f vs %.4f m3)" % (negative, positive)
    if positive <= negative * REVERSE_RATIO:
        return "AMBIGUOUS", "end mass near-symmetric (%.4f vs %.4f m3)" % (negative, positive)
    return "PASS", "grip/working-end moment %.4f/%.4f m3" % (negative, positive)


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
        if state == "FAIL" and path.stem in KNOWN_REVERSED_PENDING_REVIEW:
            print("%-9s %-42s %s (KNOWN, tracked in PROJECT_STATE.md)" % (state, path.stem, detail))
            continue
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
