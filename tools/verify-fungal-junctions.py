"""Check the repaired puffcap has one connected stalk and valid shading normals."""
import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'assets/models/source/underworld-flora-fungal-puffcap.blend'))
stalk=bpy.data.objects['fused-stalk']
mesh=stalk.data
adj={v.index:set() for v in mesh.vertices}
for edge in mesh.edges:
 a,b=edge.vertices
 adj[a].add(b); adj[b].add(a)
seen=set(); pending=[0]
while pending:
 v=pending.pop()
 if v in seen:continue
 seen.add(v); pending.extend(adj[v]-seen)
assert len(seen)==len(mesh.vertices), 'Stalk must be one connected skin'
for obj in bpy.context.scene.objects:
 if obj.type=='MESH':
  assert all(n.vector.length>.99 for n in obj.data.corner_normals), obj.name
print('VERIFIED fused puffcap: connected root/trunk/branch skin and valid corner normals',flush=True)
