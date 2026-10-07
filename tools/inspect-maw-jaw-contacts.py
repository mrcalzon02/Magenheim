import bpy,json,numpy as np,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-blackwater-maw-jaws-r5-raw.blend'))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');me=o.data;me.calc_loop_triangles();points=[o.matrix_world@v.co for v in me.vertices];records=json.loads(bpy.context.scene['tissue_attachment_contract'])
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');arm.animation_data.action=None;arm.data.pose_position='REST';bpy.context.scene.frame_set(0);graph=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(graph);em=ev.to_mesh(preserve_all_data_layers=True,depsgraph=graph);em.calc_loop_triangles()
print('VERIFIED eval vertex discrepancy',max((a.co-b.co).length for a,b in zip(me.vertices,em.vertices)),len(me.vertices),len(em.vertices),flush=True)
print('VERIFIED eval triangle discrepancy',sum(tuple(a.vertices)!=tuple(b.vertices) for a,b in zip(me.loop_triangles,em.loop_triangles)),flush=True)
me=em;me.calc_loop_triangles();points=[ev.matrix_world@v.co for v in me.vertices]
for name in ('UpperFang10','Sensory-1_3','CheekPlate-1_6'):
 item=next(r for r in records if r['tissue']==name);component=item['support_component'];faces=[list(t.vertices) for t in me.loop_triangles if me.attributes['attachment_component'].data[t.polygon_index].value==component];bvh=BVHTree.FromPolygons(points,faces,all_triangles=True);tris=np.array([[list(points[i]) for i in f] for f in faces])
 for index,a in enumerate(me.attributes['attachment_endpoint'].data):
  if a.value!=item['id']:continue
  p=points[index];counts=[]
  for raw in ((.713,.421,.561),(.234,.865,.421),(.913,-.311,.263),(-.421,.351,.837)):
   d=Vector(raw).normalized();q=p.copy();hits=0
   for _ in range(100):
    loc,n,t,dist=bvh.ray_cast(q,d,100)
    if loc is None:break
    hits+=1;q=loc+d*.00001
   counts.append(hits)
  xyz=tris-np.array(p);a,b,c=xyz[:,0],xyz[:,1],xyz[:,2];la=np.linalg.norm(a,axis=1);lb=np.linalg.norm(b,axis=1);lc=np.linalg.norm(c,axis=1)
  den=la*lb*lc+np.sum(a*b,axis=1)*lc+np.sum(b*c,axis=1)*la+np.sum(c*a,axis=1)*lb
  winding=float(np.sum(2*np.arctan2(np.sum(np.cross(a,b)*c,axis=1),den))/(4*math.pi))
  print('VERIFIED contact diagnostic',name,index,counts,'winding',winding,'distance',bvh.find_nearest(p)[3],flush=True)
