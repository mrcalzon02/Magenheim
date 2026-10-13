"""Continuous torso ribs beneath a narrow stone band wrapped around the cage."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
from first_bloom_frill_sculpt import assign
from first_bloom_deeper_chest import embed
from first_bloom_attachment_repair import tree,inside
import rift_titan_r4_base as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-wrap-r6'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;trunk=previous.build(B,arm,record,profile,tube,cap)
 # Remove the mistaken basket and the loose pieces occupying the plate/neck region.
 for o in list(bpy.context.scene.objects):
  if o.type=='MESH' and (o.name.startswith(('CoreHoop','DiagonalCage','ShoulderBinding')) or o.name in ('FloatingStratum4','FloatingStratum5')):bpy.data.objects.remove(o,do_unlink=True)
 metal=B.mats['rift-bands'].copy();metal.name='Rift corroded metal ribs';B.mats['rift-metal-ribs']=metal
 bs=metal.node_tree.nodes['Principled BSDF'];value=metal.node_tree.nodes.new('ShaderNodeValue');value.outputs[0].default_value=.76;metal.node_tree.links.new(value.outputs[0],bs.inputs['Roughness'])
 def rib(name,points,radii,category):
  o=tube(name,points,radii,'rift-metal-ribs','Body',3,category)
  pts=[Vector(p) for p in points];normal=Vector((1,0,0));last=None
  # Transport the section along the bend, avoiding the track-quaternion roll
  # flips that twist a three-sided rib into alternating pinched sections.
  for j,(p,r) in enumerate(zip(pts,radii)):
   tangent=(pts[min(j+1,len(pts)-1)]-pts[max(j-1,0)]).normalized()
   if last is not None:normal=last.rotation_difference(tangent)@normal
   normal=(normal-tangent*normal.dot(tangent)).normalized();cross=tangent.cross(normal).normalized()
   for k in range(3):
    a=math.tau*k/3;o.data.vertices[j*3+k].co=p+r*(normal*math.cos(a)+cross*.83*math.sin(a))
   last=tangent
  o.data.update();return o
 def plate(name,outline,y,depth,relief=0,strip=False):
  n=len(outline);vertices=[(x,y+side*depth+relief*max(0,1-abs(x)/.20),z) for side in (-1,1) for x,z in outline]
  # A bent V band has nonplanar front/back surfaces. Explicit corresponding
  # strip stations preserve the concave silhouette; ngon projection can bridge
  # across the V and falsely render a straight band plus an isolated triangle.
  ends=[tuple(reversed(range(n))),tuple(range(n,2*n))]
  if strip:ends=[(offset+k,offset+k+1,offset+n-2-k,offset+n-1-k) for offset in (0,n) for k in range(n//2-1)]
  faces=ends+[(k,(k+1)%n,(k+1)%n+n,k+n) for k in range(n)]
  me=bpy.data.meshes.new(name);me.from_pydata(vertices,[],faces);me.materials.append(B.mats['rift-strata']);o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);record(o,'Body','torso-stone')
  bpy.context.view_layer.objects.active=o;o.select_set(True);bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces),ngon_method='EAR_CLIP');bm.to_mesh(me);bm.free()
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(65),island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT');me.uv_layers.active.name='BossConstructionUV';o.select_set(False);return o
 def ring(name,stations,outer,inner,height,category):
  vertices=[];faces=[];n=len(stations)
  for angle,z in stations:
   s=math.sin(angle);c=math.cos(angle);fracture=.004*math.sin(angle*5)
   vertices.extend([(outer[0]*s,outer[2]+outer[1]*c,z+height/2+fracture),(outer[0]*s,outer[2]+outer[1]*c,z-height/2+fracture),(inner[0]*s,inner[2]+inner[1]*c,z)])
  for j in range(n):
   for k in range(3):
    a=j*3+k;b=j*3+(k+1)%3;next_j=(j+1)%n
    faces.append((a,b,next_j*3+(k+1)%3,next_j*3+k))
  me=bpy.data.meshes.new(name);me.from_pydata(vertices,[],faces);me.materials.append(B.mats['rift-strata'])
  o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);record(o,'Body',category)
  bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED');bm.to_mesh(me);bm.free()
  bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(65),island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT');me.uv_layers.active.name='BossConstructionUV';o.select_set(False)
  return o
 collar=ring('StoneCollarbone',[(math.tau*j/8,2.76+.014*math.sin(math.tau*j/8*3)) for j in range(8)],(.62,.33,.06),(.51,.23,.06),.09,'torso-stone')
 keel=plate('LowerRibKeel',[(-.11,1.71),(.11,1.71),(0,1.58)],.17,.07)
 roots=[]
 # Seat the collar into the larger torso before seating ribs into that collar;
 # later collar deformations would invalidate previously embedded rib roots.
 for side,j in [('R',2),('L',6)]:
  roots.append((collar,list(range(j*3,j*3+3)),bpy.data.objects['ShoulderStratum'+side+'0'],'collar-to-upper-torso-'+side))
 ribs=[];stations=[];count=12
 # Every metal rib continues through the band into the lower bowl. Five
 # coherent sections per rib replace the split upper/lower construction.
 for j in range(count):
  angle=math.tau*j/count;s=math.sin(angle);c=math.cos(angle)
  notch=.095*max(0,1-abs(s)/.40) if c>0 else 0
  z=2.20+.012*math.sin(angle*3)-notch
  points=[(.56*s,.06+.28*c,2.76),(.51*s,.14+.30*c,2.48+.012*math.sin(angle*3)),(.52*s,.14+.32*c,z),(.32*s,.14+.22*c,1.88),(.035*s,.17+.026*c,1.65)]
  radii=[.014,.028,.028,.025,.014]
  if j in (4,5,7,8):points.pop(1);radii.pop(1)
  o=rib('ContinuousTorsoRib'+str(j),points,radii,'torso-ribs');ribs.append(o)
  end=(len(points)-1)*3
  roots.extend(((o,list(range(3)),collar,'collar-root'),(o,list(range(end,end+3)),keel,'lower-keel-root')))
  stations.append((angle,z))
 # Closed, narrow wedge-section stone ribbon, following the entire torso
 # circumference. Outer top/bottom form a thin face; the inner ridge bites
 # into each underlying metal rib instead of hovering outside the cage.
 band=ring('WrappedStoneBand',stations,(.555,.355,.14),(.52,.32,.14),.058,'torso-band')
 for j,o in enumerate(ribs):roots.append((band,[j*3+2],o,'band-to-rib-'+str(j)))
 # Ribbed neck visibly joins the upper stone collar to the head's lower plate.
 head=bpy.data.objects['SplitCrown3']
 for j in range(6):
  x=-.125+.050*j
  o=rib('NeckMetalRib'+str(j),[(x,.34,2.78),(x*.94,.25,2.96),(x*.88,.17,3.08)],[.013,.025,.013],'neck-ribs')
  roots.extend(((o,list(range(3)),collar,'collar-neck-root'),(o,list(range(6,9)),head,'head-neck-root')))
 meshes=sorted([o for o in bpy.context.scene.objects if o.type=='MESH'],key=lambda o:o.name);components={o.name:i+1 for i,o in enumerate(meshes)}
 for o in meshes:
  attr=o.data.attributes.new('attachment_component','INT','FACE')
  for a in attr.data:a.value=components[o.name]
  o.data.attributes.new('attachment_endpoint','INT','POINT')
 records=[]
 for o,indices,host,label in roots:
  marker=len(records)+1
  for i in indices:
   p,w=embed(o.data.vertices[i].co,host,.013);assign(o,i,p,w);o.data.attributes['attachment_endpoint'].data[i].value=marker
  assert all(inside(o.data.vertices[i].co,tree(host)) for i in indices),(o.name,label)
  records.append(dict(id=marker,tissue=o.name,end=label,support=host.name,support_component=components[host.name],endpoint_seating='full rib endpoint section or wrapped band inner ridge embedded into named support; matching Body weights'))
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'torso-authoring.json').write_text(json.dumps(dict(reference='full concept and user supplied torso close-ups; thin wrapped band and upper collar ring correction, 2026-10-13',torso_ribs=count,neck_ribs=6,central_plate='closed 360-degree narrow sculpted stone band with a small central V; 12 wedge-section stations, 72 triangles',band_height_authored=.058,band_radial_thickness_authored=.035,band_rib_contacts=count,upper_plate='closed eight-station stone collar ring, 48 triangles; full left and right sections embedded into the larger shoulder/upper torso stones',supports='all twelve torso ribs run continuously from upper collar ring to lower keel; middle band seats into every rib; neck ribs attach to the collar front arc and retained head; collar joins both upper torso masses',removed='r5 thick front V plate, flat collar bridge and split upper/lower rib layout',scope='torso and collar ring; neck roots reseated; previous limbs, shoulders, head, rig and actions retained',acceptance='candidate for concept review'),indent=2)+'\n',encoding='utf-8')
 return trunk
