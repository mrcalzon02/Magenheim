"""Saved-source contacts, cap interiors, preservation and standing-plane checks."""
from pathlib import Path
import sys,json,builtins
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from maw_contact_validation import validation_source,contains
s=(ROOT/'tools/verify-blackwater-maw-contacts.py').read_text().replace('blackwater-maw-refinement-r3','rift-titan-concept-body-r17').replace('creature-models/2026-10-04','creature-models/2026-10-16')
exec(compile(validation_source(s),str(__file__),'exec'))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
obj=next(o for o in scene.objects if o.type=='MESH');me=obj.data
parts=json.loads(scene['silhouette_components']);records=json.loads(scene['tissue_attachment_contract'])
labels=me.attributes['attachment_component'];markers=me.attributes['attachment_endpoint']
def part_ids(mesh,name):
    tag=mesh.attributes['attachment_component'];component=parts[name]
    return {i for f in mesh.polygons if tag.data[f.index].value==component for i in f.vertices}
with bpy.data.libraries.load(str(ROOT/'assets/models/studies/underworld-boss-rift-titan-fracture-planes-r16.blend'),link=False) as (data,loaded):loaded.objects=['RiftTitan_CoherentSurface']
old=loaded.objects[0];errors={}
for name in ['UnifiedCrystalHead']+['NeckMetalRib'+str(j) for j in range(6)]:
    a=sorted(tuple(me.vertices[i].co) for i in part_ids(me,name));b=sorted(tuple(old.data.vertices[i].co) for i in part_ids(old.data,name))
    assert len(a)==len(b)
    error=max(abs(x-y) for p,q in zip(a,b) for x,y in zip(p,q));assert error<2e-6,(name,error);errors[name]=error
feet={name:min(me.vertices[i].co.z for i in part_ids(me,name)) for name in ('FootL','FootR')}
sole=min(feet.values());lowest=min(v.co.z for v in me.vertices);assert abs(lowest-sole)<2e-6
uv=me.uv_layers.active.data
for f in me.polygons:
    head=labels.data[f.index].value==parts['UnifiedCrystalHead']
    assert all((uv[li].uv.y>=.6599 if head else uv[li].uv.y<=.6401) for li in f.loop_indices),'head/body atlas regions overlap'
shin={name:min(me.vertices[i].co.z for i in part_ids(me,name))-sole for name in ('LongFractureLegL2','LongFractureLegR2')};assert min(shin.values())>.10
hands={name:min(me.vertices[i].co.z for i in part_ids(me,name))-sole for name in parts if name.startswith('FingerStratum')};assert max(hands.values())<.25
# Independently sample every actual marked closing triangle in each retained
# action. This includes new digit topology and all preserved neck cap sections.
samples=[('REST',None,0)]
for name in str(scene['magenheim_authored_actions']).split(','):
    action=bpy.data.actions[name];start,end=action.curve_frame_range
    samples.extend((name,action,round(start+(end-start)*t)) for t in (0,.25,.5,.75,1))
bary=((1/3,1/3,1/3),(.5,.25,.25),(.25,.5,.25),(.25,.25,.5),(.5,.5,0),(0,.5,.5),(.5,0,.5))
total=0;gaps=[];counts={}
for name,action,frame in samples:
    arm.data.pose_position='POSE' if action else 'REST';arm.animation_data.action=action
    if action and action.slots:arm.animation_data.action_slot=action.slots[0]
    scene.frame_set(frame);graph=bpy.context.evaluated_depsgraph_get();ev=obj.evaluated_get(graph);mesh=ev.to_mesh(preserve_all_data_layers=True,depsgraph=graph);mesh.calc_loop_triangles()
    points=[ev.matrix_world@v.co for v in mesh.vertices];tag=mesh.attributes['attachment_component'];marker=mesh.attributes['attachment_endpoint'];cache={}
    for r in records:
        part=r['support_component']
        if part not in cache:
            ff=[tuple(t.vertices) for t in mesh.loop_triangles if tag.data[t.polygon_index].value==part]
            cache[part]=(BVHTree.FromPolygons(points,ff,all_triangles=True),np.array([[list(points[i]) for i in f] for f in ff]))
        bvh,tris=cache[part];ids={i for i,m in enumerate(marker.data) if m.value==r['id']}
        caps=[t for t in mesh.loop_triangles if set(t.vertices)<=ids];counts[str(r['id'])]=len(caps)
        for tri in caps:
            a,b,c=[points[i] for i in tri.vertices]
            for u,v,w in bary:
                p=a*u+b*v+c*w;total+=1
                if not contains(p,bvh,tris) and bvh.find_nearest(p)[3]>.001:gaps.append(dict(action=name,frame=frame,tissue=r['tissue'],support=r['support']))
    ev.to_mesh_clear()
report=dict(result='PASS' if not gaps else 'FAIL',actor_triangles=sum(len(f.vertices)-2 for f in me.polygons),head_neck_max_difference_metres=errors,standing_plane_z=sole,foot_planes=feet,shin_clearance_metres=shin,fingertip_clearance_metres=hands,closing_face_samples=total,closing_faces_by_contact=counts,poses=len(samples),outside_samples=gaps,scope='offline saved geometry and sampled action support; visual and native acceptance remain separate')
(OUT/'body-verification.json').write_text(json.dumps(report,indent=2)+'\n');assert not gaps,gaps[:5]
print('VERIFIED concept-body contacts, preserved head/neck, planted feet and hanging hands')
