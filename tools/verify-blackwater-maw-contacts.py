"""Verify every saved support through five samples of all retained actions."""
import bpy,json,sys
from pathlib import Path
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from first_bloom_attachment_repair import inside
OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-refinement-r3'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-blackwater-maw-refinement-r3.blend'))
scene=bpy.context.scene;o=next(o for o in scene.objects if o.type=='MESH');arm=next(o for o in scene.objects if o.type=='ARMATURE')
records=json.loads(scene['tissue_attachment_contract']);actions=str(scene['magenheim_authored_actions']).split(',');report=[];worst=0
samples=[('REST',None,0)]
for name in actions:
 action=bpy.data.actions[name];start,end=action.curve_frame_range
 for t in (0,.25,.5,.75,1):samples.append((name,action,round(start+(end-start)*t)))
for name,action,frame in samples:
 arm.data.pose_position='POSE' if action else 'REST';arm.animation_data.action=action
 if action and action.slots:arm.animation_data.action_slot=action.slots[0]
 scene.frame_set(frame);graph=bpy.context.evaluated_depsgraph_get();evaluated=o.evaluated_get(graph);me=evaluated.to_mesh(preserve_all_data_layers=True,depsgraph=graph)
 me.calc_loop_triangles();points=[evaluated.matrix_world@v.co for v in me.vertices];components=me.attributes['attachment_component'];markers=me.attributes['attachment_endpoint'];hosts={};gaps=[]
 for item in records:
  component=item['support_component']
  if component not in hosts:hosts[component]=BVHTree.FromPolygons(points,[list(t.vertices) for t in me.loop_triangles if components.data[t.polygon_index].value==component],all_triangles=True)
  ids=[i for i,a in enumerate(markers.data) if a.value==item['id']];assert ids,item
  distance=max((hosts[component].find_nearest(points[i])[3] for i in ids if not inside(points[i],hosts[component])),default=0)
  worst=max(worst,distance)
  if distance>.001:gaps.append(dict(tissue=item['tissue'],end=item['end'],support=item['support'],distance=distance))
 report.append(dict(action=name,frame=frame,outside_contacts=gaps));evaluated.to_mesh_clear()
result=dict(result='PASS' if worst<=.001 else 'FAIL',endpoints=len(records),retained_actions=len(actions),sampled_poses=len(samples)-1,max_outside_distance_metres=worst,tolerance_metres=.001,samples=report,scope='saved rest mesh and five Blender samples per retained action; native and every-frame acceptance pending')
(OUT/'attachment-verification.json').write_text(json.dumps(result,indent=2)+'\n')
print('VERIFIED Maw contacts',result['result'],len(records),'contacts',len(samples)-1,'poses',worst,flush=True)
assert result['result']=='PASS','saved or animated support separation exceeds one millimetre'
