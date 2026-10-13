"""Redistribute binding topology into supported limb crystals and deeper stone facets."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
import rift_titan_joint_silhouette as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-crystal-limbs-r9'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;trunk=previous.build(B,arm,record,profile,tube,cap)
 components=json.loads(bpy.context.scene['silhouette_components'])
 contacts=json.loads(bpy.context.scene['tissue_attachment_contract'])
 def finish(o):
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
  bpy.context.view_layer.objects.active=o;o.select_set(True)
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(65),island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT');o.data.uv_layers.active.name='BossConstructionUV';o.select_set(False)
 def replace(o,points,faces,key,bone):
  me=bpy.data.meshes.new(o.name+' sculpt');me.from_pydata(points,[],faces);me.materials.append(B.mats[key]);o.data=me
  for g in list(o.vertex_groups):o.vertex_groups.remove(g)
  vg=o.vertex_groups.new(name=bone);vg.add(list(range(len(points))),1,'REPLACE')
  attr=me.attributes.new('attachment_component','INT','FACE')
  for a in attr.data:a.value=components[o.name]
  me.attributes.new('attachment_endpoint','INT','POINT');finish(o)
 # The six slender joint bindings need a single closed section, rather than
 # two repeated four-sided sections. Keep their full axial reach and rig owner.
 for side in ('L','R'):
  for bone in ('Arm'+side,'Forearm'+side,'Leg'+side):
   o=bpy.data.objects['JointBinding'+bone];b=arm.data.bones[bone];a=b.head_local.copy();z=b.tail_local.copy();c=a.lerp(z,.5);basis=previous.frame(a-z)
   pts=[a,z]+[c+basis@Vector((.029*math.cos(k*math.tau/4),.024*math.sin(k*math.tau/4),0)) for k in range(4)]
   faces=[(0,2+k,2+(k+1)%4) for k in range(4)]+[(1,2+(k+1)%4,2+k) for k in range(4)]
   replace(o,pts,faces,'rift-bands',bone)
 # Keep the shoulder-blended proximal plates intact. Develop staggered depth
 # in the remaining existing limb plates: narrow splinter tips, oblique front
 # ridges, and a deeper rear plane instead of identical flattened wedges.
 for side,s in [('L',-1),('R',1)]:
  for bi,bone in enumerate(('Arm'+side,'Forearm'+side,'Leg'+side)):
   for j in range(3):
    if bi==0 and j==0:continue
    o=bpy.data.objects['LongFracture'+bone+str(j)];pts=[v.co.copy() for v in o.data.vertices];c=sum(pts,Vector())/len(pts)
    axis=arm.data.bones[bone].head_local-arm.data.bones[bone].tail_local;basis=previous.frame(axis)
    for v in o.data.vertices:
     p=basis.transposed()@(v.co-c)
     p.y*=1.17+.06*math.sin(j*2+bi)
     if v.index in (0,5,6,11):p.x*=.84;p.z-=.025*(1+j)
     if v.index==13:p.x+=s*(.055 if j%2==0 else -.045);p.z+=.055 if j==1 else -.035;p.y+=.035
     if v.index==12:p.x-=s*.025;p.y-=.025
     v.co=c+basis@p
    finish(o)
 # Reseating follows all support shaping, so existing palm/collar roots remain
 # tied to their actual final hosts rather than stale pre-sculpt coordinates.
 for r in contacts:
  o=bpy.data.objects[r['tissue']];host=bpy.data.objects[r['support']]
  for v in o.data.vertices:
   if o.data.attributes['attachment_endpoint'].data[v.index].value==r['id']:
    p,w=embed(v.co,host,.008);assign(o,v.index,p,w)
 growths=[]
 def crystal(name,host_name,bone,seed,reuse=False):
  host=bpy.data.objects[host_name];surface=host.data.vertices[13].co.copy()
  # Roots form a small complete ring inside the front stone face; the shard
  # leans across the adjacent fracture instead of standing like a vertical peg.
  s=-1 if bone.endswith('L') else 1
  surface.z+=(.07,-.07,.025)[seed%3];surface.x+=s*(.025 if seed%2 else -.025)
  axis=Vector((s*(.25 if seed%2 else -.18),.35,(-.90,-.70,.80)[seed%3])).normalized()
  n=5 if reuse else 3;r=.095 if reuse else .078
  ring=[surface+Vector((r*math.cos(k*math.tau/n),-.035,r*math.sin(k*math.tau/n))) for k in range(n)]
  points=[surface-Vector((0,.09,0))]+ring+[surface+axis*(.24+.050*(seed%3))]
  tip=n+1;faces=[(0,1+(k+1)%n,1+k) for k in range(n)]+[(tip,1+k,1+(k+1)%n) for k in range(n)]
  if reuse:o=bpy.data.objects[name]
  else:
   me=bpy.data.meshes.new(name);o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);record(o,bone,'binding-crystals');components[name]=max(components.values())+1
  replace(o,points,faces,'binding-prism',bone)
  marker=len(contacts)+1
  for i in range(n+1):
   p,w=embed(o.data.vertices[i].co,host,.025);assign(o,i,p,w);o.data.attributes['attachment_endpoint'].data[i].value=marker
  finish(o)
  r=dict(id=marker,tissue=name,end='limb-crystal-root',support=host_name,support_component=components[host_name],endpoint_seating='complete basal crystal section embedded in sculpted stone, identical support bone weights')
  contacts.append(r);growths.append(dict(name=name,support=host_name,bone=bone,triangles=2*n,basal_vertices=n+1,basal_faces=n))
 for side in ('L','R'):
  for j,(bone,index) in enumerate((('Arm'+side,1),('Arm'+side,2),('Forearm'+side,0),('Forearm'+side,1),('Leg'+side,0),('Leg'+side,1))):
   crystal('LimbCrystal'+side+str(j),'LongFracture'+bone+str(index),bone,j+(1 if side=='R' else 0))
 for k,(side,bone,index) in enumerate((('L','ForearmL',2),('R','ForearmR',2),('L','LegL',2),('R','LegR',2))):
  crystal('FloatingPrism'+str(k+2),'LongFracture'+bone+str(index),bone,k+2,True)
 bpy.context.scene['silhouette_components']=json.dumps(components)
 bpy.context.scene['tissue_attachment_contract']=json.dumps(contacts)
 (OUT/'limb-authoring.json').write_text(json.dumps(dict(changes=['deeper staggered front and rear rock facets','narrow splinter tips and oblique face ridges','twelve added limb shards and four formerly floating shards seated into limbs'],crystal_growths=growths,budget=dict(binding_before=120,binding_after=48,freed_triangles=72,new_crystals=72,total=1800),preserved='original head, torso rings, shoulder mantle, rig, actions, arm root blends and existing supports',scope='requested limb depth/detail and crystal growth; remaining full-creature texture refinement and native acceptance pending',acceptance='unaccepted candidate'),indent=2)+'\n',encoding='utf-8')
 return trunk
