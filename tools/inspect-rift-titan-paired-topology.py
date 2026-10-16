import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-rift-titan-paired-recesses-r15.blend'))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');me=o.data;part=json.loads(bpy.context.scene['silhouette_components'])['UnifiedCrystalHead'];ff=[f for f in me.polygons if me.attributes['attachment_component'].data[f.index].value==part];edges={}
for f in ff:
 for a,b in zip(f.vertices,(*f.vertices[1:],f.vertices[0])):edges.setdefault(tuple(sorted((a,b))),[]).append(f.index)
def label(i):
 return dict(id=i,xyz=list(me.vertices[i].co),outline=me.attributes['head_outline_vertex'].data[i].value,ring=me.attributes['head_socket_ring'].data[i].value,corner=me.attributes['head_socket_corner'].data[i].value)
print(json.dumps(dict(faces=len(ff),vertices=len({i for f in ff for i in f.vertices}),bad_edges=[dict(vertices=[label(i) for i in e],faces=f) for e,f in edges.items() if len(f)!=2]),indent=2))
