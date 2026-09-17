"""Fail when the exported visible geode shell contains literal missing planes.

    python tools/verify-geode-shell-topology.py

GeodeShell is now authored by rebuild-geode-watertight-shell.py as one shared-vertex rock skin.
Unlike the historical decorative plate shell, it is therefore expected to be manifold everywhere
except the deliberate crystal mouth. This gate inspects the exported runtime payload so an old
binary export cannot pass merely because the Blender source repair exists.
"""
import json
import math
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODEL = ROOT / "assets/models/runtime/geode-sample.model.json"
WELD = 4
OPENING_COS = 0.52
CUT_DIRECTION = (1.0, 0.0, 0.34)
MOUTH_MARGIN = 0.18


def normalise(v):
    length = math.sqrt(sum(c * c for c in v)) or 1.0
    return tuple(c / length for c in v)


def dot(a, b):
    return sum(a[i] * b[i] for i in range(3))
# export-model-assets.py writes Blender coordinates as (x, z, -y). CUT_DIRECTION is authored
# in Blender space, the same literal the repair tools carve with, so it must be mapped the
# same way before it can be compared against exported vertices.
#
# Testing the unmapped direction compared the mouth against the wrong axis: it reported the
# geode as carrying 14 core and 198 shell boundary edges of "literal missing planes" while
# the mouth was in fact one clean loop. Mapped, every boundary vertex sits in a tight band
# (core 0.39-0.66, shell 0.45-0.58) around the cut, which is what a rim looks like.
def to_export_space(vector):
    return (vector[0], vector[2], -vector[1])


def weld(vertices):
    lookup, welded, remap = {}, [], []
    for vertex in vertices:
        key = tuple(round(c, WELD) for c in vertex)
        if key not in lookup:
            lookup[key] = len(welded)
            welded.append(vertex)
        remap.append(lookup[key])
    return welded, remap


def main():
    document = json.loads(MODEL.read_text(encoding="utf-8"))
    parts = {part["name"]: part for part in document.get("parts", [])}
    shell = parts.get("GeodeShell")
    if shell is None:
        raise SystemExit("Geode shell topology FAILED: exported payload has no GeodeShell")

    vertices, remap = weld(shell["vertices"])
    triangles = shell["triangles"]
    if len(triangles) % 3:
        raise SystemExit("Geode shell topology FAILED: triangle index count is not divisible by three")

    edge_use = Counter()
    for i in range(0, len(triangles), 3):
        a, b, c = remap[triangles[i]], remap[triangles[i + 1]], remap[triangles[i + 2]]
        if len({a, b, c}) != 3:
            raise SystemExit("Geode shell topology FAILED: degenerate triangle survives vertex welding")
        for x, y in ((a, b), (b, c), (c, a)):
            edge_use[tuple(sorted((x, y)))] += 1

    nonmanifold = [edge for edge, count in edge_use.items() if count > 2]
    if nonmanifold:
        raise SystemExit("Geode shell topology FAILED: %d non-manifold edges" % len(nonmanifold))

    boundaries = [edge for edge, count in edge_use.items() if count == 1]
    if not boundaries:
        raise SystemExit("Geode shell topology FAILED: deliberate crystal mouth is sealed")

    cut = normalise(to_export_space(CUT_DIRECTION))
    illegal = [edge for edge in boundaries if any(
        dot(normalise(vertices[index]), cut) < OPENING_COS - MOUTH_MARGIN for index in edge)]
    if illegal:
        raise SystemExit("Geode shell topology FAILED: %d visible boundary edges lie outside the crystal mouth; missing planes remain" % len(illegal))

    adjacency = {}
    for a, b in boundaries:
        adjacency.setdefault(a, set()).add(b)
        adjacency.setdefault(b, set()).add(a)
    bad_degree = [v for v, neighbours in adjacency.items() if len(neighbours) != 2]
    if bad_degree:
        raise SystemExit("Geode shell topology FAILED: mouth boundary branches at %d vertices" % len(bad_degree))

    seen, components = set(), 0
    for start in adjacency:
        if start in seen:
            continue
        components += 1
        stack = [start]
        while stack:
            current = stack.pop()
            if current in seen:
                continue
            seen.add(current)
            stack.extend(adjacency[current] - seen)
    if components != 1:
        raise SystemExit("Geode shell topology FAILED: expected one mouth loop, found %d" % components)

    print("Geode visible shell verified: one mouth loop (%d edges), zero exterior holes, zero non-manifold edges." % len(boundaries))


main()
