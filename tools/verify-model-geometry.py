"""Fail the build when a model part is wound inside-out.

    python tools/verify-model-geometry.py [model-id ...]

Inverted winding has now shipped from three independent sources: the copied prism/cylinder
builders across eleven visual files, the geode cavity, and the geode interior crystals. It
produces no compile error, no exception and no log line. Nothing in the existing gates sees
it, because the mesh is valid in every respect except which way its faces point. It is only
discovered when a player looks at the wrong side of a surface and reports "transparency".

Deciding this needs care, because "normals point outward" is only meaningful for a closed
solid. An open sheet - a cavern floor, a vault, an agate collar - has no inside, and testing
its face normals against its own centroid yields noise. So:

  * vertices are welded by position first. The exporter writes unwelded vertices, so every
    directed edge is unique in the payload and topological tests are useless until this is
    undone. This is why the earlier ad-hoc pass had to discard its open-edge count.
  * a part whose welded surface is closed gets the decisive test: signed volume must be
    positive. A closed solid with negative signed volume is inside-out, with no ambiguity.
  * a part whose surface is open cannot be adjudicated this way. It is reported, never
    failed, unless it is overwhelmingly inward-facing, which is worth a human look.

Parts that are genuinely interior surfaces - the inside of a cavity, the underside of a
vault - are declared in INTERIOR_SURFACES with the reason. That list is intent, which
geometry cannot supply.

This does NOT duplicate the winding check in verify-model-assets.py, and neither should be
removed in favour of the other. That one compares each geometric face normal against the
authored vertex normals, which catches a face flipped out of step with its neighbours or
normals edited by hand, and stays valid on open and concave meshes. It cannot see a solid
that is uniformly inside-out, because the exporter derives the authored normals from the
same winding, so the two agree perfectly. Applied to the pre-rebuild geode - whose cavity
was 246 of 246 faces inverted - it reports zero disagreeing faces on every part. This gate
uses signed volume instead, which is exactly the case the other cannot reach, and is the
defect that has now shipped three times.
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'assets/models/runtime'

WELD = 4                     # decimal places used to weld coincident vertices
INWARD_WARN = 0.85           # open surfaces this strongly inward are worth reporting
# A shell with a mouth cut in it is still a shell. Demanding a perfectly closed manifold put
# the very defect this gate exists for into the "cannot judge" bucket: the geode cavity is
# 99% paired and the interior crystals 96%, and both were silently passed. Anything this
# close to sealed has a meaningful signed volume.
CLOSED_TOLERANCE = 0.10      # fraction of directed edges allowed to go unpaired

# Parts whose faces are meant to point into a void rather than out of a solid.
INTERIOR_SURFACES = {
    'GeodeCavity': 'inside of the geode shell; the player looks in at it through the mouth',
    'CavernVault': 'underside of a cavern roof',
    'CavernFloor': 'walkable top of a cavern floor',
    'CavernWalls': 'perimeter rock seen from inside the chamber',
    'CavernShaft': 'shaft walls seen from inside the shaft',
    'CavernSkyCap': 'cap closing a fissure, seen from below',
    'PassageShell': 'inside of a passage tunnel, walked through by the player',
}


def weld(vertices):
    """Map coincident positions onto one index so topology can be examined."""
    lookup, remap = {}, []
    for x, y, z in vertices:
        key = (round(x, WELD), round(y, WELD), round(z, WELD))
        if key not in lookup:
            lookup[key] = len(lookup)
        remap.append(lookup[key])
    return remap, len(lookup)


def analyse(part):
    vertices = part['vertices']
    triangles = part['triangles']
    remap, _ = weld(vertices)

    centre = [sum(v[k] for v in vertices) / len(vertices) for k in range(3)]
    directed = set()
    volume = 0.0
    inward = 0
    faces = 0

    for i in range(0, len(triangles), 3):
        ia, ib, ic = triangles[i], triangles[i + 1], triangles[i + 2]
        a, b, c = vertices[ia], vertices[ib], vertices[ic]

        ux, uy, uz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
        vx, vy, vz = c[0] - a[0], c[1] - a[1], c[2] - a[2]
        nx = uy * vz - uz * vy
        ny = uz * vx - ux * vz
        nz = ux * vy - uy * vx

        fx = (a[0] + b[0] + c[0]) / 3.0 - centre[0]
        fy = (a[1] + b[1] + c[1]) / 3.0 - centre[1]
        fz = (a[2] + b[2] + c[2]) / 3.0 - centre[2]
        if nx * fx + ny * fy + nz * fz <= 0.0:
            inward += 1
        faces += 1

        volume += (a[0] * (b[1] * c[2] - b[2] * c[1])
                   - a[1] * (b[0] * c[2] - b[2] * c[0])
                   + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6.0

        wa, wb, wc = remap[ia], remap[ib], remap[ic]
        for edge in ((wa, wb), (wb, wc), (wc, wa)):
            directed.add(edge)

    unpaired = sum(1 for e in directed if (e[1], e[0]) not in directed)
    openness = unpaired / float(len(directed)) if directed else 1.0
    closed = faces > 0 and openness <= CLOSED_TOLERANCE
    return closed, volume, inward, faces, openness


def verify(model_id):
    path = RUNTIME / (model_id + '.model.json')
    if not path.exists():
        return ['%s: no exported runtime payload' % model_id], []
    document = json.loads(path.read_text(encoding='utf-8'))

    failures, notes = [], []
    for part in document.get('parts', []):
        name = part['name']
        closed, volume, inward, faces, openness = analyse(part)
        if not faces:
            failures.append('%s/%s: no triangles' % (model_id, name))
            continue
        declared = name in INTERIOR_SURFACES

        if closed:
            if volume < 0.0 and not declared:
                failures.append(
                    '%s/%s: sealed surface (%.0f%% of edges paired) with negative signed '
                    'volume (%.5f); it is wound inside-out and will render see-through. '
                    '%d of %d faces point inward.'
                    % (model_id, name, 100.0 * (1.0 - openness), volume, inward, faces))
            elif volume > 0.0 and declared:
                failures.append(
                    '%s/%s: declared an interior surface but is a closed outward solid; '
                    'remove it from INTERIOR_SURFACES or fix the winding.'
                    % (model_id, name))
        else:
            ratio = inward / float(faces)
            if ratio >= INWARD_WARN and not declared:
                notes.append('%s/%s: open surface, %d of %d faces inward-facing'
                             % (model_id, name, inward, faces))
    return failures, notes


def main():
    targets = sys.argv[1:]
    if not targets:
        targets = sorted(p.name[:-11] for p in RUNTIME.glob('*.model.json'))

    all_failures, all_notes = [], []
    for model_id in targets:
        failures, notes = verify(model_id)
        all_failures.extend(failures)
        all_notes.extend(notes)

    if all_notes:
        print('Open surfaces worth a look (not failures, intent cannot be inferred):')
        for note in all_notes:
            print('  ' + note)
        print()

    if all_failures:
        print('Model geometry verification FAILED:')
        for failure in all_failures:
            print('  ' + failure)
        raise SystemExit(1)

    print('Verified winding on %d models; no closed part is inside-out.' % len(targets))


main()
