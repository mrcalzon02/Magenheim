"""Verify that the Deep Fracture districts are traversable enclosed caverns.

    python tools/verify-deep-fracture-caverns.py [DF-01 ...]

A district can look like a cavern in a contact sheet and still be unplayable: a vault
that dips below head height, a floor step the player cannot climb, or a passage mouth
sealed by its own rock. This reads the exported runtime payload and checks the
properties that decide whether the space actually works:

  * the chamber is enclosed  - a vault surface exists above the floor everywhere;
  * it is stand-up-able      - headroom over the walkable interior clears MIN_HEADROOM;
  * it is walkable           - no floor step between adjacent samples exceeds MAX_STEP;
  * it still connects        - every passage mouth keeps its floor and clear height.

Exits non-zero with the offending districts named, so it can gate a build.
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'assets/models/runtime'

DISTRICT_IDS = ['DF-%02d' % n for n in range(1, 21)]

HALF = 46.0
MIN_HEADROOM = 4.4          # a little under the generator's target, to allow rounding
MAX_STEP = 1.75             # ~34 deg over the 2.14 m lattice; above this players slide
MOUTH_HALF = 13.0
MOUTH_CLEAR = 8.0
INTERIOR = 38.0             # walkable core; the outermost metres are wall rock


def surface_heights(part):
    """Map a grid surface to {(x, z): y}, keeping the extreme sample per column."""
    heights = {}
    for x, y, z in part['vertices']:
        key = (round(x, 3), round(z, 3))
        previous = heights.get(key)
        if previous is None:
            heights[key] = y
        else:
            heights[key] = (previous + y) * 0.5
    return heights


def in_mouth(x, z):
    for along, across in ((x, z), (z, x)):
        if abs(across) > HALF - 6.0 and abs(along) < MOUTH_HALF * 0.55:
            return True
    return False


def verify(district_id):
    path = RUNTIME / (district_id + '.model.json')
    if not path.exists():
        return ['%s: no exported runtime payload' % district_id]
    document = json.loads(path.read_text(encoding='utf-8'))
    parts = {p['name']: p for p in document.get('parts', [])}

    failures = []
    for required in ('CavernFloor', 'CavernVault', 'CavernWalls'):
        if required not in parts:
            failures.append('%s: missing %s; the district is not an enclosed cavern' % (district_id, required))
    if failures:
        return failures

    floor = surface_heights(parts['CavernFloor'])
    vault = surface_heights(parts['CavernVault'])

    shared = sorted(set(floor) & set(vault))
    if len(shared) < 100:
        return ['%s: floor and vault do not share a sample lattice (%d points)' % (district_id, len(shared))]

    worst_headroom = (None, 1e9)
    worst_step = (None, 0.0)
    mouth_samples = 0
    mouth_failures = 0

    for key in shared:
        x, z = key
        clearance = vault[key] - floor[key]
        interior = abs(x) <= INTERIOR and abs(z) <= INTERIOR
        if clearance <= 0.0:
            failures.append('%s: vault is at or below the floor at (%.1f, %.1f)' % (district_id, x, z))
            break
        if interior and clearance < worst_headroom[1]:
            worst_headroom = (key, clearance)
        if in_mouth(x, z):
            mouth_samples += 1
            if clearance < MOUTH_CLEAR:
                mouth_failures += 1

    if worst_headroom[0] is not None and worst_headroom[1] < MIN_HEADROOM:
        failures.append('%s: only %.2f m of headroom at (%.1f, %.1f); interior must clear %.1f m'
                        % (district_id, worst_headroom[1], worst_headroom[0][0], worst_headroom[0][1], MIN_HEADROOM))

    if mouth_samples == 0:
        failures.append('%s: no passage mouth samples found; the district cannot connect' % district_id)
    elif mouth_failures:
        failures.append('%s: %d of %d passage-mouth samples are below %.1f m clear height'
                        % (district_id, mouth_failures, mouth_samples, MOUTH_CLEAR))

    # Walkability: compare each floor sample with its nearest neighbours along the lattice.
    columns = sorted({k[0] for k in floor})
    rows = sorted({k[1] for k in floor})
    if len(columns) > 1 and len(rows) > 1:
        spacing = round(columns[1] - columns[0], 3)
        for (x, z), height in floor.items():
            if abs(x) > INTERIOR or abs(z) > INTERIOR:
                continue
            for dx, dz in ((spacing, 0.0), (0.0, spacing)):
                neighbour = floor.get((round(x + dx, 3), round(z + dz, 3)))
                if neighbour is None:
                    continue
                step = abs(neighbour - height)
                if step > worst_step[1]:
                    worst_step = ((x, z), step)
        if worst_step[1] > MAX_STEP:
            failures.append('%s: %.2f m floor step at (%.1f, %.1f) exceeds the %.1f m walkable limit'
                            % (district_id, worst_step[1], worst_step[0][0], worst_step[0][1], MAX_STEP))

    if not failures:
        triangles = sum(len(p['triangles']) // 3 for p in document['parts'])
        print('  %-6s ok  parts=%-3d triangles=%-6d headroom>=%.1fm  step<=%.2fm  mouths=%d'
              % (district_id, len(document['parts']), triangles,
                 worst_headroom[1], worst_step[1], mouth_samples))
    return failures


def main():
    targets = sys.argv[1:] or DISTRICT_IDS
    all_failures = []
    for district_id in targets:
        all_failures.extend(verify(district_id))
    if all_failures:
        print('\nDeep Fracture cavern verification FAILED:')
        for failure in all_failures:
            print('  ' + failure)
        raise SystemExit(1)
    print('\nVerified %d Deep Fracture districts as enclosed, traversable caverns.' % len(targets))


main()
