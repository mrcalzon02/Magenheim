import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-rift-titan-fracture-planes-r16.blend'))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');me=o.data;me.calc_loop_triangles()
parts=json.loads(bpy.context.scene['silhouette_components']);labels=me.attributes['attachment_component']
rows={}
for name,part in parts.items():
 ff=[f for f in me.polygons if labels.data[f.index].value==part];ids={i for f in ff for i in f.vertices}
 rows[name]=dict(triangles=sum(len(f.vertices)-2 for f in ff),vertices=len(ids),min=[min(me.vertices[i].co[k] for i in ids) for k in range(3)],max=[max(me.vertices[i].co[k] for i in ids) for k in range(3)])
(ROOT/'dist/art-inspection-logs/current-parts.json').write_text(json.dumps(rows,indent=2))
print('VERIFIED current component inventory')
