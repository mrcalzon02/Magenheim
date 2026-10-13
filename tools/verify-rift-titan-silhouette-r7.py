from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/verify-rift-titan-wrap-r6.py').read_text(encoding='utf-8').replace('rift-titan-wrap-r6','rift-titan-silhouette-r7')
exec(compile(source,str(__file__),'exec'))
import json
from mathutils import Vector
expected={'Root':None,'Body':'Root','ArmL':'Body','ArmR':'Body','ForearmL':'ArmL','ForearmR':'ArmR','LegL':'Body','LegR':'Body'}
arm=next(o for o in scene.objects if o.type=='ARMATURE')
assert {b.name:b.parent.name if b.parent else None for b in arm.data.bones}==expected
parts=json.loads(scene['silhouette_components']);obj=next(o for o in scene.objects if o.type=='MESH');me=obj.data;components=me.attributes['attachment_component']
def bounds(names):
 ids={parts[n] for n in names};vertices={v for f in me.polygons if components.data[f.index].value in ids for v in f.vertices}
 points=[obj.matrix_world@me.vertices[i].co for i in vertices]
 lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
 return dict(minimum=list(lo),maximum=list(hi),dimensions=list(hi-lo))
whole=bounds(list(parts));height=whole['dimensions'][2]
groups={'head':[n for n in parts if n.startswith('SplitCrown')], 'shoulders':[n for n in parts if n.startswith('ShoulderStratum')], 'cage':[n for n in parts if n.startswith(('ContinuousTorsoRib','WrappedStoneBand','LowerRibKeel','StoneCollarbone'))], 'feet':['FootL','FootR']}
report={name:bounds(names) for name,names in groups.items()}
for row in report.values():row['width_over_actor_height']=row['dimensions'][0]/height;row['height_over_actor_height']=row['dimensions'][2]/height
out=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-silhouette-r7'
(out/'silhouette-measurements.json').write_text(json.dumps(dict(actor=whole,groups=report,original_hierarchy=expected,scope='saved geometry proportions and original rig identities; these measurements do not establish concept matching or native deformation acceptance'),indent=2)+'\n',encoding='utf-8')
print('VERIFIED saved silhouette proportions and original eight-bone hierarchy')
