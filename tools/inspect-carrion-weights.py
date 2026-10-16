import bpy,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-carrion-crown-anatomy-r3.blend'))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');me=o.data;parts=json.loads(bpy.context.scene['silhouette_components']);names={v:k for k,v in parts.items()};component=me.attributes['attachment_component'];owners={i:names[component.data[f.index].value] for f in me.polygons for i in f.vertices}
bad=[]
for v in me.vertices:
 w=[g for g in v.groups if g.weight>1e-7]
 if not (1<=len(w)<=2 and abs(sum(g.weight for g in w)-1)<1e-5):bad.append(dict(id=v.index,part=owners.get(v.index),weights={o.vertex_groups[g.group].name:g.weight for g in w}))
print(json.dumps(bad[:15],indent=2))
