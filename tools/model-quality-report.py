"""One cheap call that measures the model library, so progress is not re-derived each time.

    python tools/model-quality-report.py [--csv]

Reports triangle budget and texture resolution by family against the library median, and
runs every model verifier that exists, including the ones not yet wired into build.ps1.
Read-only; it never writes an asset.
"""
import json
import struct
import subprocess
import sys
from collections import defaultdict
from pathlib import Path
from statistics import median

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'assets/models'

FAMILIES = ('crystal-weapon', 'staff', 'crystal-banner', 'furniture', 'geology-decor',
            'architecture', 'deep-fracture-creature', 'deep-fracture', 'underworld',
            'sentinel', 'crystal-bed', 'icebox', 'geode', 'earth-', 'dark-throne',
            'effect-', 'df-')

VERIFIERS = ('verify-model-assets', 'verify-model-geometry', 'verify-model-surface-continuity',
             'verify-held-model-orientation', 'verify-held-model-grip-direction',
             'verify-geode-topology', 'verify-geode-shell-topology',
             'verify-deep-fracture-caverns', 'verify-model-scale', 'verify-icon-assets',
             'verify-weapon-materials')


def family_of(model_id: str) -> str:
    lowered = model_id.lower()
    for key in FAMILIES:
        if key in lowered or lowered.startswith(key):
            return key
    return 'other'


def texture_widths(entry) -> set:
    document = json.loads((ASSETS / entry['runtime']).read_text())
    widths = set()
    for part in document['parts']:
        texture = (part.get('material') or {}).get('texture')
        if not texture:
            continue
        file = ASSETS / 'textures' / texture
        if file.is_file():
            widths.add(struct.unpack('>II', file.read_bytes()[16:24])[0])
    return widths


def main() -> None:
    catalog = json.loads((ASSETS / 'catalog.json').read_text())
    grouped = defaultdict(list)
    for entry in catalog:
        grouped[family_of(entry['id'])].append(entry)

    library_median = median([e['triangles'] for e in catalog])
    as_csv = '--csv' in sys.argv

    rows = []
    for name, entries in grouped.items():
        triangles = [e['triangles'] for e in entries]
        widths = set()
        for entry in entries[:6]:
            widths |= texture_widths(entry)
        single_part = sum(1 for e in entries if e['parts'] <= 1)
        rows.append((name, len(entries), median(triangles), min(triangles), max(triangles),
                     sorted(widths), single_part))
    rows.sort(key=lambda row: row[2])

    if as_csv:
        print('family,models,median_triangles,min,max,texture_px,single_part_models')
        for name, n, med, low, high, widths, single in rows:
            print(f'{name},{n},{med:.0f},{low},{high},"{widths}",{single}')
    else:
        print(f"{'family':24s} {'n':>4s} {'median':>8s} {'vs lib':>7s} {'min':>6s} {'max':>7s} "
              f"{'tex px':>12s} {'1-part':>7s}")
        for name, n, med, low, high, widths, single in rows:
            share = med / library_median if library_median else 0
            print(f'{name:24s} {n:4d} {med:8.0f} {share:6.0%} {low:6d} {high:7d} '
                  f'{str(widths):>12s} {single:7d}')
        print(f'\nlibrary: {len(catalog)} models, median {library_median:.0f} triangles, '
              f'{sum(e["triangles"] for e in catalog):,} total')

    print('\nverifiers:')
    failing = 0
    for name in VERIFIERS:
        script = ROOT / 'tools' / f'{name}.py'
        if not script.is_file():
            continue
        result = subprocess.run([sys.executable, str(script)], capture_output=True, text=True)
        status = 'pass' if result.returncode == 0 else 'FAIL'
        if result.returncode != 0:
            failing += 1
            tail = [ln for ln in (result.stdout + result.stderr).splitlines() if ln.strip()][-1:]
            print(f'  {status}  {name}: {tail[0].strip()[:110] if tail else ""}')
        else:
            print(f'  {status}  {name}')
    print(f'\n{failing} verifier(s) failing.')


main()
