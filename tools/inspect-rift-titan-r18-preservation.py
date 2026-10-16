import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-rift-titan-broken-strata-r18.blend'))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');parts=json.loads(bpy.context.scene['silhouette_components'])
with bpy.data.libraries.load(str(ROOT/'assets/models/studies/underworld-boss-rift-titan-concept-body-r17.blend'),link=False) as (d,l):l.objects=['RiftTitan_CoherentSurface']
old=l.objects[0]
def points(me,name):
    tag=me.attributes['attachment_component'];ids={i for f in me.polygons if tag.data[f.index].value==parts[name] for i in f.vertices}
    return sorted(tuple(me.vertices[i].co) for i in ids)
for name in ['UnifiedCrystalHead','StoneCollarbone','WrappedStoneBand']+['NeckMetalRib'+str(j) for j in range(6)]:
    a=points(o.data,name);b=points(old.data,name);print('VERIFIED',name,len(a),len(b),max(abs(x-y) for p,q in zip(a,b) for x,y in zip(p,q)))
