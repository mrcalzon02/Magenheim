"""Concept torso: numerous swept metal ribs, V stone plate, collar and ribbed neck."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
from first_bloom_frill_sculpt import assign
from first_bloom_deeper_chest import embed
from first_bloom_attachment_repair import tree,inside
import rift_titan_r4_base as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-torso-r5'

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
 collar=plate('StoneCollarbone',[(-.64,2.82),(-.25,2.86),(.24,2.86),(.64,2.82),(.51,2.65),(0,2.68),(-.52,2.65)],.04,.145)
 center=plate('CentralVStonePlate',[(-.49,2.18),(-.12,2.16),(0,1.99),(.12,2.16),(.50,2.20),(.44,2.01),(.16,1.99),(0,1.78),(-.18,1.99),(-.45,1.99)],.43,.105,.06,True)
 keel=plate('LowerRibKeel',[(-.13,1.72),(.13,1.72),(0,1.56)],.17,.10)
 roots=[]
 # Front ribs join the cross-chest plate; the deeper ribs sweep beneath it
 # into the pointed lower cage visible in the concept's torso close-up.
 for side,s in [('L',-1),('R',1)]:
  for j in range(3):
   mid_x=s*(.26+.125*j)
   points=[(s*(.18+.06*j),.08,2.72),(mid_x,.44,2.47),(mid_x*.97,.46,2.28),(s*(.22+.08*j),.43,2.06)]
   o=rib('TorsoMetalRib'+side+str(j),points,[.014,.029,.025,.014],'torso-ribs')
   roots.extend(((o,list(range(3)),collar,'collar-root'),(o,list(range(9,12)),center,'central-plate-root')))
  for j in range(4):
   angle=(j+.5)*math.pi/4;mid_x=s*(.38+.14*math.sin(angle));mid_y=.14+.30*math.cos(angle)
   points=[(s*(.21+.075*j),.01,2.72),(mid_x,mid_y,2.43),(mid_x*.97,mid_y,2.04),(s*(.19+.10*math.sin(angle)),.14+.20*math.cos(angle),1.79),(s*.03,.17,1.65)]
   o=rib('LowerSweptRib'+side+str(j),points,[.014,.027,.028,.025,.014],'torso-ribs')
   roots.extend(((o,list(range(3)),collar,'collar-root'),(o,list(range(12,15)),keel,'lower-keel-root')))
 # Ribbed neck visibly joins the upper stone collar to the head's lower plate.
 head=bpy.data.objects['SplitCrown3']
 for j in range(6):
  x=-.125+.050*j
  o=rib('NeckMetalRib'+str(j),[(x,.08,2.80),(x*.94,.125,2.96),(x*.88,.17,3.08)],[.013,.025,.013],'neck-ribs')
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
  records.append(dict(id=marker,tissue=o.name,end=label,support=host.name,support_component=components[host.name],endpoint_seating='entire three-vertex rib root embedded into named stone plate; matching Body weights'))
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'torso-authoring.json').write_text(json.dumps(dict(reference='full Rift Titan concept, user torso interpretation and supplied V-band close-up, 2026-10-13',torso_ribs=14,neck_ribs=6,central_plate='stone cross-chest band bent into a solid downward V with a shallow projecting center',upper_plate='stone collarbone between the shoulders',supports='six front ribs join collarbone and central plate; eight deeper ribs sweep below the central plate into a small lower keel; neck ribs join collarbone and head',removed='basket hoops, four diagonal braces, unsupported shoulder bars and two loose lower torso blocks',scope='torso and neck only; previous limbs, head, rig and actions retained',acceptance='candidate for concept review'),indent=2)+'\n',encoding='utf-8')
 return trunk
