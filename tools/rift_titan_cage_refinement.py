"""Close cage hoops coherently and seat both ends of each diagonal brace."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
from first_bloom_frill_sculpt import assign
from first_bloom_deeper_chest import embed
from first_bloom_attachment_repair import tree,inside
import rift_titan_primary_refinement as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-strata-r4'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;trunk=previous.build(B,arm,record,profile,tube,cap)
 def assign_mesh(o,me):
  o.data=me;o.vertex_groups.clear()
  for v in me.vertices:assign(o,v.index,v.co,{'Body':1.0})
 for j,z in enumerate((2.04,2.47)):
  o=bpy.data.objects['CoreHoop'+str(j)];n=12;m=4
  centers=[Vector((math.cos(k*math.tau/n)*.52,.14+math.sin(k*math.tau/n)*.29,z+.035*math.sin(k*math.tau/6))) for k in range(n)]
  vertices=[];faces=[]
  for k,c in enumerate(centers):
   tangent=(centers[(k+1)%n]-centers[(k-1)%n]).normalized();up=Vector((0,0,1));up=(up-tangent*up.dot(tangent)).normalized();out=tangent.cross(up).normalized()
   for a in range(m):angle=math.tau*a/m;vertices.append(c+.035*(up*math.cos(angle)+out*math.sin(angle)))
  for k in range(n):
   for a in range(m):faces.append((k*m+a,k*m+(a+1)%m,((k+1)%n)*m+(a+1)%m,((k+1)%n)*m+a))
  me=bpy.data.meshes.new(o.name+'_ClosedLoop');me.from_pydata(vertices,[],faces);me.materials.append(B.mats['rift-bands']);assign_mesh(o,me);uv=me.uv_layers.new(name='BossConstructionUV')
  for p in me.polygons:
   p.use_smooth=True;seam=any(i//m==0 for i in p.vertices) and any(i//m==n-1 for i in p.vertices)
   for li in p.loop_indices:
    index=me.loops[li].vertex_index;k,a=divmod(index,m);uv.data[li].uv=(1 if seam and k==0 else k/n,a/m)
  bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();me.update()
 # Diagonal roots lie at interpolated hoop stations, rather than projecting past them.
 for s in (-1,1):
  for rear in (-1,1):
   o=bpy.data.objects['DiagonalCage'+str(s)+'_'+str(rear)]
   points=[(s*.355,.14+rear*.198,2.0703109),(s*.49,.14+rear*.28,2.28),(s*.355,.14+rear*.198,2.5003109)]
   temp=B.tube(o.name+'_seated',points,[.018,.041,.018],'rift-bands','Body',4);me=temp.data;assign_mesh(o,me);bpy.data.objects.remove(temp,do_unlink=True)
 meshes=sorted([o for o in bpy.context.scene.objects if o.type=='MESH'],key=lambda o:o.name);components={o.name:i+1 for i,o in enumerate(meshes)}
 for o in meshes:
  attr=o.data.attributes.new('attachment_component','INT','FACE')
  for a in attr.data:a.value=components[o.name]
  o.data.attributes.new('attachment_endpoint','INT','POINT')
 records=[]
 for o in meshes:
  if not o.name.startswith('DiagonalCage'):continue
  for end,indices,host_name in [('lower',range(4),'CoreHoop0'),('upper',range(8,12),'CoreHoop1')]:
   host=bpy.data.objects[host_name];marker=len(records)+1
   for i in indices:
    p,w=embed(o.data.vertices[i].co,host,.012);assign(o,i,p,w);o.data.attributes['attachment_endpoint'].data[i].value=marker
   assert all(inside(o.data.vertices[i].co,tree(host)) for i in indices),o.name
   records.append(dict(id=marker,tissue=o.name,end=end,support=host.name,support_component=components[host.name],endpoint_seating='complete four-vertex brace section inside closed hoop; Body weights match'))
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'cage-authoring.json').write_text(json.dumps(dict(closed_hoops=2,brace_roots=len(records),removed_duplicate_end_cap_triangles=8,weights='all cage vertices bound to Body; hoop/brace contacts move together',method='cyclic cross-sections, no overlapping seam caps; explicit embedded brace roots'),indent=2)+'\n',encoding='utf-8')
 return trunk
