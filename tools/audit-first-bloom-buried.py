"""Conservative hidden-face audit against closed, retained support components."""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from first_bloom_attachment_repair import inside
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-trim-r14';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-first-bloom-membranes-r13.blend'))
scene=bpy.context.scene;o=next(o for o in scene.objects if o.type=='MESH');arm=next(o for o in scene.objects if o.type=='ARMATURE')
records=json.loads(scene['tissue_attachment_contract']);protected={r['support_component'] for r in records}
o.data.calc_loop_triangles();attr=o.data.attributes['attachment_component'];tris={};face_tris={}
for t in o.data.loop_triangles:
 c=attr.data[t.polygon_index].value;tris.setdefault(c,[]).append((t.polygon_index,list(t.vertices)));face_tris.setdefault(t.polygon_index,[]).append(list(t.vertices))
def classify(mesh):
 points=[o.matrix_world@v.co for v in mesh.vertices];trees={};bounds={}
 for c,faces in tris.items():
  ids=set(i for p,f in faces for i in f);coords=[points[i] for i in ids]
  bounds[c]=([min(v[a] for v in coords) for a in range(3)],[max(v[a] for v in coords) for a in range(3)])
  trees[c]=BVHTree.FromPolygons(points,[f for p,f in faces],all_triangles=True)
 cuts={};pairs={}
 for c,faces in tris.items():
  if c in protected:continue
  hosts=[];lo,hi=bounds[c]
  for host,(hlo,hhi) in bounds.items():
   if host==c:continue
   # A protected component stays closed after trimming and is a usable host.
   if host not in protected:continue
   if any(hi[a]<hlo[a] or lo[a]>hhi[a] for a in range(3)):continue
   blocked={i for i,j in trees[c].overlap(trees[host])};hosts.append((host,blocked))
  for face in set(p for p,f in faces):
   local=[i for i,(p,f) in enumerate(faces) if p==face];indices=set(i for t in face_tris[face] for i in t)
   for host,blocked in hosts:
    if any(i in blocked for i in local):continue
    hlo,hhi=bounds[host]
    if any(any(points[i][a]<=hlo[a]+.001 or points[i][a]>=hhi[a]-.001 for a in range(3)) for i in indices):continue
    if not all(inside(points[i],trees[host]) and trees[host].find_nearest(points[i])[3]>.0015 for i in indices):continue
    if not all(inside(sum((points[i] for i in t),Vector())/3,trees[host]) for t in face_tris[face]):continue
    cuts[face]=host;break
 return cuts
arm.animation_data.action=None;arm.data.pose_position='REST';scene.frame_set(1)
for b in arm.pose.bones:b.matrix_basis=Matrix.Identity(4)
rest=classify(o.data);accepted=set(rest);samples=[]
names=str(scene['magenheim_authored_actions']).split(',');arm.data.pose_position='POSE'
for name in names:
 action=bpy.data.actions[name];arm.animation_data.action=action
 if action.slots:arm.animation_data.action_slot=action.slots[0]
 start,end=action.curve_frame_range
 for t in (0,.25,.5,.75,1):
  scene.frame_set(round(start+(end-start)*t));graph=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(graph);mesh=ev.to_mesh(preserve_all_data_layers=True,depsgraph=graph)
  current=classify(mesh);accepted&={f for f in current if current[f]==rest.get(f)};samples.append(dict(action=name,sample=t,remaining_faces=len(accepted)));ev.to_mesh_clear()
report=dict(source_triangles=len(o.data.loop_triangles),rest_candidate_faces=len(rest),accepted_faces=sorted(accepted),removed_triangles=sum(len(face_tris[f]) for f in accepted),retained_closed_support_components=sorted(protected),hosts={str(f):rest[f] for f in accepted},sampled_actions=names,samples=samples,method='whole faces inside a retained closed support, no potential boundary intersections, 1.5mm vertex clearance; same host through five samples of every retained action',scope='sampled deformation only; no all-frame or native animation proof')
(OUT/'buried-face-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print('VERIFIED hidden-face audit',report['rest_candidate_faces'],'rest candidates;',len(accepted),'accepted faces;',report['removed_triangles'],'removable triangles',flush=True)
