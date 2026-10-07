import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-first-bloom-bundles-r21.blend'))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');me=o.data;me.calc_loop_triangles()
records=json.loads(bpy.context.scene['tissue_attachment_contract']);names={r['support_component']:r['support'] for r in records}
bad=[]
for t in me.loop_triangles:
 a,b,c=[me.vertices[i].co for i in t.vertices]
 if (b-a).cross(c-a).length<=1e-10:
  component=me.attributes['attachment_component'].data[t.polygon_index].value
  bad.append(dict(vertices=list(t.vertices),component=names.get(component,component),points=[list(me.vertices[i].co) for i in t.vertices],markers=[me.attributes['attachment_endpoint'].data[i].value for i in t.vertices]))
print('VERIFIED degenerate triangle diagnostic '+json.dumps(bad),flush=True)
