"""Measure saved support contacts through five inherited attack samples."""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from first_bloom_attachment_repair import inside
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-attachments-r5'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-first-bloom-attachments-r5.blend'))
scene=bpy.context.scene;arm=next(o for o in scene.objects if o.type=='ARMATURE');o=next(o for o in scene.objects if o.type=='MESH')
records=json.loads(scene['tissue_attachment_contract']);names=str(scene['magenheim_authored_actions']).split(',')
names=[n for n in names if any(t in n.lower() for t in ('slam','sweep','bite','cast','attack'))] or names
action=bpy.data.actions[names[0]];arm.data.pose_position='POSE';arm.animation_data.action=action
if action.slots:arm.animation_data.action_slot=action.slots[0]
start,end=action.curve_frame_range;report=[];worst=0
for t in (0,.25,.5,.75,1):
 scene.frame_set(round(start+(end-start)*t));graph=bpy.context.evaluated_depsgraph_get();evaluated=o.evaluated_get(graph);mesh=evaluated.to_mesh(preserve_all_data_layers=True,depsgraph=graph)
 mesh.calc_loop_triangles();points=[evaluated.matrix_world@v.co for v in mesh.vertices];components=mesh.attributes['attachment_component'];markers=mesh.attributes['attachment_endpoint'];hosts={};gaps=[]
 for record in records:
  component=record['support_component']
  if component not in hosts:
   faces=[list(tri.vertices) for tri in mesh.loop_triangles if components.data[tri.polygon_index].value==component];hosts[component]=BVHTree.FromPolygons(points,faces,all_triangles=True)
  ids=[i for i,a in enumerate(markers.data) if a.value==record['id']]
  distance=0
  for index in ids:
   if not inside(points[index],hosts[component]):distance=max(distance,hosts[component].find_nearest(points[index])[3])
  worst=max(worst,distance)
  if distance>.00001:gaps.append(dict(tissue=record['tissue'],end=record['end'],support=record['support'],outside_distance_metres=distance))
 report.append(dict(sample=t,frame=scene.frame_current,max_outside_distance_metres=max((r['outside_distance_metres'] for r in gaps),default=0),outside_contacts=gaps));evaluated.to_mesh_clear()
result=dict(result='PASS' if worst<=.001 else 'FAIL',action=action.name,samples=report,max_outside_distance_metres=worst,tolerance_metres=.001,scope='five inherited Blender attack frames; all vertices strictly inside supports in rest geometry; native animation unverified')
(OUT/'attachment-pose-verification.json').write_text(json.dumps(result,indent=2)+'\n')
print('VERIFIED attachment pose measurement '+result['result']+' max gap '+str(worst),flush=True)
assert result['result']=='PASS','sampled attachment separation exceeds one millimetre'
