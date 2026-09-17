"""Gate the world scale of held equipment.

Nothing has ever checked model scale. The crystal weapon family shipped oversized:
crystal-weapon-knife measured 1.25m on its longest axis -- longer than a vanilla sword --
and crystal-weapon-sword measured 2.09m, longer than a vanilla greatsword. Scale produces
no compile error, no exception and no log line; it is only visible in a player's hand,
which is why it survived every other gate.

The staff family is the reference: it was observed in play to read correctly and measures
1.80-2.96m. The weapon targets below are the measured sizes corrected by the factor
observed on the sword, which was about a third longer than it needed to be.

Targets are the longest axis in metres, against a Valheim player of roughly 1.8m.
Exits non-zero on drift so build.ps1 fails the build.
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'assets/models/runtime'

# Longest-axis target in metres, and the tolerance allowed around it.
WEAPON_TARGETS = {
    'crystal-weapon-knife': 0.94,
    'crystal-weapon-axe': 1.17,
    'crystal-weapon-mace': 1.30,
    'crystal-weapon-crossbow': 1.32,
    'crystal-weapon-battleaxe': 1.56,
    'crystal-weapon-sword': 1.57,
    'crystal-weapon-bow': 1.84,
    'crystal-weapon-spear': 1.99,
    'crystal-weapon-greatsword': 2.08,
    'crystal-weapon-atgeir': 2.27,
}
WEAPON_TOLERANCE = 0.06

# The staff family is the in-play reference and must stay inside the band it already
# occupies; a staff is a two-handed pole and is expected to be longer than a sword.
STAFF_MINIMUM = 1.70
STAFF_MAXIMUM = 3.05

failures: list[str] = []


def longest_axis(model_id: str) -> float:
    path = RUNTIME / f'{model_id}.model.json'
    if not path.is_file():
        failures.append(f'{model_id}: no runtime model at {path.relative_to(ROOT)}')
        return -1.0
    document = json.loads(path.read_text())
    low = [float('inf')] * 3
    high = [float('-inf')] * 3
    for part in document['parts']:
        for vertex in part['vertices']:
            for axis in range(3):
                low[axis] = min(low[axis], vertex[axis])
                high[axis] = max(high[axis], vertex[axis])
    return max(high[axis] - low[axis] for axis in range(3))


for model_id, target in sorted(WEAPON_TARGETS.items()):
    measured = longest_axis(model_id)
    if measured < 0:
        continue
    if abs(measured - target) > WEAPON_TOLERANCE:
        failures.append(
            f'{model_id}: longest axis {measured:.2f}m, expected {target:.2f}m '
            f'+/-{WEAPON_TOLERANCE:.2f} (drift {measured - target:+.2f}m)')

staves = sorted(p.name[:-len('.model.json')] for p in RUNTIME.glob('*.model.json')
          if 'staff' in p.name.lower())
if len(staves) != 32:
    failures.append(f'Expected 32 staff models, found {len(staves)}.')
for model_id in staves:
    measured = longest_axis(model_id)
    if measured < 0:
        continue
    if not (STAFF_MINIMUM <= measured <= STAFF_MAXIMUM):
        failures.append(
            f'{model_id}: longest axis {measured:.2f}m is outside the '
            f'{STAFF_MINIMUM:.2f}-{STAFF_MAXIMUM:.2f}m staff reference band')

# A weapon must never out-reach the staff family's own maximum; that is the shape of the
# defect this gate exists to catch.
for model_id, target in WEAPON_TARGETS.items():
    if target > STAFF_MAXIMUM:
        failures.append(f'{model_id}: declared target {target:.2f}m exceeds the staff band.')

if failures:
    print('FAIL: model scale verification', file=sys.stderr)
    for failure in failures:
        print('  - ' + failure, file=sys.stderr)
    sys.exit(1)

print(f'PASS: {len(WEAPON_TARGETS)} weapons within {WEAPON_TOLERANCE:.2f}m of declared '
      f'world scale; {len(staves)} staves inside the {STAFF_MINIMUM:.2f}-{STAFF_MAXIMUM:.2f}m reference band.')
