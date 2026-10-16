"""Verify two separated recessed pockets and an uncut frontal stone plate."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
builder=(ROOT/'tools/verify-rift-titan-lined-volume-r14.py').read_text(encoding='utf-8')
exec(compile(builder[:builder.index("exec(compile(s,str(__file__),'exec'))")],str(__file__),'exec'))
s=s.replace('rift-titan-lined-volume-r14','rift-titan-paired-recesses-r15').replace('{1:76,2:28,3:12}','{1:78,2:28,3:10}')
start=s.index("stations=me.attributes['head_station']");end=s.index('groups={g.index:g.name',start)
s=s[:start]+'''depths=[];back_wall=[]
ring_attr=me.attributes['head_socket_ring'];corner_attr=me.attributes['head_socket_corner']
for side,sign in ((1,-1),(2,1)):
 lip={corner_attr.data[i].value:me.vertices[i].co for i in ids if ring_attr.data[i].value==side*2-1}
 floor={corner_attr.data[i].value:me.vertices[i].co for i in ids if ring_attr.data[i].value==side*2}
 assert set(lip)==set(floor)==set(range(1,8))
 for j in lip:
  depth=sign*(lip[j].x-floor[j].x);assert depth>.045,(side,j,depth);depths.append(depth)
''' +s[end:]
s=s.replace('assets/models/studies/underworld-boss-rift-titan-fractured-head-r13.blend','assets/models/studies/underworld-boss-rift-titan-lined-volume-r14.blend').replace('saved r13','saved r14')
exec(compile(s,str(__file__),'exec'))
from mathutils.bvhtree import BVHTree
from maw_contact_validation import contains
me=obj.data;components=me.attributes['attachment_component'];head=parts['UnifiedCrystalHead'];faces=[f for f in me.polygons if components.data[f.index].value==head];ids={i for f in faces for i in f.vertices}
old_labels=old.data.attributes['attachment_component'];old_faces=[f for f in old.data.polygons if old_labels.data[f.index].value==head];old_ids={i for f in old_faces for i in f.vertices}
outline=me.attributes['head_outline_vertex'];landmarks={outline.data[i].value:me.vertices[i].co for i in ids if outline.data[i].value};assert set(landmarks)==set(range(1,21))
max_outline_error=max(min((p-old.data.vertices[i].co).length for i in old_ids) for p in landmarks.values());assert max_outline_error<2e-6
front=me.attributes['head_front_plate'];lined=me.attributes['head_crystal_lining'];side_attr=me.attributes['head_socket_side'];role=me.attributes['head_surface_role']
plate_faces=[f for f in faces if front.data[f.index].value];assert len(plate_faces)==18 and all(role.data[f.index].value==1 and lined.data[f.index].value==0 and side_attr.data[f.index].value==0 for f in plate_faces)
plate_ids={i for f in plate_faces for i in f.vertices};assert plate_ids=={i for i in ids if outline.data[i].value}
bvh=BVHTree.FromPolygons([v.co for v in me.vertices],[tuple(f.vertices) for f in faces],all_triangles=True);tris=np.array([[list(me.vertices[i].co) for i in f.vertices] for f in faces])
for f in plate_faces:
 center=sum((me.vertices[i].co for i in f.vertices),Vector())/3;assert contains(center-f.normal*.001,bvh,tris),'front plate has no solid backing'
ring_attr=me.attributes['head_socket_ring'];corner_attr=me.attributes['head_socket_corner'];socket_rows=[]
for side in (1,2):
 lip={corner_attr.data[i].value:me.vertices[i].co for i in ids if ring_attr.data[i].value==side*2-1};floor={corner_attr.data[i].value:me.vertices[i].co for i in ids if ring_attr.data[i].value==side*2}
 floor_faces=[f for f in faces if side_attr.data[f.index].value==side and role.data[f.index].value==3];wall_faces=[f for f in faces if side_attr.data[f.index].value==side and role.data[f.index].value==2]
 assert len(floor_faces)==5 and len(wall_faces)==14 and all(lined.data[f.index].value for f in floor_faces)
 socket_rows.append(dict(side='left' if side==1 else 'right',rim_vertices=7,floor_vertices=7,wall_triangles=len(wall_faces),floor_triangles=len(floor_faces),lined_triangles=sum(lined.data[f.index].value for f in faces if side_attr.data[f.index].value==side),depths_metres=[abs(lip[j].x-floor[j].x) for j in range(1,8)]))
left_floor={corner_attr.data[i].value:me.vertices[i].co for i in ids if ring_attr.data[i].value==2};right_floor={corner_attr.data[i].value:me.vertices[i].co for i in ids if ring_attr.data[i].value==4}
for j in range(1,8):assert contains((left_floor[j]+right_floor[j])/2,bvh,tris),'paired pockets break through the central stone'
# Neck geometry is preserved except permitted head-end seating, independently
# checked against actual r14. All twelve attachment caps were sampled above.
neck_difference=0
for name,component in parts.items():
 if not name.startswith('NeckMetalRib'):continue
 current={i for f in me.polygons if components.data[f.index].value==component for i in f.vertices};previous={i for f in old.data.polygons if old_labels.data[f.index].value==component for i in f.vertices};assert len(current)==len(previous)==9
 current_fixed=[me.vertices[i].co for i in current if me.attributes['attachment_endpoint'].data[i].value not in {r['id'] for r in records if r['support']=='UnifiedCrystalHead'}]
 previous_fixed=[old.data.vertices[i].co for i in previous if old.data.attributes['attachment_endpoint'].data[i].value not in {r['id'] for r in records if r['support']=='UnifiedCrystalHead'}]
 difference=max(min((p-q).length for q in previous_fixed) for p in current_fixed);assert difference<2e-6;neck_difference=max(neck_difference,difference)
report=dict(result='PASS',connected_closed_head=True,vertices=60,triangles=116,solid_front_plate_triangles=18,median_crystal_faces=0,approved_front_outline_max_3d_difference_metres=max_outline_error,sockets=socket_rows,central_bridge_interior_samples=7,front_plate_interior_samples=18,neck_non_head_end_max_difference_metres=neck_difference,neck_closing_samples=3024,poses=36,actor_triangles=1800,scope='actual saved solid front plate, two independently closed crystal-lined side recesses and preserved outline/neck; user art and native acceptance remain separate')
(out/'paired-recess-verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('VERIFIED continuous stone front plate, two deep lined flank pockets and intact central bridge')
