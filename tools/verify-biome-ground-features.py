"""Low scenery must stay walk-through, grounded, inexpensive and visibly distinct."""
import json, hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]
paths=sorted((root/'assets/models/runtime').glob('underworld-ground-*.model.json'))
assert len(paths)==10, 'Expected ten biome ground features'
seen=set()
for path in paths:
 d=json.loads(path.read_text()); parts=d['parts']
 points=[v for p in parts for v in p['vertices']]
 lo=[min(p[i] for p in points) for i in range(3)]; hi=[max(p[i] for p in points) for i in range(3)]
 extent=[hi[i]-lo[i] for i in range(3)]
 triangles=sum(len(p['triangles'])//3 for p in parts)
 assert 200<=triangles<=8500,(path.name,triangles)
 assert -.3<=lo[1]<=0 and .1<hi[1]<1,(path.name,lo,hi)
 assert 2.5<max(extent[0],extent[2])<6,(path.name,extent)
 assert not d['lights'] and not any(p['collider'] for p in parts),path.name
 signature=hashlib.sha256(repr(sorted({tuple(round((p[i]-lo[i])/extent[i],4) for i in range(3)) for p in points})).encode()).hexdigest()
 assert signature not in seen, 'Duplicate/scale-only ground geometry: '+path.name
 seen.add(signature)
 assert all(p['material'].get('texture') for p in parts), 'Missing authored atlas: '+path.name
 print(f'PASS {path.stem}: {triangles} triangles, {extent[1]:.2f}m tall, grounded and nonblocking')
print('PASS: ten distinct authored biome ground features; no lights/collision and bounded mesh budgets.')
