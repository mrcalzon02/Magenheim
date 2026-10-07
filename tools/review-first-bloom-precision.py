"""Matched review plus source-topology and weight parity for precision pass."""
from pathlib import Path
import bpy,json,hashlib
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-precision-r4'
def signature(path):
 bpy.ops.wm.open_mainfile(filepath=str(path))
 o=next(o for o in bpy.context.scene.objects if o.type=='MESH')
 groups={g.index:g.name for g in o.vertex_groups}
 return dict(vertices=len(o.data.vertices),edges=len(o.data.edges),polygons=len(o.data.polygons),faces=[list(p.vertices) for p in o.data.polygons],weights=[sorted((groups[w.group],round(w.weight,6)) for w in v.groups) for v in o.data.vertices])
old=signature(ROOT/'assets/models/studies/underworld-boss-first-bloom-concept-r3.blend')
new=signature(ROOT/'assets/models/studies/underworld-boss-first-bloom-precision-r4.blend')
assert old==new,'precision pass changed topology or weights'
(OUT/'precision-parity.json').write_text(json.dumps(dict(result='PASS identical source topology and vertex weights',vertices=new['vertices'],edges=new['edges'],polygons=new['polygons'],source_faces_and_weights_sha256=hashlib.sha256(json.dumps(new,sort_keys=True).encode()).hexdigest()),indent=2)+'\n')
source=(ROOT/'tools/review-first-bloom-concept.py').read_text().replace('first-bloom-concept-r3','first-bloom-precision-r4')
exec(compile(source,str(ROOT/'tools/review-first-bloom-concept.py'),'exec'),{'__file__':str(ROOT/'tools/review-first-bloom-concept.py'),'__name__':'__main__'})
print('VERIFIED unchanged precision topology and weights',flush=True)
