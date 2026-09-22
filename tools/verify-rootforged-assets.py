"""Rootforged catalog-to-model binding, dimensions, materials and icon regression gate."""
import json, math
from pathlib import Path
from PIL import Image
R=Path(__file__).resolve().parents[1]
pieces=json.loads((R/'default-data/foundation.json').read_text())['underworldArchitecture']['pieces']
assert len(pieces)==11
for e in pieces:
 mid='rootforged-'+e['Id'].split('.')[-1].replace('_','-')
 data=json.loads((R/'assets/models/runtime'/f'{mid}.model.json').read_text())
 assert len(data['parts'])>=5,(mid,'missing detail')
 assert any(p['collider'] for p in data['parts']),(mid,'no structural collision')
 points=[v for p in data['parts'] for v in p['vertices']]
 expected=[e['Dimensions'][k] for k in ['WidthMeters','HeightMeters','DepthMeters']]
 lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
 assert abs(lo[1])<.06,(mid,'ground contact',lo)
 for i,size in enumerate(expected):assert abs(hi[i]-lo[i]-size)<.09,(mid,'catalog scale mismatch',hi,lo,expected)
 for p in data['parts']:
  assert all(abs(c-1)<.001 for c in p['material']['color']),(mid,'texture would be tinted twice')
  assert p['material']['texture'],(mid,'missing authored texture')
 if e['Tier']==1:assert sum('iron-collar' in p['name'] for p in data['parts'])==3,(mid,'missing physical iron bands')
 if e['Kind']==5:
  # Open passage beneath the crown, not a solid slab across the walk-through area.
  assert not any(abs(v[0])<.35 and v[1]<expected[1]*.45 for v in points),(mid,'blocked arch opening')
 with Image.open(R/'assets/earth'/f'{mid}.icon.png') as im:assert im.size==(256,256) and im.mode=='RGBA'
print('PASS: 11 Rootforged catalog bindings, dimensions, ground contact, collision, open arches, physical iron collars, neutral texture tint and model icons.')
