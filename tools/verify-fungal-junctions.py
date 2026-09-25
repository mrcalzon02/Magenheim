"""Check every fused canopy variant has one connected stalk and valid shading normals."""
import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[1]
paths = sorted((root/'assets/models/source').glob('underworld-flora-fungal-puffcap*.blend'))
paths += sorted((root/'assets/models/source').glob('underworld-flora-fungal-glowcap-*.blend'))
assert len(paths) == 7, 'Expected four puffcaps and three branching glowcaps'
for path in paths:
 bpy.ops.wm.open_mainfile(filepath=str(path))
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
 assert len(seen)==len(mesh.vertices), path.name + ': disconnected stalk'
 for obj in bpy.context.scene.objects:
  if obj.type=='MESH':
   assert all(n.vector.length>.99 for n in obj.data.corner_normals), path.name + '/' + obj.name
 print('VERIFIED fused canopy: ' + path.stem,flush=True)
