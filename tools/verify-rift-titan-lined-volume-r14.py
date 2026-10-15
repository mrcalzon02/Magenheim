"""Saved head shape preservation, asymmetric lining and volumetric neck checks."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/verify-rift-titan-cracked-head-r12.py').read_text(encoding='utf-8').replace('rift-titan-cracked-head-r12','rift-titan-lined-volume-r14')
s=s.replace('len(faces)==108 and len(ids)==56','len(faces)==116 and len(ids)==60').replace('{1:76,2:24,3:8}','{1:76,2:28,3:12}')
s=s.replace('set(range(1,9))','set(range(1,5))')
s=s.replace('for j in range(1,8):',"from mathutils.bvhtree import BVHTree\nhead_tree=BVHTree.FromPolygons([obj.matrix_world@v.co for v in me.vertices],[tuple(f.vertices) for f in faces],all_triangles=True)\nfor j in range(1,8):")
s=s.replace('lip=(ring[2].y+ring[5].y)/2;floor=(ring[3].y+ring[4].y)/2;rear=(ring[7].y+ring[8].y)/2', "lip=(ring[1].y+ring[2].y)/2;floor=(ring[3].y+ring[4].y)/2\n assert ring[3].y<ring[4].y-.025,('left fracture must recede farther',j)\n origin=(ring[3]+ring[4])/2-Vector((0,.0001,0));hit=head_tree.ray_cast(origin,Vector((0,-1,0)));assert hit[0] is not None,('missing rear wall',j)\n rear=hit[0].y")
s=s.replace("roots=[r for r in records if r['support']=='UnifiedCrystalHead'];assert len(roots)==6 and all(r['end']=='head-neck-root' for r in roots)","roots=[r for r in records if r['tissue'].startswith('NeckMetalRib')];assert len(roots)==12 and {r['end'] for r in roots}=={'head-neck-root','collar-neck-root'}")
s=s.replace("root_groups=6,closing_triangles_per_group=1","root_groups=12,closing_triangles_per_group=1").replace('6 triangles,','12 triangles,').replace("neck['sampled_points']==1512","neck['sampled_points']==3024")
s=s.replace("metrics['triangles']==1792","metrics['triangles']==1800").replace("['head-crystal']==108","['head-crystal']==116").replace('vertices=56,triangles=108','vertices=60,triangles=116').replace('neck_face_samples=1512','neck_face_samples=3024').replace('total_actor_triangles=1792','total_actor_triangles=1800')
s=s.replace('assets/models/studies/underworld-boss-rift-titan-notched-r11.blend','assets/models/studies/underworld-boss-rift-titan-fractured-head-r13.blend').replace('saved r11','saved r13').replace('six reseated neck sections excluded','six remodeled neck bars excluded')
s=s.replace("['total']=1792","['total']=1800").replace('1792 triangles','1800 triangles')
exec(compile(s,str(__file__),'exec'))
# Re-fetch persistent data after evaluated closing-face meshes have been freed.
me=obj.data;components=me.attributes['attachment_component'];head=parts['UnifiedCrystalHead'];faces=[f for f in me.polygons if components.data[f.index].value==head];ids={i for f in faces for i in f.vertices}
old_labels=old.data.attributes['attachment_component'];old_faces=[f for f in old.data.polygons if old_labels.data[f.index].value==head];old_ids={i for f in old_faces for i in f.vertices}
outline=me.attributes['head_outline_vertex'];landmarks={outline.data[i].value:me.vertices[i].co.copy() for i in ids if outline.data[i].value};assert set(landmarks)==set(range(1,21))
max_xz_error=0
for p in landmarks.values():
 error=min(max(abs(p.x-old.data.vertices[i].co.x),abs(p.z-old.data.vertices[i].co.z)) for i in old_ids);assert error<2e-6,error;max_xz_error=max(max_xz_error,error)
tip=landmarks[10];bottom=landmarks[1];assert tip.y<bottom.y-.30,'top must rake back from lower point'
def volume(mesh,ff):
 return abs(sum(mesh.vertices[f.vertices[0]].co.dot(mesh.vertices[f.vertices[1]].co.cross(mesh.vertices[f.vertices[2]].co))/6 for f in ff))
old_volume=volume(old.data,old_faces);new_volume=volume(me,faces);assert new_volume>old_volume*1.35,(old_volume,new_volume)
lining=me.attributes['head_crystal_lining'];sections=me.attributes['head_section'];roles=me.attributes['head_surface_role']
left_walls=[f for f in faces if roles.data[f.index].value==2 and {sections.data[i].value for i in f.vertices}=={1,3}];assert len(left_walls)==12 and all(lining.data[f.index].value==1 for f in left_walls)
floor_faces=[f for f in faces if roles.data[f.index].value==3];assert len(floor_faces)==12 and all(lining.data[f.index].value==1 for f in floor_faces)
uv=me.uv_layers.active.data;lining_area=0
for f in faces:
 if lining.data[f.index].value:
  a,b,c=[uv[i].uv for i in f.loop_indices];lining_area+=abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))/2*512*512
neck_mid=[];neck_centers=[]
for j in range(6):
 component=parts['NeckMetalRib'+str(j)];ff=[f for f in me.polygons if components.data[f.index].value==component];vv={i for f in ff for i in f.vertices};assert len(ff)==14 and len(vv)==9
 marker=me.attributes['attachment_endpoint'];middle=[me.vertices[i].co for i in vv if marker.data[i].value==0];assert len(middle)==3
 center=sum(middle,Vector())/3;neck_mid.append(list(center));neck_centers.extend(list(me.vertices[i].co) for i in vv)
mid=np.array(neck_mid);all_points=np.array(neck_centers);sv=np.linalg.svd(all_points-all_points.mean(axis=0),compute_uv=False)
assert np.ptp(mid[:,1])>.20 and np.ptp(mid[:,0])>.10,(np.ptp(mid,axis=0).tolist())
assert sv[-1]>.07,sv.tolist()
report=dict(result='PASS',approved_front_outline_landmarks=20,max_front_xz_error_metres=max_xz_error,tip_recession_from_base_metres=bottom.y-tip.y,previous_head_volume_cubic_metres=old_volume,head_volume_cubic_metres=new_volume,volume_ratio=new_volume/old_volume,left_lined_wall_triangles=len(left_walls),lined_floor_triangles=len(floor_faces),lining_nominal_uv_texel_area=lining_area,neck_middle_centers=neck_mid,neck_middle_extent_metres=np.ptp(mid,axis=0).tolist(),neck_coordinate_singular_values=sv.tolist(),neck_closing_samples=3024,poses=36,scope='saved front-shape preservation, increased head volume, left-receding violet lining and genuinely noncoplanar neck geometry; art and native acceptance remain separate')
(out/'volume-fracture-neck-verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('VERIFIED approved front outline retained, increased head volume, left crystal lining and 3D neck')
