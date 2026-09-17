"""Fail when the exported geode contains literal exterior holes.

    python tools/verify-geode-topology.py

The decorative GeodeShell is intentionally split into displaced fracture plates, so it is not
itself a manifold. GeodeCore is the closure authority behind those grooves. Its only legal open
boundary is the deliberate crystal cutaway mouth. This verifier operates on the exported runtime
payload, after Blender/export, so a source repair that was never baked cannot accidentally pass.
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
    lookup = {}
    welded = []
    remap = []
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
    core = parts.get("GeodeCore")
    if core is None:
        raise SystemExit("Geode topology FAILED: exported payload has no GeodeCore closure authority")

    vertices, remap = weld(core["vertices"])
    edge_use = Counter()
    triangles = core["triangles"]
    for i in range(0, len(triangles), 3):
        a, b, c = (remap[triangles[i]], remap[triangles[i + 1]], remap[triangles[i + 2]])
        for x, y in ((a, b), (b, c), (c, a)):
            edge_use[tuple(sorted((x, y)))] += 1

    nonmanifold = [edge for edge, count in edge_use.items() if count > 2]
    if nonmanifold:
        raise SystemExit("Geode topology FAILED: GeodeCore has %d non-manifold edges" % len(nonmanifold))

    cut = normalise(to_export_space(CUT_DIRECTION))
    boundaries = [edge for edge, count in edge_use.items() if count == 1]
    if not boundaries:
        raise SystemExit("Geode topology FAILED: intentional crystal mouth is sealed")

    illegal = []
    for edge in boundaries:
        for index in edge:
            if dot(normalise(vertices[index]), cut) < OPENING_COS - MOUTH_MARGIN:
                illegal.append(edge)
                break
    if illegal:
        raise SystemExit(
            "Geode topology FAILED: %d boundary edges lie outside the intentional mouth; literal missing planes remain"
            % len(illegal))

    # A valid mouth is one connected boundary loop. Multiple loops mean islands/holes survived
    # even when each happened to lie on the cutaway side.
    adjacency = {}
    for a, b in boundaries:
        adjacency.setdefault(a, set()).add(b)
        adjacency.setdefault(b, set()).add(a)
    bad_degree = [v for v, neighbours in adjacency.items() if len(neighbours) != 2]
    if bad_degree:
        raise SystemExit("Geode topology FAILED: mouth boundary branches at %d vertices" % len(bad_degree))
    seen = set()
    components = 0
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
        raise SystemExit("Geode topology FAILED: expected one intentional mouth loop, found %d boundary loops" % components)

    print("Geode topology verified: one intentional mouth loop (%d edges), zero exterior holes, zero non-manifold edges." % len(boundaries))


main()
