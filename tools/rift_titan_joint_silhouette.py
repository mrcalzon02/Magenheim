"""Continue the front/left-side fit: sloped mantle and articulated stone masses."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector,Matrix
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
import rift_titan_silhouette_fit as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-joints-r8'
OUTLINES=(
 ((-.86,-.38),(-.95,.12),(-.32,.52),(.61,.44),(1,.02),(.28,-.52)),
 ((-1,-.22),(-.51,.49),(.24,.53),(.92,.20),(.70,-.43),(-.24,-.52)),
 ((-.62,-.47),(-.94,.05),(-.34,.50),(.75,.39),(1,-.14),(.25,-.53)),
)

def arm_fit(p,s):
 t=max(0,min(1,(3.02-p.z)/.92))
 z=p.z;stations=((0,.035),(1.30,.10),(2.10,-.24),(3.02,-.07),(4,-.07))
 dy=-.07
 for (a,b),(c,d) in zip(stations,stations[1:]):
  if z<=c:dy=b+(z-a)*(d-b)/(c-a);break
 return p+Vector((-s*.11*t,dy,0))

def frame(axis):
 up=Vector(axis).normalized();front=Vector((0,1,0));front=(front-up*front.dot(up)).normalized()
 right=front.cross(up).normalized()
 return Matrix((right,front,up)).transposed()

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;trunk=previous.build(B,arm,record,profile,tube,cap)
 # Move the existing elbow/wrist rest positions and their geometry together.
 # The front fit is modest; the profile gets a backward elbow and forward wrist.
 for side,s in [('L',-1),('R',1)]:
  for o in bpy.context.scene.objects:
   if o.type!='MESH':continue
   if o.name.startswith(('LongFractureArm'+side,'LongFractureForearm'+side,'JointBindingArm'+side,'JointBindingForearm'+side,'Palm'+side,'FingerStratum'+side)):
    for v in o.data.vertices:v.co=arm_fit(v.co,s)
   elif o.name.startswith('LongFractureLeg'+side) or o.name=='Foot'+side:
    for v in o.data.vertices:v.co.x-=s*.04
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
 for side,s in [('L',-1),('R',1)]:
  for name in ('Arm'+side,'Forearm'+side):
   bone=arm.data.edit_bones[name];bone.head=arm_fit(bone.head.copy(),s);bone.tail=arm_fit(bone.tail.copy(),s)
  bone=arm.data.edit_bones['Leg'+side];bone.head.x-=s*.04;bone.tail.x-=s*.04
 bpy.ops.object.mode_set(mode='OBJECT');arm.select_set(False)
 def stone(name,center,width,height,depth,axis=(0,0,1),variant=0,seed=0):
  o=bpy.data.objects[name];c=Vector(center);basis=frame(axis);vertices=[];outline=OUTLINES[variant%3]
  for side in (-1,1):
   for k,(u,z) in enumerate(outline):
    x=u*width*(1+.045*math.sin(seed+k*2.1))*(1 if side>0 else .88)
    taper=.42+.58*max(0,1-abs(z)/.55)
    y=side*depth*taper*(1+.12*math.cos(k*1.9+seed))
    zz=z*height+side*.025*height*math.sin(k*1.5+seed)
    vertices.append(c+basis@Vector((x,y,zz)))
  for side in (-1,1):vertices.append(c+basis@Vector((width*.025*math.sin(seed+1.3),side*depth*1.04,height*.04*math.cos(seed+.7))))
  assert len(vertices)==len(o.data.vertices)==14
  for v,p in zip(o.data.vertices,vertices):v.co=p
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
  return o
 for side,s in [('L',-1),('R',1)]:
  stone('ShoulderStratum'+side+'0',(s*.72,-.005,3.01),.39,.72,.28,(s*.40,.13,1),0,5)
  stone('ShoulderStratum'+side+'1',(s*1.10,.015,3.00),.285,.80,.25,(s*.13,.12,1),1,6)
  stone('ShoulderStratum'+side+'2',(s*.83,-.32,3.23),.34,.24,.17,(-s*.28,-.12,1),2,7)
  stone('FloatingStratum'+str(0 if side=='L' else 1),(s*.48,.14,2.91),.235,.52,.18,(-s*.32,.20,1),2,29)
  for bone in ('Arm'+side,'Forearm'+side,'Leg'+side):
   b=arm.data.bones[bone];a=b.head_local;end=b.tail_local;axis=a-end;length=axis.length
   for j,t in enumerate((.18,.50,.82)):
    c=a.lerp(end,t);local_axis=axis.copy()
    if bone.startswith('Leg'):
     c.y+=(.055,.235,.06)[j];local_axis.y=(-.18,.02,.16)[j]
     width=(.25,.26,.245)[j];depth=(.23,.25,.23)[j]
    elif bone.startswith('Forearm'):
     c.y+=(.01,.035,.035)[j];width=(.23,.255,.215)[j];depth=(.22,.25,.215)[j]
    else:
     c.y+=(.005,-.015,.015)[j];width=(.21,.22,.205)[j];depth=(.215,.225,.205)[j]
    stone('LongFracture'+bone+str(j),c,width,length*(.62 if j==1 else .55),depth,local_axis,j,j*3+len(bone))
  b=arm.data.bones['Forearm'+side];wrist=b.tail_local;axis=b.head_local-wrist
  stone('Palm'+side,wrist+Vector((0,.035,-.15)),.255,.35,.20,axis,1,13)
  for j in range(3):stone('FingerStratum'+side+str(j),wrist+Vector(((j-1)*.13,.12,-.36+.02*math.sin(j+1))),.105,.31,.14,axis,j,18+j)
  foot=arm.data.bones['Leg'+side].tail_local
  stone('Foot'+side,(foot.x,.31,.205),.30,.36,.33,(0,-.035,1),1,7)
 # Re-seat the old body support chain, then add the newly explicit limb roots.
 contacts=json.loads(bpy.context.scene['tissue_attachment_contract'])
 components=json.loads(bpy.context.scene['silhouette_components'])
 for side in ('L','R'):
  contacts.append(dict(id=len(contacts)+1,tissue='LongFractureArm'+side+'0',end='proximal-stone-root',support='ShoulderStratum'+side+'1',support_component=components['ShoulderStratum'+side+'1'],endpoint_seating='six proximal vertices seated into shoulder; Body weights at root, blended ridge into Arm'))
  for j in range(3):contacts.append(dict(id=len(contacts)+1,tissue='FingerStratum'+side+str(j),end='finger-palm-root',support='Palm'+side,support_component=components['Palm'+side],endpoint_seating='six proximal digit vertices embedded into palm with matching Forearm weights'))
 for r in contacts:
  o=bpy.data.objects[r['tissue']];host=bpy.data.objects[r['support']]
  if r['id']>50:
   ids=[1,2,3,7,8,9]
   for i in ids:o.data.attributes['attachment_endpoint'].data[i].value=r['id']
  else:ids=[v.index for v in o.data.vertices if o.data.attributes['attachment_endpoint'].data[v.index].value==r['id']]
  for i in ids:
   p,w=embed(o.data.vertices[i].co,host,.008);assign(o,i,p,w)
  if r['end']=='proximal-stone-root':
   side='L' if 'ArmL' in o.name else 'R'
   for i in (12,13):assign(o,i,o.data.vertices[i].co,{'Body':.45,'Arm'+side:.55})
 bpy.context.scene['tissue_attachment_contract']=json.dumps(contacts)
 (OUT/'joint-authoring.json').write_text(json.dumps(dict(reference='adopted front and left-facing side concept; continue silhouette before surface detail',changes=['steeper layered shoulder mantle instead of horizontal wings','modest inward arm fit, elbows back and wrists forward','arm stone axes follow the full three-dimensional joint direction','individual wedge sections replace repeated square-ended profiles','slightly closer leg stance, forward knees and lower foot masses','shorter attached stone fingers instead of detached long claws'],new_contacts='two arm/shoulder roots and six digit/palm roots; fifty existing body roots retained and reseated',weights='Body at proximal arm section, 45/55 Body/Arm ridge, remaining Arm vertices; matching Forearm digit roots; maximum two influences',cost='1800 existing triangles; eight original rig identities and seven retained actions; four existing 512px maps',scope='silhouette and attachment shaping only; no texture detail, no runtime promotion',acceptance='continued unaccepted silhouette candidate'),indent=2)+'\n',encoding='utf-8')
 return trunk
