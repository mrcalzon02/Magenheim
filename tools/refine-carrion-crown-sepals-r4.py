"""Build the previously unregenerated sepal revision into a separate study."""
from pathlib import Path
import json,sys
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
import carrion_crown_anatomy
carrion_crown_anatomy.OUT=ROOT/'docs/art/creature-models/2026-10-25/carrion-crown-sepals-r4'
s=(ROOT/'tools/refine-carrion-crown-anatomy-r3.py').read_text().replace('carrion-crown-anatomy-r3','carrion-crown-sepals-r4').replace('2026-10-16','2026-10-25')
s=s.replace('export_animations=False,',"export_animations=False,export_vertex_color='NONE',")
exec(compile(s,str(__file__),'exec'))
p=OUT/'refinement-metrics.json';row=json.loads(p.read_text());row.pop('reduction_percent',None);row.update(previous_id='underworld-boss-carrion-crown-anatomy-r3',previous_triangles=2046,triangle_delta=80,status='built sepal candidate; visual and native acceptance separate');p.write_text(json.dumps(row,indent=2)+'\n')
