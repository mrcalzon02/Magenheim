"""Reject duplicate/scale-only canopy variants and broken gameplay asset contracts."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
runtime = root / 'assets/models/runtime'
total = 0
for family in ('glowcap', 'spirestalk', 'puffcap', 'tanglecap'):
    paths = sorted(runtime.glob(f'underworld-flora-fungal-{family}*.model.json'))
    assert len(paths) == 4, f'{family}: expected prototype plus three growth forms'
    silhouettes = set()
    for path in paths:
        data = json.loads(path.read_text())
        parts = data['parts']
        points = [v for part in parts for v in part['vertices']]
        low = [min(p[i] for p in points) for i in range(3)]
        high = [max(p[i] for p in points) for i in range(3)]
        extent = [high[i] - low[i] for i in range(3)]
        # Per-axis normalization makes pure stretching/translation collide as duplicates.
        normalized = sorted({tuple(round((p[i]-low[i])/extent[i], 4) for i in range(3)) for p in points})
        signature = hashlib.sha256(repr(normalized).encode()).hexdigest()
        assert signature not in silhouettes, f'{path.stem}: duplicated or merely stretched geometry'
        silhouettes.add(signature)
        triangles = sum(len(p['triangles'])//3 for p in parts)
        assert 500 <= triangles <= 20000, f'{path.stem}: triangle budget {triangles}'
        assert 2.5 <= extent[1] <= 15, f'{path.stem}: implausible canopy height {extent[1]}'
        assert -.5 <= low[1] <= .05, f'{path.stem}: floating/buried foot {low[1]}'
        colliders = [p for p in parts if p['collider']]
        assert colliders and all(p['name'] in ('stalk', 'fused-stalk', 'strands') for p in colliders), path.stem
        assert not data['lights'], f'{path.stem}: per-tree point lights are not allowed'
        total += 1
        print(f'PASS {path.stem}: {extent[1]:.2f}m, {triangles} triangles, distinct geometry and stem collision')
print(f'PASS: {total} canopy models across four families; unique growth forms, grounded collision and mesh budgets.')
