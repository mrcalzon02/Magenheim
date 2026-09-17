"""Gate weapon surface semantics.

The retro-texture pass assigned surface families to the crystal weapons essentially at
random: 76 of 79 weapon materials disagreed with their own declared intent. A sword grip was
textured as stone, its crystal as metal, and a greatsword's blackmetal as carapace, on five
shared 256px maps.

Material names are spelled `magenheim.crystal-weapon.<model>.<intent>[.<n>].<family>`, and
the family token is load-bearing twice over: it selects the packed albedo, and
GeneratedSurfaceTextures.Classify reads it to pick the runtime fallback surface. A material
ending `.stone` is classified Stone whatever it was meant to be.

This gate keeps intent and family in agreement and holds weapon albedo at the 512px standard
the family was re-authored to. Exits non-zero so build.ps1 fails the build.
"""
import json
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'assets/models/runtime'
TEXTURES = ROOT / 'assets/models/textures'
WEAPON_ALBEDO_PX = 512

# Intent -> the family tokens that legitimately express it. The tokens are also chosen so
# GeneratedSurfaceTextures.Classify lands on the matching SurfaceKind.
ALLOWED = {
    'grip': {'leather', 'timber'},
    'blackmetal': {'metal'},
    'silver': {'silver'},
    'crystal': {'crystal'},
    'crystal-bright': {'crystal'},
    'rainbow': {'crystal'},
}

failures: list[str] = []
models = sorted(RUNTIME.glob('crystal-weapon-*.model.json'))
if len(models) != 10:
    failures.append(f'Expected 10 crystal weapon models, found {len(models)}.')

parts = 0
for path in models:
    document = json.loads(path.read_text())
    for part in document['parts']:
        parts += 1
        material = part.get('material') or {}
        name = material.get('name') or ''
        tokens = name.split('.')
        if len(tokens) < 5 or not name.startswith('magenheim.crystal-weapon.'):
            failures.append(f'{path.name}: material name is not a weapon identity: {name!r}')
            continue
        intent, family = tokens[3], tokens[-1]
        if intent not in ALLOWED:
            failures.append(f'{path.name}: unknown weapon surface intent {intent!r} in {name}')
            continue
        if family not in ALLOWED[intent]:
            failures.append(
                f'{path.name}: {name} declares intent {intent!r} but carries family '
                f'{family!r}; expected one of {sorted(ALLOWED[intent])}')
        texture = material.get('texture')
        if not texture:
            failures.append(f'{path.name}: {name} has no albedo map')
            continue
        file = TEXTURES / texture
        if not file.is_file():
            failures.append(f'{path.name}: {name} references missing texture {texture}')
            continue
        data = file.read_bytes()
        if data[:8] != b'\x89PNG\r\n\x1a\n':
            failures.append(f'{texture}: not a PNG')
            continue
        width, height = struct.unpack('>II', data[16:24])
        if width != WEAPON_ALBEDO_PX or height != WEAPON_ALBEDO_PX:
            failures.append(
                f'{path.name}: {name} albedo {texture} is {width}x{height}, '
                f'expected {WEAPON_ALBEDO_PX}x{WEAPON_ALBEDO_PX}')

if failures:
    print('FAIL: weapon material verification', file=sys.stderr)
    for failure in failures:
        print('  - ' + failure, file=sys.stderr)
    sys.exit(1)

print(f'PASS: {parts} weapon parts across {len(models)} weapons carry a family matching their '
      f'declared intent on {WEAPON_ALBEDO_PX}px authored albedo.')
